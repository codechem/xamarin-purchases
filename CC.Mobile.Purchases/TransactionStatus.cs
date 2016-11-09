using System;
using System.Threading.Tasks;

namespace CC.Mobile.Purchases
{

    public enum TransactionStatus
    {
        Purchased,
        Failed,
        Cancelled
    }

    public class TransactionStatusArgs : EventArgs
    {
        public string TransactionId { get; protected set; }
        public string ProductId { get; protected set; }
        public TransactionStatus Status { get; protected set; }

        public TransactionStatusArgs(string transactionId, string productId, TransactionStatus status)
        {
            Status = status;
            ProductId = productId;
            TransactionId = transactionId;
        }
    }

}
