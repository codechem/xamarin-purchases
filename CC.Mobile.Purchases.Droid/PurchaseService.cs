using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using IAB = Xamarin.InAppBilling;

namespace CC.Mobile.Purchases
{
    public class PurchaseService:IPurchaseService
    {
        string publicKey;
        IAB.InAppBillingServiceConnection inAppSvc;
        TaskCompletionSource<Purchase> currentPurchaseTask;
        TaskCompletionSource<bool> serviceStatusTask;
        IProduct currentProduct;
        Purchase currentPurchase;

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:PurchaseExample.Droid.PurchaseService"/> is started.
        /// </summary>
        /// <value><c>true</c> if is started; otherwise, <c>false</c>.</value>
        public bool IsStarted { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PurchaseExample.Droid.PurchaseService"/> class.
        /// </summary>
        /// <param name="publicKey">Public key.</param>
        public PurchaseService(string publicKey)
        {
            this.publicKey = publicKey;
        }

        /// <summary>
        /// Inits the service with an activity;
        /// </summary>
        /// <param name="context">Context.</param>
        public Task<IPurchaseService> Init(object context = null)
        {
            var activity = context as Activity;
            inAppSvc = new IAB.InAppBillingServiceConnection(activity, publicKey);
            inAppSvc.OnConnected += () =>
            {
                IsStarted = true;
                serviceStatusTask.SetResult(IsStarted);
                serviceStatusTask = null;
                if (inAppSvc?.BillingHandler != null)
                {
                    inAppSvc.BillingHandler.OnProductPurchased += OnProductPurchased;
                    inAppSvc.BillingHandler.OnProductPurchasedError += OnProductPurchasedError;
                    inAppSvc.BillingHandler.OnPurchaseConsumed += OnPurchaseConsumed;
                    inAppSvc.BillingHandler.OnPurchaseConsumedError += OnPurchaseConsumedError;
                    inAppSvc.BillingHandler.OnProductPurchasedError += OnProductPurchasedError;
                    inAppSvc.BillingHandler.OnPurchaseFailedValidation += OnPurchaseFailedValidation;
                    inAppSvc.BillingHandler.OnUserCanceled += OnUserCanceled;
                }
            };

            inAppSvc.OnDisconnected += () =>
            {
                IsStarted = false;
                serviceStatusTask?.SetResult(IsStarted);
                serviceStatusTask = null;
                if (inAppSvc?.BillingHandler != null)
                {
                    inAppSvc.BillingHandler.OnProductPurchased -= OnProductPurchased;
                    inAppSvc.BillingHandler.OnProductPurchasedError -= OnProductPurchasedError;
                    inAppSvc.BillingHandler.OnPurchaseConsumed -= OnPurchaseConsumed;
                    inAppSvc.BillingHandler.OnPurchaseConsumedError -= OnPurchaseConsumedError;
                    inAppSvc.BillingHandler.OnProductPurchasedError -= OnProductPurchasedError;
                    inAppSvc.BillingHandler.OnPurchaseFailedValidation-=OnPurchaseFailedValidation;
                    inAppSvc.BillingHandler.OnUserCanceled -= OnUserCanceled;
                }
            };

            inAppSvc.OnInAppBillingError += (errType, err) =>
            {
                IsStarted = false;
                serviceStatusTask.SetException(new PurchaseError($"{errType.ToString()}:{err}"));
                serviceStatusTask = null;
            };

            return Task.FromResult(this as IPurchaseService);
        }

        public Task<bool> Resume()
        {
            return SetObserver();
        }

        async public Task<bool> Pause()
        {
             //in case when there is ongoing purchase the service will not be paused
            if (IsStarted && currentProduct == null)
            {
                return await UnsetObserver();
            }
            return false;
        }

        async public Task<Purchase> Purchase(IProduct product)
        {
            currentPurchaseTask = new TaskCompletionSource<Purchase>();
            currentProduct = product;

            var products = new List<string> { product.ProductId };
            var inAppProducts = await inAppSvc.BillingHandler.QueryInventoryAsync(products, IAB.ItemType.Product);
            if (inAppProducts == null || inAppProducts.Count == 0)
            {
                currentPurchaseTask = null;
                throw new PurchaseError("Product not found");
            }
            inAppSvc.BillingHandler.BuyProduct(inAppProducts[0]);
            return await currentPurchaseTask.Task;
        }
        /// <summary>
        /// This method must be invoked in the Activities OnActivityResult
        /// </summary>
        public void HandleActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (resultCode == Result.Canceled)
            {
                SetResultAndReset(TransactionStatus.Cancelled);
            }
            inAppSvc.BillingHandler.HandleActivityResult(requestCode, resultCode, data);
        }

