using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Calculator;
using EPiServer.Core;
using EPiServer.ServiceLocation;

using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Orders;

using System.Collections.Generic;
using System.Linq;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    [ServiceConfiguration(typeof(ILineItemTaxCalculator))]
    public class LineItemTaxCalculator : DefaultTaxCalculator, ILineItemTaxCalculator
    {
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;

        public LineItemTaxCalculator(
            IContentRepository contentRepository,
            ReferenceConverter referenceConverter) : base(contentRepository, referenceConverter)
        {
            _contentRepository = contentRepository;
            _referenceConverter = referenceConverter;
        }

        public decimal PriceIncludingTaxAmount(decimal basePrice, decimal taxAmount, IMarket market)
        {
            return market.PricesIncludeTax ? basePrice : basePrice + taxAmount;
        }

        public decimal PriceIncludingTaxPercent(decimal basePrice, decimal taxPercent, IMarket market)
        {
            return market.PricesIncludeTax ? basePrice : basePrice * taxPercent * 0.01m + basePrice;
        }

        public decimal PriceIncludingTax(decimal basePrice, IEnumerable<ITaxValue> taxes, TaxType taxType)
        {
            return basePrice + GetTaxes(basePrice, taxes, taxType);
        }

        public IEnumerable<ITaxValue> GetTaxValuesForLineItem(ILineItem lineItem, IMarket market, IShipment shipment)
        {
            if (TryGetTaxCategoryId(lineItem, out int taxCategoryId))
            {
                var categoryNameById = CatalogTaxManager.GetTaxCategoryNameById(taxCategoryId);
                var taxValues = base.GetTaxValues(categoryNameById, market.DefaultLanguage.Name, shipment.ShippingAddress);
                return taxValues;
            }
            return Enumerable.Empty<ITaxValue>();
        }

        public decimal GetTaxes(decimal basePrice, IEnumerable<ITaxValue> taxes, TaxType taxType)
        {
            return taxes
                .Where(x => x.TaxType == taxType)
                .Sum(x => basePrice * (decimal)x.Percentage * 0.01m);
        }

        public bool TryGetTaxCategoryId(ILineItem item, out int taxCategoryId)
        {
            var contentLink = _referenceConverter.GetContentLink(item.Code);
            if (ContentReference.IsNullOrEmpty(contentLink))
            {
                taxCategoryId = 0;
                return false;
            }
            var pricing = _contentRepository.Get<EntryContentBase>(contentLink) as IPricing;
            if (pricing?.TaxCategoryId == null)
            {
                taxCategoryId = 0;
                return false;
            }
            taxCategoryId = pricing.TaxCategoryId.Value;
            return true;
        }
    }
}