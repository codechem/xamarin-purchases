using System;
using System.Threading.Tasks;

namespace CC.Mobile.Purchases
{

    /// <summary>
    /// Purchasing result
    /// </summary>
    public class Purchase
    {
        public IProduct Product { get; private set; }
        public string TransactionId { get; private set; }
        public TransactionStatus Status { get; private set; }
        public Purchase(IProduct product, string transactionId, TransactionStatus status)
        {
            Product = product;
            TransactionId = transactionId;
            Status = status;
        }
    }
    
}
