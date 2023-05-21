using ECommerce_MW.Common;
using ECommerce_MW.Models;

namespace ECommerce_MW.Helpers
{
    public interface IOrderHelper
    {
        Task<Response> ProcessOrderAsync(ShowCartViewModel showCartViewModel);

        Task<Response> CancelOrderAsync(Guid orderId);
    }
}
