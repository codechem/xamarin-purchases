using System;
using System.Threading.Tasks;
using StoreKit;

namespace CC.Mobile.Purchases
{

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