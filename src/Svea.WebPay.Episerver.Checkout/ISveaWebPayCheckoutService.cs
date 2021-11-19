using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;

using Mediachase.Commerce;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.SDK.CheckoutApi;

using System.Globalization;
using System.Threading.Tasks;


namespace Svea.WebPay.Episerver.Checkout
{
    public interface ISveaWebPayCheckoutService : ISveaWebPayService
    {
        Task<Data> CreateOrUpdateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null);
        Task<Data> GetOrder(IOrderGroup orderGroup);
        Task<Data> GetOrder(long orderId, IMarket market, string languageId);
        CheckoutConfiguration LoadCheckoutConfiguration(IMarket market, string languageId);
    }
}