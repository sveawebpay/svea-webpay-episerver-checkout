using EPiServer.Commerce.Order;

using Mediachase.Commerce;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.SDK.CheckoutApi;

using System.Globalization;


namespace Svea.WebPay.Episerver.Checkout
{
    public interface ISveaWebPayCheckoutService : ISveaWebPayService
    {
        Data CreateOrUpdateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage);
        Data CreateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage);
        Data GetOrder(IOrderGroup orderGroup);
        Data GetOrder(long orderId, IMarket market, string languageId);
        CheckoutConfiguration LoadCheckoutConfiguration(IMarket market, string languageId);
    }
}
