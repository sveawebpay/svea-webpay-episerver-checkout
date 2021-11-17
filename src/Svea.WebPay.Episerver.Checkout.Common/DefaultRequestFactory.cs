using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using EPiServer.Web;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;

using Svea.WebPay.Episerver.Checkout.Common.Extensions;
using Svea.WebPay.Episerver.Checkout.Common.Helpers;
using Svea.WebPay.SDK;
using Svea.WebPay.SDK.CheckoutApi;
using Svea.WebPay.SDK.PaymentAdminApi;
using Svea.WebPay.SDK.PaymentAdminApi.Models;
using Svea.WebPay.SDK.PaymentAdminApi.Request;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Cart = Svea.WebPay.SDK.CheckoutApi.Cart;
using OrderRow = Svea.WebPay.SDK.CheckoutApi.OrderRow;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    [ServiceConfiguration(typeof(IRequestFactory))]
    public class DefaultRequestFactory : IRequestFactory
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly ILineItemTaxCalculator _lineItemTaxCalculator;
        private readonly LocalizationService _localizationService;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IReturnOrderFormCalculator _returnOrderFormCalculator;

        public DefaultRequestFactory(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            ILineItemTaxCalculator lineItemTaxCalculator,
            LocalizationService localizationService,
            IOrderGroupCalculator orderGroupCalculator,
            IReturnOrderFormCalculator returnOrderFormCalculator)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader ?? throw new ArgumentNullException(nameof(checkoutConfigurationLoader));
            _lineItemTaxCalculator = lineItemTaxCalculator;
            _localizationService = localizationService;
            _orderGroupCalculator = orderGroupCalculator;
            _returnOrderFormCalculator = returnOrderFormCalculator;
        }

        public virtual CreateOrderModel GetOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null)
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
            var orderRows = GetOrderRows(orderGroup, market, includeTaxOnLineItems, temporaryReference, merchantData);
            var clientOrderNumber = DateTime.Now.Ticks.ToString();

            return new CreateOrderModel(new RegionInfo(CountryCodeHelper.GetTwoLetterCountryCode(market.MarketId.Value)), new CurrencyCode(orderGroup.Currency.CurrencyCode),
                new Language(currentLanguage.TextInfo.CultureName), clientOrderNumber,
                GetMerchantSettings(configuration, orderGroup, clientOrderNumber),
                new Cart(orderRows), configuration.RequireElectronicIdAuthentication, presetValues, identityFlags, partnerKey, merchantData);
        }

        public virtual UpdateOrderModel GetUpdateOrderRequest(IOrderGroup orderGroup, IMarket market, PaymentMethodDto paymentMethodDto,
            CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, string merchantData = null)
        {
            if (orderGroup == null)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if (market == null)
            {
                throw new ArgumentNullException(nameof(market));
            }

            var orderRows = GetOrderRows(orderGroup, market, includeTaxOnLineItems, temporaryReference, merchantData);
            return new UpdateOrderModel(new Cart(orderRows), merchantData);
        }

        public virtual List<OrderRow> GetOrderRows(IOrderGroup orderGroup, IMarket market, string temporaryReference = null, string merchantData = null)
        {
            return GetOrderRows(orderGroup, market, market.PricesIncludeTax, temporaryReference, merchantData);
        }

        public virtual List<OrderRow> GetOrderRows(IOrderGroup orderGroup, IMarket market, bool includeTaxOnLineItems, string temporaryReference = null, string merchantData = null)
        {
            var orderGroupTotals = _orderGroupCalculator.GetOrderGroupTotals(orderGroup);
            return includeTaxOnLineItems
                ? GetOrderRowsWithTax(orderGroup, market, orderGroupTotals, temporaryReference, merchantData)
                : GetOrderRowsWithoutTax(orderGroup, orderGroupTotals, temporaryReference, merchantData);
        }

        public virtual CreditOrderRowsRequest GetCreditOrderRowsRequest(Delivery delivery, IShipment shipment)
        {
            var creditOrderRows = shipment.LineItems
                .Select(lineItem =>
                {
                    var orderRow = delivery.OrderRows.FirstOrDefault(x =>
                        x.AvailableActions.Contains(OrderRowActionType.CanCreditRow) &&
                        x.ArticleNumber == lineItem.Code);
                    if (orderRow != null)
                    {
                        return new RowCreditingOptions(orderRow.OrderRowId, new MinorUnit(lineItem.ReturnQuantity));
                    }

                    return null;
                }).Where(x => x != null).ToList();

            return new CreditOrderRowsRequest(creditOrderRows.Select(x => x.OrderRowId).ToList(), creditOrderRows);
        }

        public virtual CreditNewOrderRowRequest GetCreditNewOrderRowRequest(OrderForm orderForm, IPayment payment, IShipment shipment, IMarket market, Currency currency)
        {
            var transactionDescription = string.IsNullOrWhiteSpace(orderForm.ReturnComment)
                ? "Crediting payment."
                : orderForm.ReturnComment;

            var taxPercentage = GetTaxPercentage(shipment, market);
            
            var creditOrderRows = orderForm.LineItems.Select(l =>
            {
                var prices = l.GetPrices(market, shipment, currency);
                return new CreditOrderRow(l.DisplayName, prices.UnitPrice - (prices.TotalDiscountAmount / l.ReturnQuantity), prices.TaxRate, new MinorUnit(l.ReturnQuantity));
            }).ToList();
            
            var orderDiscountTotal = _returnOrderFormCalculator.GetOrderDiscountTotal(orderForm, currency);
            if (orderDiscountTotal.Amount > 0)
            {
                var orderDiscountWithTax = _lineItemTaxCalculator.PriceIncludingTaxPercent(orderDiscountTotal, taxPercentage, market);
                creditOrderRows.Add(new CreditOrderRow("DISCOUNT", new MinorUnit(currency.Round(orderDiscountWithTax) * -1), new MinorUnit(taxPercentage)));
            }

            var creditOrderRowSum = creditOrderRows.Sum(o => o.UnitPrice * o.Quantity);
            if (payment.Amount > creditOrderRowSum)
            {
                var restSum = payment.Amount - creditOrderRowSum;
                creditOrderRows.Add(new CreditOrderRow("CREDIT", new MinorUnit(restSum), new MinorUnit(taxPercentage)));
            }

            var creditOrderRow = new CreditOrderRow(transactionDescription, new MinorUnit(payment.Amount), new MinorUnit(taxPercentage));
            var creditNewOrderRowRequest = new CreditNewOrderRowRequest(creditOrderRow, creditOrderRows);
            return creditNewOrderRowRequest;
        }

        private decimal GetTaxPercentage(IShipment shipment, IMarket market)
        {
            var taxValues = _lineItemTaxCalculator.GetTaxValuesForLineItem(shipment.LineItems.First(), market, shipment);
            var taxPercentage = taxValues
                .Where(x => x.TaxType == TaxType.SalesTax)
                .Sum(x => (decimal)x.Percentage);
            return taxPercentage;
        }

        public virtual CreditAmountRequest GetCreditAmountRequest(IPayment payment, IShipment shipment)
        {
            return new CreditAmountRequest(new MinorUnit(payment.Amount));
        }

        public virtual CancelAmountRequest GetCancelAmountRequest(Order paymentOrder, IPayment payment, IShipment shipment)
        {
            return new CancelAmountRequest(new MinorUnit(paymentOrder.CancelledAmount + payment.Amount));
        }

        public virtual DeliveryRequest GetDeliveryRequest(IPayment payment, IMarket market, IShipment shipment, Order paymentOrder)
        {
            var orderRowIds = paymentOrder.OrderRows.Select(x => Convert.ToInt64(x.OrderRowId)).ToList();
            return new DeliveryRequest(orderRowIds, null, null);
        }

        public virtual CancelRequest GetCancelRequest()
        {
            return new CancelRequest(true);
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


        private List<OrderRow> GetOrderRowsWithoutTax(IOrderGroup orderGroup, OrderGroupTotals orderGroupTotals, string temporaryReference = null, string merchantData = null)
        {
            var orderRowInfos = new List<OrderRowInfo>();

            // Line items
            foreach (var lineItem in orderGroup.GetAllLineItems())
            {
                var orderLine = lineItem.GetOrderRow();
                orderRowInfos.Add(orderLine);
            }

            // Shipment
            if (orderGroupTotals.ShippingTotal.Amount > 0)
            {
                foreach (var form in orderGroup.Forms)
                {
                    foreach (var shipment in form.Shipments)
                    {
                        var shipmentOrderLine = shipment.GetOrderRow(orderGroup, false);
                        orderRowInfos.Add(shipmentOrderLine);
                    }
                }
            }

            // Sales tax
            orderRowInfos.Add(new OrderRowInfo
            {
                Type = OrderLineType.SalesTax,
                Name = "Sales Tax",
                Quantity = new MinorUnit(1),
                TotalAmount = new MinorUnit(orderGroupTotals.TaxTotal),
                UnitPrice = new MinorUnit(orderGroupTotals.TaxTotal),
                TotalTaxAmount = new MinorUnit(0),
                TaxRate = new MinorUnit(0)
            });

            // Order level discounts
            var orderDiscount = orderGroup.GetOrderDiscountTotal();

            var totalDiscount = orderDiscount.Amount;

            if (totalDiscount > 0)
            {
                orderRowInfos.Add(new OrderRowInfo
                {
                    ArticleNumber = "DISCOUNT",
                    Type = OrderLineType.Discount,
                    Name = _localizationService.GetString("/svea/orderrow/discount", "Discount"),
                    Quantity = new MinorUnit(1),
                    TotalAmount = -new MinorUnit(totalDiscount),
                    UnitPrice = -new MinorUnit(totalDiscount),
                    TotalTaxAmount = new MinorUnit(0),
                    TaxRate = new MinorUnit(0),
                    QuantityUnit = _localizationService.GetString("/svea/orderrow/discountpcs", "pcs")
                });
            }


            var retVal = new List<OrderRow>();
            for (var rowNumber = 0; rowNumber < orderRowInfos.Count; rowNumber++)
            {
                var orderRowInfo = orderRowInfos[rowNumber];
                retVal.Add(new OrderRow(orderRowInfo.ArticleNumber.TrimIfNecessary(256), orderRowInfo.Name.TrimIfNecessary(40), orderRowInfo.Quantity, orderRowInfo.UnitPrice, orderRowInfo.TotalDiscountAmount,
                    orderRowInfo.TaxRate, orderRowInfo.QuantityUnit, temporaryReference, rowNumber + 1, merchantData));
            }
            return retVal;
        }

        private List<OrderRow> GetOrderRowsWithTax(IOrderGroup orderGroup, IMarket market, OrderGroupTotals orderGroupTotals, string temporaryReference = null, string merchantData = null)
        {
            var orderRowInfos = new List<OrderRowInfo>();

            // Line items
            foreach (var lineItem in orderGroup.GetAllLineItems())
            {
                var orderRow = lineItem.GetOrderRowWithTax(market, orderGroup.GetFirstShipment(), orderGroup.Currency);
                orderRowInfos.Add(orderRow);
            }

            // Shipment
            if (orderGroupTotals.ShippingTotal.Amount > 0)
            {
                foreach (var form in orderGroup.Forms)
                {
                    foreach (var shipment in form.Shipments)
                    {
                        var shipmentOrderLine = shipment.GetOrderRow(orderGroup, true);
                        orderRowInfos.Add(shipmentOrderLine);
                    }
                }
            }

            // Without tax
            var orderLevelDiscount = new MinorUnit(orderGroup.GetOrderDiscountTotal());
            if (orderLevelDiscount > 0)
            {
                // Order level discounts with tax
                var totalOrderAmountWithoutDiscount = orderRowInfos.Sum(x => x.TotalAmount);
                var totalOrderAmountWithDiscount = orderGroupTotals.Total.Amount;
                var orderLevelDiscountIncludingTax = totalOrderAmountWithoutDiscount - totalOrderAmountWithDiscount;

                // Tax
                var totalTaxAmountWithoutDiscount = orderRowInfos.Sum(x => x.TotalTaxAmount);
                var totalTaxAmountWithDiscount = orderGroupTotals.TaxTotal;
                var discountTax = totalTaxAmountWithoutDiscount - totalTaxAmountWithDiscount;

                var orderLevelDiscountExcludingTax = market.PricesIncludeTax
                    ? orderLevelDiscount - discountTax
                    : orderLevelDiscount;

                var taxRate = discountTax * 100 / orderLevelDiscountExcludingTax;

                orderRowInfos.Add(new OrderRowInfo
                {
                    ArticleNumber = "DISCOUNT",
                    Type = OrderLineType.Discount,
                    Name = _localizationService.GetString("/svea/orderrow/discount", "Discount"),
                    Quantity = new MinorUnit(1),
                    TotalAmount = new MinorUnit(orderLevelDiscountIncludingTax * -1),
                    UnitPrice = new MinorUnit(orderLevelDiscountIncludingTax * -1),
                    TotalTaxAmount = new MinorUnit(discountTax * -1),
                    TaxRate = new MinorUnit(taxRate),
                    QuantityUnit = _localizationService.GetString("/svea/orderrow/discountpcs", "pcs")
                });
            }

            var retVal = new List<OrderRow>();
            for (var rowNumber = 0; rowNumber < orderRowInfos.Count; rowNumber++)
            {
                var orderRowInfo = orderRowInfos[rowNumber];
                retVal.Add(new OrderRow(orderRowInfo.ArticleNumber.TrimIfNecessary(256), orderRowInfo.Name.TrimIfNecessary(40), orderRowInfo.Quantity, orderRowInfo.UnitPrice, orderRowInfo.TotalDiscountAmount,
                    orderRowInfo.TaxRate, orderRowInfo.QuantityUnit, temporaryReference, rowNumber + 1, merchantData));
            }
            return retVal;
        }
    }
}