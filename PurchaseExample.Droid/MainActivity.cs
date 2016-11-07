using Android.App;
using Android.Widget;
using Android.OS;
using CC.Mobile.Purchases;
using System.Threading.Tasks;

namespace PurchaseExample.Droid
{
    [Activity(Label = "Purchase Example", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        static IProduct PROD_OK = new Product("android.test.purchased");
        static IProduct PROD_CANCELLED = new Product("android.test.cancelled");
        static IProduct PROD_UNAVAILABLE = new Product("android.test.item_unavailable");
        IPurchaseService svc;
        async protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);
            svc = new PurchaseService("in app billing public key");
            await svc.Init();
            await svc.Resume();

            BindPurchaseButton(Resource.Id.ok, PROD_OK);
            BindPurchaseButton(Resource.Id.canceled, PROD_CANCELLED);
            BindPurchaseButton(Resource.Id.unavailable, PROD_UNAVAILABLE);
        }

        void BindPurchaseButton(int resource, IProduct product)
        {
            FindViewById<Button>(resource).Click += async (s, e) => await MakePurchase(product);
        }

        async Task MakePurchase(IProduct product)
        {
            try
            {
                var purchase = await svc.Purchase(product);
                if (purchase.Status == TransactionStatus.Purchased)
                {
                    Toast.MakeText(this, $"Success: Just Purchased {product}", ToastLength.Long).Show();
                }
                else {
                    Toast.MakeText(this, $"Failed Purchase: Cannot Purchase {product}", ToastLength.Long).Show();
                }
            }
            catch (PurchaseError ex)
            {
                Toast.MakeText(this, $"Error with {product}:{ex.Message}", ToastLength.Long).Show();
            }
        }

        /// <summary>
        /// IMPORTANT!!! This method must be overriden and the PurchaseService method HandleActivityResult 
        /// must be invoked because the purchase is being handled thru this activity 
        /// </summary>
        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            (svc as PurchaseService).HandleActivityResult(requestCode, resultCode, data);
        }

        protected override void OnDestroy()
        {
            svc.Dispose();
            svc = null;
            base.OnDestroy();
        }
    }
}

