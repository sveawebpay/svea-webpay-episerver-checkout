using System;
using EPiServer.Commerce.Order;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;

using Svea.WebPay.SDK.CheckoutApi;
using Svea.WebPay.SDK.PaymentAdminApi.Models;
using Svea.WebPay.SDK.PaymentAdminApi.Request;

using System.Collections.Generic;
using System.Globalization;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public interface IRequestFactory
    {
        CreateOrderModel GetOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto, CultureInfo currentLanguage);
        UpdateOrderModel GetUpdateOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto,  CultureInfo currentLanguage, string merchantData = null);
        DeliveryRequest GetDeliveryRequest(IPayment payment, IMarket market, IShipment shipment, Order paymentOrder, TimeSpan? pollingTimeout = null);
        CancelRequest GetCancelRequest();
        
        CreditOrderRowsRequest GetCreditOrderRowsRequest(Order paymentOrder, IPayment payment, IEnumerable<ILineItem> lineItems, IMarket market, IShipment shipment, TimeSpan? pollingTimeout = null);
        CreditNewOrderRowRequest GetCreditNewOrderRowRequest(IPayment payment, IShipment shipment, string transactionDescription, TimeSpan? pollingTimeout = null);
        CreditAmountRequest GetCreditAmountRequest(IPayment payment, IShipment shipment);
        CancelAmountRequest GetCancelAmountRequest(Order paymentOrder, IPayment payment, IShipment shipment);
    }
}