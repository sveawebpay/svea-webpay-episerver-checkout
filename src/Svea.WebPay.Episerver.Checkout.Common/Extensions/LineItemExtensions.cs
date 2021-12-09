using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;

using Svea.WebPay.SDK;

using System.Linq;

namespace Svea.WebPay.Episerver.Checkout.Common.Extensions
{
    public static class LineItemExtensions
    {
#pragma warning disable 649
        private static Injected<ILineItemTaxCalculator> _lineItemTaxCalculator;
        private static Injected<ITaxCalculator> _taxCalculator;
#pragma warning restore 649

        public static OrderRowInfo GetOrderRow(this ILineItem lineItem)
        {
            return GetOrderRow(
                lineItem,
                new MinorUnit(lineItem.PlacedPrice),
                new MinorUnit(lineItem.PlacedPrice * lineItem.Quantity) - new MinorUnit(lineItem.GetEntryDiscount()),
                new MinorUnit(lineItem.GetEntryDiscount()), new MinorUnit(0), new MinorUnit(0));
        }


        public static OrderRowInfo GetOrderRowWithTax(
            this ILineItem lineItem,
            IMarket market,
            IShipment shipment,
            Currency currency)
        {
            var prices = lineItem.GetPrices(market, shipment, currency);
            return GetOrderRow(
                lineItem,
                prices.UnitPrice,
                prices.TotalAmount,
                prices.TotalDiscountAmount,
                prices.TotalTaxAmount,
                prices.TaxRate);
        }


        private static OrderRowInfo GetOrderRow(
            ILineItem lineItem,
            MinorUnit unitPrice,
            MinorUnit totalAmount,
            MinorUnit totalDiscountAmount,
            MinorUnit totalTaxAmount,
            MinorUnit taxRate)
        {
            var orderLine = new OrderRowInfo
            {
                Quantity = new MinorUnit(lineItem.Quantity),
                QuantityUnit = LocalizationService.Current.GetString("/svea/orderrow/physicalpcs", "pcs"),
                Name = lineItem.DisplayName,
                ArticleNumber = lineItem.Code.TrimIfNecessary(40),
                Type = OrderLineType.Physical
            };

            if (string.IsNullOrEmpty(orderLine.Name))
            {
                var entry = lineItem.GetEntryContent();
                if (entry != null)
                {
                    orderLine.Name = entry.DisplayName;
                }
            }

            orderLine.UnitPrice = unitPrice;
            orderLine.TotalAmount = totalAmount;
            orderLine.TotalDiscountAmount = totalDiscountAmount;
            orderLine.TotalTaxAmount = totalTaxAmount;
            orderLine.TaxRate = taxRate;

            return orderLine;
        }

        public static Prices GetPrices(this ILineItem lineItem, IMarket market, IShipment shipment, Currency currency)
        {
            var taxType = TaxType.SalesTax;

            // All excluding tax
            var unitPrice = lineItem.PlacedPrice;
            var totalPriceWithoutDiscount = lineItem.PlacedPrice * lineItem.Quantity;
            var extendedPrice = lineItem.GetDiscountedPrice(currency);
            var discountAmount = (totalPriceWithoutDiscount - extendedPrice);

            // Tax value
            var taxValues = _lineItemTaxCalculator.Service.GetTaxValuesForLineItem(lineItem, market, shipment);
            var taxPercentage = taxValues
                .Where(x => x.TaxType == taxType)
                .Sum(x => (decimal)x.Percentage);

            // Using ITaxCalculator instead of ILineItemCalculator because ILineItemCalculator
            // calculates tax from the price which includes order discount amount and line item discount amount
            // but should use only line item discount amount
            var salesTax = _taxCalculator.Service.GetSalesTax(lineItem, market, shipment.ShippingAddress, extendedPrice);

            // Includes tax, excludes discount.
            var unitPriceIncludingTax = new MinorUnit(_lineItemTaxCalculator.Service.PriceIncludingTaxPercent(unitPrice, taxPercentage, market));
            // Non - negative minor units. Includes tax
            var totalDiscountAmount = new MinorUnit(_lineItemTaxCalculator.Service.PriceIncludingTaxPercent(discountAmount, taxPercentage, market));
            // Includes tax and discount. Must match (quantity * unit_price) - total_discount_amount within quantity.
            var totalAmount = new MinorUnit(_lineItemTaxCalculator.Service.PriceIncludingTaxAmount(extendedPrice, salesTax.Amount, market));

            // Non-negative. In percent, two implicit decimals. I.e 2500 = 25%.
            var taxRate = new MinorUnit(taxPercentage);
            var totalTaxAmount = new MinorUnit(salesTax.Amount);

            return new Prices(unitPriceIncludingTax, taxRate, totalDiscountAmount, totalAmount, totalTaxAmount);
        }
    }
}