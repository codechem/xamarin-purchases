using System;
using UIKit;
using CC.Mobile.Purchases;
using System.Threading.Tasks;

namespace PurchaseExample.Touch
{
    public partial class ViewController : UIViewController
    {
        public static IProduct PROD_ONE = new Product("com.codechem.com.mobile.xamarin-purchase.one");
        IPurchaseService svc;
        protected ViewController(IntPtr handle) : base(handle)
        {
        }

        async public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            svc = new PurchaseServiceIOS();
            btnPurchase.TouchUpInside += async (s, e) => await MakePurchase(PROD_ONE);
            await svc.Init();
        }

        async public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            await svc.Resume();
        }

        async public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            await svc.Pause();
        }

        async Task MakePurchase(IProduct product)
        {
            try
            {
                var purchase = await svc.Purchase(product);
                if (purchase.Status == TransactionStatus.Purchased)
                {
                    new UIAlertView("Success", $"Just Purchased {product}", null, "OK").Show();
                }
                else {
                    new UIAlertView("Failed Purchase", $"Cannot Purchase {product}", null, "OK").Show();
                }
            }
            catch (PurchaseError ex)
            {
                new UIAlertView("Error", $"Error with {product}:{ex.Message}", null, "OK").Show();
            }
        }
    }
}
