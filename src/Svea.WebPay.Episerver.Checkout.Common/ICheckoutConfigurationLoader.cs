﻿using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public interface ICheckoutConfigurationLoader
    {
        CheckoutConfiguration GetConfiguration(MarketId marketId, string languageId);

        CheckoutConfiguration GetConfiguration(PaymentMethodDto paymentMethodDto, MarketId marketId);

        void SetConfiguration(CheckoutConfiguration configuration, PaymentMethodDto paymentMethod, string currentMarket);
    }
}
