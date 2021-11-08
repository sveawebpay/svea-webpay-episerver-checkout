using EPiServer.Commerce.Order;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;

using Svea.WebPay.SDK.CheckoutApi;
using Svea.WebPay.SDK.PaymentAdminApi.Models;
using Svea.WebPay.SDK.PaymentAdminApi.Request;

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public interface IRequestFactory
    {
        CreateOrderModel GetOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null);
        UpdateOrderModel GetUpdateOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, string merchantData = null);
        CreditOrderRowsRequest GetCreditOrderRowsRequest(Delivery delivery, IShipment shipment);
        CreditNewOrderRowRequest GetCreditNewOrderRowRequest(OrderForm returnForm, IPayment payment, IShipment shipment, IMarket market, Currency currency);
        CreditAmountRequest GetCreditAmountRequest(IPayment payment, IShipment shipment);
        CancelAmountRequest GetCancelAmountRequest(Order paymentOrder, IPayment payment, IShipment shipment);
        DeliveryRequest GetDeliveryRequest(IPayment payment, IMarket market, IShipment shipment, Order paymentOrder);
        CancelRequest GetCancelRequest();
    }
}