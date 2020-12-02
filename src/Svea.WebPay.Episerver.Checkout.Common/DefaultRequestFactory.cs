using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using EPiServer.Web;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

using Svea.WebPay.Episerver.Checkout.Common.Helpers;
using Svea.WebPay.SDK;
using Svea.WebPay.SDK.CheckoutApi;
using Svea.WebPay.SDK.PaymentAdminApi.Models;
using Svea.WebPay.SDK.PaymentAdminApi.Request;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Svea.WebPay.Episerver.Checkout.Common.Extensions;
using OrderRow = Svea.WebPay.SDK.CheckoutApi.OrderRow;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    [ServiceConfiguration(typeof(IRequestFactory))]
    public class DefaultRequestFactory : IRequestFactory
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IShippingCalculator _shippingCalculator;
        
        private readonly IReturnLineItemCalculator _returnLineItemCalculator;
        
        public DefaultRequestFactory(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IOrderGroupCalculator orderGroupCalculator,
            IShippingCalculator shippingCalculator,
            IReturnLineItemCalculator returnLineItemCalculator)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader ??
                                           throw new ArgumentNullException(nameof(checkoutConfigurationLoader));
            _orderGroupCalculator = orderGroupCalculator;
            _shippingCalculator = shippingCalculator;
            _returnLineItemCalculator = returnLineItemCalculator;
        }

        public virtual CreateOrderModel GetOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto, CultureInfo currentLanguage)
        {
            if (orderGroup == null)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if (market == null)
            {
                throw new ArgumentNullException(nameof(market));
            }

            var configuration = _checkoutConfigurationLoader.GetConfiguration(market.MarketId, currentLanguage.TwoLetterISOLanguageName);

            List<OrderRow> orderRows = new List<OrderRow>();
            foreach (var orderGroupForm in orderGroup.Forms)
            {
                foreach (var shipment in orderGroupForm.Shipments)
                {
                    orderRows.AddRange(GetOrderRowItems(market, orderGroup.Currency, shipment.ShippingAddress, shipment.LineItems));
                    orderRows.Add(GetShippingOrderItem(orderGroup, shipment, market));
                }
            }

            var clientOrderNumber = DateTime.Now.Ticks.ToString();

            return new CreateOrderModel(new RegionInfo(CountryCodeHelper.GetTwoLetterCountryCode(market.MarketId.Value)), new CurrencyCode(orderGroup.Currency.CurrencyCode),
                new Language(currentLanguage.TextInfo.CultureName), clientOrderNumber,
                GetMerchantSettings(configuration, orderGroup, clientOrderNumber),
                new SDK.CheckoutApi.Cart(orderRows));
        }

        public virtual UpdateOrderModel GetUpdateOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto,
            CultureInfo currentLanguage, string merchantData = null)
        {
            if (orderGroup == null)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if (market == null)
            {
                throw new ArgumentNullException(nameof(market));
            }

            List<OrderRow> orderRows = new List<OrderRow>();
            foreach (var orderGroupForm in orderGroup.Forms)
            {
                foreach (var shipment in orderGroupForm.Shipments)
                {
                    orderRows.AddRange(GetOrderRowItems(market, orderGroup.Currency, shipment.ShippingAddress, shipment.LineItems));
                    orderRows.Add(GetShippingOrderItem(orderGroup, shipment, market));
                }
            }

            return new UpdateOrderModel(new SDK.CheckoutApi.Cart(orderRows), merchantData);
        }

        public virtual CreditOrderRowsRequest GetCreditOrderRowsRequest(Order paymentOrder, IPayment payment, IEnumerable<ILineItem> lineItems, IMarket market, IShipment shipment, TimeSpan? pollingTimeout = null)
        {
            var orderRows = paymentOrder.OrderRows.Where(x => lineItems.Any(lineItem => lineItem.Code == x.ArticleNumber));
            var orderRowIds = orderRows.Select(row => Convert.ToInt64(row.OrderRowId)).ToList();
            return new CreditOrderRowsRequest(orderRowIds, pollingTimeout);
        }

        public virtual CreditNewOrderRowRequest GetCreditNewOrderRowRequest(IPayment payment, IShipment shipment, string name, TimeSpan? pollingTimeout = null)
        {
            var creditOrderRow = new CreditOrderRow(name, MinorUnit.FromDecimal(payment.Amount), MinorUnit.FromDecimal(0)); //TODO VATPercent
            var creditNewOrderRowRequest = new CreditNewOrderRowRequest(creditOrderRow, null, pollingTimeout); //TODO: newCreditOrderRows?
            return creditNewOrderRowRequest;
        }

        public virtual CreditAmountRequest GetCreditAmountRequest(IPayment payment, IShipment shipment)
        {
            return new CreditAmountRequest(MinorUnit.FromDecimal(payment.Amount));
        }

        public virtual CancelAmountRequest GetCancelAmountRequest(Order paymentOrder, IPayment payment, IShipment shipment)
        {
            return new CancelAmountRequest(MinorUnit.FromDecimal(MinorUnit.ToDecimal(paymentOrder.CancelledAmount) + payment.Amount));
        }

        public virtual DeliveryRequest GetDeliveryRequest(IPayment payment, IMarket market, IShipment shipment, Order paymentOrder, TimeSpan? pollingTimeout = null)
        {
            var lineItemCodes = shipment.LineItems.Select(lineItem => lineItem.Code);
            var orderRowIds = paymentOrder.OrderRows
                .Where(row => lineItemCodes.Contains(row.ArticleNumber))
                .Select(r => Convert.ToInt64(r.OrderRowId))
                .ToList();

            var shippingOrderRow = paymentOrder.OrderRows.FirstOrDefault(x => x.ArticleNumber == "SHIPPING");
            if (shippingOrderRow != null)
            {
                orderRowIds.Add(Convert.ToInt64(shippingOrderRow.OrderRowId));
            }

            return new DeliveryRequest(orderRowIds, null, pollingTimeout);
        }

        public virtual CancelRequest GetCancelRequest()
        {
            return new CancelRequest(true);
        }

        public virtual IEnumerable<OrderRow> GetOrderRowItems(IMarket market, Currency currency, IOrderAddress shippingAddress, IEnumerable<ILineItem> lineItems)
        {
            return lineItems.Select(item =>
            {
                var extendedPrice = item.ReturnQuantity > 0 ? _returnLineItemCalculator.GetExtendedPrice(item as IReturnLineItem, currency) : item.GetExtendedPrice(currency);

                var itemSalesTex = item.GetSalesTax(market, currency, shippingAddress);
                var vatPercent = extendedPrice.Amount > 0
                    ? market.PricesIncludeTax
                        ? itemSalesTex.Amount / (extendedPrice.Amount - itemSalesTex.Amount)
                        : itemSalesTex.Amount / extendedPrice.Amount
                    : 0;
                var unitSalesTax = item.PlacedPrice * vatPercent;
                var unitPrice = market.PricesIncludeTax ? item.PlacedPrice : item.PlacedPrice + unitSalesTax;

                var discountPercent = (item.GetEntryDiscount() / (item.GetDiscountedPrice(currency) + item.GetEntryDiscount()));

                return new OrderRow(item.Code, item.DisplayName.TrimIfNecessary(40), MinorUnit.FromDecimal(item.Quantity), MinorUnit.FromDecimal(unitPrice),
                    MinorUnit.FromDecimal(discountPercent * 100), MinorUnit.FromDecimal(vatPercent * 100), "PCS", null, item.LineItemId, null);
            });
        }

        public virtual OrderRow GetShippingOrderItem(IOrderGroup orderGroup, IShipment shipment, IMarket market)
        {
            var currency = shipment.ParentOrderGroup.Currency;
            var extendPrice = _orderGroupCalculator.GetShippingSubTotal(orderGroup);
            var discountedShippingAmount = _shippingCalculator.GetDiscountedShippingAmount(shipment, market, currency);

            var shippingTax = _shippingCalculator.GetShippingTax(shipment, market, currency);
            var vatPercent = discountedShippingAmount > 0
                ? market.PricesIncludeTax
                    ? shippingTax.Amount / (discountedShippingAmount - shippingTax.Amount)
                    : shippingTax.Amount / discountedShippingAmount
                : 0;

            var unitShippingTax = extendPrice * vatPercent;
            var unitPrice = market.PricesIncludeTax ? extendPrice : extendPrice + unitShippingTax;
            var discountPercent = extendPrice.Amount > 0 ? (extendPrice.Amount - discountedShippingAmount.Amount) / extendPrice.Amount : 0;

            var shippingMethodInfoModel = ShippingManager.GetShippingMethod(shipment.ShippingMethodId).ShippingMethod.Single();
            return new OrderRow("SHIPPING", shippingMethodInfoModel.DisplayName.TrimIfNecessary(40), MinorUnit.FromInt(1), MinorUnit.FromDecimal(unitPrice.Amount),
                MinorUnit.FromDecimal(discountPercent * 100), MinorUnit.FromDecimal(vatPercent * 100),
                "PCS", null, 999, null);
        }

        private MerchantSettings GetMerchantSettings(CheckoutConfiguration checkoutConfiguration, IOrderGroup orderGroup, string payeeReference)
        {
            Uri ToFullSiteUrl(Func<CheckoutConfiguration, Uri> fieldSelector)
            {
                if (fieldSelector(checkoutConfiguration) == null)
                    return null;
                var url = fieldSelector(checkoutConfiguration).OriginalString
                    .Replace("{orderGroupId}", orderGroup.OrderLink.OrderGroupId.ToString())
                    .Replace("{payeeReference}", payeeReference);

                var uriBuilder = new UriBuilder(url);
                return !uriBuilder.Uri.IsAbsoluteUri ? new Uri(SiteDefinition.Current.SiteUrl, uriBuilder.Uri.PathAndQuery) : uriBuilder.Uri;
            }

            return new MerchantSettings(ToFullSiteUrl(c => c.PushUri), ToFullSiteUrl(c => c.TermsUri),
                ToFullSiteUrl(c => c.CheckoutUri), ToFullSiteUrl(c => c.ConfirmationUri),
                ToFullSiteUrl(c => c.CheckoutValidationCallbackUri), checkoutConfiguration.ActivePartPaymentCampaigns, checkoutConfiguration.PromotedPartPaymentCampaign);
        }
    }
}