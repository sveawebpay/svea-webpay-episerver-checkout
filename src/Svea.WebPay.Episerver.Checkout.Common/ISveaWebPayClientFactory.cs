using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;

using Svea.WebPay.SDK;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public interface ISveaWebPayClientFactory
    {
        ISveaClient Create(IMarket market, string languageId);
        ISveaClient Create(PaymentMethodDto paymentMethodDto, MarketId marketMarketId);
        ISveaClient Create(ConnectionConfiguration connectionConfiguration);
    }
}