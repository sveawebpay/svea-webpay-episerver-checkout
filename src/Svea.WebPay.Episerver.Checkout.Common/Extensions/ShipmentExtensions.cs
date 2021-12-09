using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;

using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders.Managers;

using Svea.WebPay.SDK;

using System.Linq;
using EPiServer.Framework.Localization;

namespace Svea.WebPay.Episerver.Checkout.Common.Extensions
{
    public static class ShipmentExtensions
    {
#pragma warning disable 649
        private static Injected<IShippingCalculator> _shippingCalculator;
        private static Injected<IMarketService> _marketService;
#pragma warning restore 649

        public static OrderRowInfo GetOrderRow(this IShipment shipment, IOrderGroup orderGroup, bool includeTaxes)
        {
            var market = _marketService.Service.GetMarket(orderGroup.MarketId);
            var shippingCost = shipment.GetShippingCost(market, orderGroup.Currency);
            var shipmentDiscountPrice = shipment.GetShipmentDiscountPrice(orderGroup.Currency);

            var total = shippingCost.Amount - shipmentDiscountPrice.Amount;
            var unitPrice = shippingCost.Amount;
            var shippingTax = 0m;
            var taxRate = 0m;


            if (includeTaxes)
            {
                shippingTax = _shippingCalculator.Service.GetShippingTax(shipment, market, orderGroup.Currency).Amount;

                var shippingTotalExcludingTax = market.PricesIncludeTax
                    ? shippingCost - shippingTax
                    : shippingCost;

                taxRate = shippingTax * 100 / shippingTotalExcludingTax;

                if (!market.PricesIncludeTax)
                {
                    total += shippingTax;
                    unitPrice += shippingTax;
                }
            }

            var shipmentOrderLine = new OrderRowInfo
            {
                Type = OrderLineType.ShippingFee,
                Name = shipment.ShippingMethodName,
                ArticleNumber = "SHIPPING",
                Quantity = new MinorUnit(1),
                UnitPrice = new MinorUnit(unitPrice),
                TotalAmount = new MinorUnit(total),
                TaxRate = new MinorUnit(taxRate),
                TotalTaxAmount = new MinorUnit(shippingTax),
                QuantityUnit = LocalizationService.Current.GetString("/svea/orderrow/shippingpcs", "pcs"),
                TotalDiscountAmount = new MinorUnit(shipmentDiscountPrice.Amount)
            };

            if (string.IsNullOrEmpty(shipmentOrderLine.Name))
            {
                var shipmentMethod = ShippingManager.GetShippingMethod(shipment.ShippingMethodId).ShippingMethod.FirstOrDefault();
                if (shipmentMethod != null)
                {
                    shipmentOrderLine.Name = shipmentMethod.DisplayName;
                }
            }

            return shipmentOrderLine;
        }
    }
}