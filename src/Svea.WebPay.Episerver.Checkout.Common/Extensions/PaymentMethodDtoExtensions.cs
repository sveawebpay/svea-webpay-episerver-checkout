using EPiServer.Globalization;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;

using Newtonsoft.Json;

using System;
using System.Linq;

namespace Svea.WebPay.Episerver.Checkout.Common.Extensions
{
    internal static class PaymentMethodDtoExtensions
    {
        internal static ConnectionConfiguration GetConnectionConfiguration(this PaymentMethodDto paymentMethodDto, MarketId marketId)
        {
            var configuration = JsonConvert.DeserializeObject<ConnectionConfiguration>(paymentMethodDto.GetParameter($"{marketId.Value}_{Constants.SveaWebPaySerializedMarketOptions}", string.Empty));

            if (configuration == null)
            {
                throw new Exception($"PaymentMethod {paymentMethodDto.PaymentMethod.FirstOrDefault()?.SystemKeyword} is not configured for market {marketId} and language {ContentLanguage.PreferredCulture.Name}");
            }

            return new ConnectionConfiguration
            {
                CheckoutApiUri = configuration.CheckoutApiUri,
                PaymentAdminApiUri = configuration.PaymentAdminApiUri,
                MerchantId = configuration.MerchantId,
                Secret = configuration.Secret
            };
        }
    }
}