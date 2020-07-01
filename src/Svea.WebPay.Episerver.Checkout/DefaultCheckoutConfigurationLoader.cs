using EPiServer.ServiceLocation;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

using Newtonsoft.Json;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.Episerver.Checkout.Extensions;

using System;
using System.Linq;

namespace Svea.WebPay.Episerver.Checkout
{
    [ServiceConfiguration(typeof(ICheckoutConfigurationLoader))]
    public class DefaultCheckoutConfigurationLoader : ICheckoutConfigurationLoader
    {
        public CheckoutConfiguration GetConfiguration(MarketId marketId, string languageId)
        {
            var paymentMethod = PaymentManager.GetPaymentMethodBySystemName(Constants.SveaWebPayCheckoutSystemKeyword, languageId, marketId.Value, returnInactive: true);
            if (paymentMethod == null)
            {
                throw new Exception($"PaymentMethod {Constants.SveaWebPayCheckoutSystemKeyword} is not configured for market {marketId} and language {languageId}");
            }
            return GetConfiguration(paymentMethod, marketId);
        }

        public CheckoutConfiguration GetConfiguration(PaymentMethodDto paymentMethodDto, MarketId marketId)
        {
            var languageId = paymentMethodDto.PaymentMethod.First().LanguageId;
            var parameter = paymentMethodDto.GetParameter($"{marketId.Value}_{languageId}_{Constants.SveaWebPaySerializedMarketOptions}", string.Empty);

            var configuration = JsonConvert.DeserializeObject<CheckoutConfiguration>(parameter);

            return configuration ?? new CheckoutConfiguration();
        }

        public void SetConfiguration(CheckoutConfiguration configuration, PaymentMethodDto paymentMethod, string currentMarket)
        {
            var serialized = JsonConvert.SerializeObject(configuration);
            var languageId = paymentMethod.PaymentMethod.First().LanguageId;
            paymentMethod.SetParameter($"{currentMarket}_{languageId}_{Constants.SveaWebPaySerializedMarketOptions}", serialized);
        }
    }
}