        void OnProductPurchased(int response, IAB.Purchase purchase, string purchaseData, string purchaseSignature)
        {
            currentPurchase = new Purchase(currentProduct, purchase.OrderId, TransactionStatus.Purchased);
            inAppSvc.BillingHandler.ConsumePurchase(purchase);
        }
        void OnPurchaseFailedValidation(IAB.Purchase purchase, string purchaseData, string purchaseSignature)
        {
            currentPurchase = new Purchase(currentProduct, purchase.OrderId, TransactionStatus.Failed);
            SetResultAndReset(currentPurchase);
        }
        void OnPurchaseConsumed(string token)
        {
            SetResultAndReset(currentPurchase);
        }
        void OnProductPurchasedError(int responseCode, string sku)
        {
            var res = InAppPurchaseResponse.ByCode(responseCode);
            SetErrorAndReset(new PurchaseError($"Cannot Purchase: {res.Description}"));
        }

        void OnPurchaseConsumedError(int responseCode, string token)
        {
            var res = InAppPurchaseResponse.ByCode(responseCode);
            SetErrorAndReset(new PurchaseError($"Cannot Consume the purchase: {res.Description}"));
        }

        void OnUserCanceled()
        {
            currentPurchaseTask.SetCanceled();
            currentPurchaseTask = null;
        }

        Task<bool> SetObserver()
        {
            if (!IsStarted && serviceStatusTask == null)
            {
                serviceStatusTask = new TaskCompletionSource<bool>();
                inAppSvc.Connect();
                return serviceStatusTask.Task;
            }
            else {
                throw new PurchaseError("Another task is already running and must be waited");
            }
        }

        Task<bool> UnsetObserver()
        {
            if (IsStarted && serviceStatusTask == null)
            {
                serviceStatusTask = new TaskCompletionSource<bool>();
                // Attempt to connect to the service
                inAppSvc.Disconnect();
                return serviceStatusTask.Task;
            }
            else
            {
                throw new PurchaseError("Another task is already running and must be waited");
            }
        }

       
        void SetResultAndReset(TransactionStatus status, string transactionId = null)
        {
            currentPurchase = new Purchase(currentProduct, transactionId, TransactionStatus.Cancelled);
            SetResultAndReset(currentPurchase);
        }

        void SetErrorAndReset(PurchaseError error)
        {
            currentPurchaseTask?.SetException(error);
            Reset();
        }

        void SetResultAndReset(Purchase purchase)
        {
            currentPurchaseTask?.SetResult(purchase);
            Reset();
        }

        void Reset()
        {
            currentProduct = null;
            currentPurchaseTask = null;
            currentPurchase = null;
        }

        public void Dispose()
        {
            if (inAppSvc != null)
            {
                if (inAppSvc?.BillingHandler != null)
                {
                    inAppSvc.BillingHandler.OnProductPurchased -= OnProductPurchased;
                    inAppSvc.BillingHandler.OnProductPurchasedError -= OnProductPurchasedError;
                    inAppSvc.BillingHandler.OnPurchaseConsumed -= OnPurchaseConsumed;
                    inAppSvc.BillingHandler.OnPurchaseConsumedError -= OnPurchaseConsumedError;
                    inAppSvc.BillingHandler.OnProductPurchasedError -= OnProductPurchasedError;
                    inAppSvc.BillingHandler.OnUserCanceled -= OnUserCanceled;
                }
                inAppSvc.Disconnect();
                inAppSvc = null;
            }

            currentProduct = null;
            currentPurchase = null;
            if (currentPurchaseTask != null)
                currentPurchaseTask.SetCanceled();
        }

       
    }

    public class InAppPurchaseResponse
    {
        public static Dictionary<int, InAppPurchaseResponse> ALL = new Dictionary<int, InAppPurchaseResponse>
        {
            {0, new InAppPurchaseResponse(0, "Success")},
            {1, new InAppPurchaseResponse(1, "User pressed back or canceled a dialog")},
            {2, new InAppPurchaseResponse(2, "Network connection is down")},
            {3, new InAppPurchaseResponse(3, "Billing API version is not supported for the type requested")},
            {4, new InAppPurchaseResponse(4, "Requested product is not available for purchase")},
            {5, new InAppPurchaseResponse(5, "Invalid arguments provided to the API. This error can also indicate that the application was not correctly signed or properly set up for In-app Billing in Google Play, or does not have the necessary permissions in its manifest")},
            {6, new InAppPurchaseResponse(6, "Fatal error during the API action")},
            {7, new InAppPurchaseResponse(7, "Failure to purchase since item is already owned")},
            {8, new InAppPurchaseResponse(8, "Failure to consume since item is not owned")}
        };

        public int Code { get; set; }
        public string Description { get; set; }

        public static InAppPurchaseResponse ByCode(int code) => ALL[code];

        public InAppPurchaseResponse(int code, string description = "")
        {
            this.Code = code;
            this.Description = description;
        }
    }
}
