using System;
using System.Threading.Tasks;

namespace CC.Mobile.Purchases
{
    
    /// <summary>
    /// Purchase service.
    /// Important: Only one purchase can be made at a time, 
    /// There cant be more than one active purchase happening thru the service.
    /// </summary>
    public interface IPurchaseService:IDisposable
    {
        /// <summary>
        /// Initializes the purchasing service
        /// </summary>
        Task<IPurchaseService> Init(object context = null);

        /// <summary>
        /// Resumes the service and sets it to operational state
        /// returns the resulting state of the service started=true
        /// </summary>
        Task<bool> Resume();

        /// <summary>
        /// Pauses the purchase service
        /// returns the resulting state of the service started=true
        /// in case when there is ongoing purchase the service will not be paused
        /// </summary>
        Task<bool> Pause();

        /// <summary>
        /// Purchases the product.
        /// Throws a Purchase error in case the purchase is failed
        /// </summary>
        /// <param name="product">Product.</param>
        Task<Purchase> Purchase(IProduct product);
    }
}
