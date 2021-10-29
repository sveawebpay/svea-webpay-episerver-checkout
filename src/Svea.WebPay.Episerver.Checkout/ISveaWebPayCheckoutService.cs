using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;

using Mediachase.Commerce;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.SDK.CheckoutApi;

using System.Globalization;


namespace Svea.WebPay.Episerver.Checkout
{
    public interface ISveaWebPayCheckoutService : ISveaWebPayService
    {
        Data CreateOrUpdateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null);
        Data GetOrder(IOrderGroup orderGroup);
        Data GetOrder(long orderId, IMarket market, string languageId);
        CheckoutConfiguration LoadCheckoutConfiguration(IMarket market, string languageId);
    }
}