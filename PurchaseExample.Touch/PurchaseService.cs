using System;
using System.Threading.Tasks;
using StoreKit;

namespace CC.Mobile.Purchases
{
    /// <summary>
    /// Purchase service  implementation for ios.
    /// </summary>
    public class PurchaseServiceIOS : IPurchaseService
    {
        CustomPaymentObserver paymentObserver;
        TaskCompletionSource<Purchase> currentPurchaseTask;
        IProduct currentProduct;

        public bool IsStarted { get; private set; } = false;

        /// <summary>
        /// The context is not used in the iOS app so id does not need to be supplied
        /// </summary>
        /// <param name="context">Context.</param>
        public Task 
        Init(object context = null)
        {
            return Task.FromResult(this as IPurchaseService);
        }

        /// <summary>
        /// Resumes the service and sets it to operational state
        /// returns the resulting state of the service started=true
        /// </summary>
        public Task<bool>
        Resume()
        {
            SetObserver();
            IsStarted = true;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Pauses the purchase service
        /// returns the resulting state of the service started=true
        /// in case when there is ongoing purchase the service will not be paused
        /// </summary>
        public Task<bool>
        Pause()
        {
            //in case when there is ongoing purchase the service will not be paused
            if (IsStarted && currentProduct == null)
            {
                UnsetObserver();
                IsStarted = false;
            }
            return Task.FromResult(IsStarted);
        }

        /// <summary>
        /// Starts the purchase process
        /// </summary>
        /// <returns>a purchase result containing the transaction 
        /// and it's state along with the product.</returns>
        /// <param name="product">Product.</param>
        async public Task<Purchase>
        Purchase(IProduct product)
        {
            if (await Resume())
            {
                currentProduct = product;
                currentPurchaseTask = new TaskCompletionSource<Purchase>();
                SKPayment payment = SKPayment.CreateFrom(product.ProductId);
                SKPaymentQueue.DefaultQueue.AddPayment(payment);
                return await currentPurchaseTask.Task;
            }
            else {
                throw new PurchaseError("Service cannot be started or there is annother active purchase");
            }
        }

        void 
        SetObserver()
        {
            paymentObserver = new CustomPaymentObserver();
            SKPaymentQueue.DefaultQueue.AddTransactionObserver(paymentObserver);
            paymentObserver.TransactionStatusChanged += OnTransactionStatusChanged;
        }

        void 
        UnsetObserver()
        {
            SKPaymentQueue.DefaultQueue.RemoveTransactionObserver(paymentObserver);
            paymentObserver.TransactionStatusChanged -= OnTransactionStatusChanged;
            paymentObserver = null;
        }

        void 
        OnTransactionStatusChanged(object sender, TransactionStatusArgs e)
        {
            if (e.ProductId == currentProduct.ProductId)
            {
                if (currentPurchaseTask == null)
                    throw new PurchaseError("There was no purchase registered in the service");
                var purchase = new Purchase(currentProduct, e.TransactionId, e.Status);
                currentPurchaseTask.SetResult(purchase);
                currentPurchaseTask = null;
                currentProduct = null;
            }
            else {
                throw new PurchaseError("Got purchase notification for unexpected product");
            }
        }

        public void Dispose()
        {
            currentProduct = null;
            if (currentPurchaseTask != null)
                currentPurchaseTask.SetCanceled();
        }
    }

    /// <summary>
    /// Acts as a listener for the payments in the StoreKit
    /// Provides an EventHandler where the results are being transmitted in .Net style
    /// </summary>
    internal class CustomPaymentObserver : SKPaymentTransactionObserver
    {
        public event EventHandler<TransactionStatusArgs>
        TransactionStatusChanged;

        // called when the transaction status is updated
        public override void
        UpdatedTransactions(SKPaymentQueue queue, SKPaymentTransaction[] transactions)
        {
            foreach (SKPaymentTransaction transaction in transactions)
            {
                switch (transaction.TransactionState)
                {
                    case SKPaymentTransactionState.Purchased:

                        TransactionStatusChanged?.Invoke(
                            this,
                            new TransactionStatusArgs(
                                transaction.TransactionIdentifier,
                                transaction.Payment.ProductIdentifier,
                                TransactionStatus.Purchased));

                        // remove the transaction from the payment queue.
                        SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);
                        break;
                    case SKPaymentTransactionState.Failed:
                        TransactionStatusChanged?.Invoke(
                            this,
                            new TransactionStatusArgs(
                                transaction.TransactionIdentifier,
                                transaction.Payment.ProductIdentifier,
                                TransactionStatus.Failed));
                        SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);
                        break;
                    default:
                        break;
                }
            }
        }
    }

}