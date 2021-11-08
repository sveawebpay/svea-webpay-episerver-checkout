using System.Collections.Generic;

using EPiServer.Commerce.Order;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public interface ILineItemTaxCalculator
    {
        decimal PriceIncludingTax(decimal basePrice, IEnumerable<ITaxValue> taxes, TaxType taxType);
        decimal PriceIncludingTaxPercent(decimal basePrice, decimal taxPercent, IMarket market);
        decimal PriceIncludingTaxAmount(decimal basePrice, decimal taxAmount, IMarket market);
        IEnumerable<ITaxValue> GetTaxValuesForLineItem(ILineItem lineItem, IMarket market, IShipment shipment);
        decimal GetTaxes(decimal basePrice, IEnumerable<ITaxValue> taxes, TaxType taxType);
        bool TryGetTaxCategoryId(ILineItem item, out int taxCategoryId);
    }
}