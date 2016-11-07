using System;
using System.Threading.Tasks;

namespace CC.Mobile.Purchases
{
 
    /// <summary>
    /// Generic error returned fron the IPurchase service implementation
    /// </summary>
    public class PurchaseError : Exception
    {
        public PurchaseError() : base("Purchasing error") { }
        public PurchaseError(string message) : base(message) { }
    }
    
}
