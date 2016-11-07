using System;
using System.Threading.Tasks;

namespace CC.Mobile.Purchases
{

    /// <summary>
    /// Minimal Definition of a product needed for purchases
    /// </summary>
    public interface IProduct
    {
        string ProductId { get; }
    }
    
}
