﻿using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Calculator;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Framework;

using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Orders;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public class SveaWebPayTaxCalculator : DefaultTaxCalculator
    {
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;

        public SveaWebPayTaxCalculator(IContentRepository contentRepository, ReferenceConverter referenceConverter) : base(contentRepository, referenceConverter)
        {
            _contentRepository = contentRepository;
            _referenceConverter = referenceConverter;
        }

        protected override Money CalculateSalesTax(ILineItem lineItem, IMarket market, IOrderAddress shippingAddress, Money basePrice)
        {
            IEnumerable<ITaxValue> taxValues = GetTaxValues(lineItem, market, shippingAddress);
            if (!taxValues.Any())
            {
                return new Money(decimal.Zero, basePrice.Currency);
            }
            return CalculateLineItemSalesTax(lineItem, market, basePrice, taxValues);
        }

        protected override Money CalculateSalesTax(IEnumerable<ILineItem> lineItems, IMarket market, IOrderAddress shippingAddress, Currency currency)
        {
            foreach (ILineItem lineItem in lineItems)
            {
                lineItem.TaxCategoryId = new int?(GetTaxCategoryId(lineItem));
            }

            decimal amount = new decimal();
            foreach (IGrouping<int?, ILineItem> source in lineItems.GroupBy(x => x.TaxCategoryId, x => x))
            {
                IEnumerable<ITaxValue> taxValues = GetTaxValues(source.First(), market, shippingAddress);
                foreach (ILineItem lineItem in source)
                {
                    ILineItemCalculatedAmount calculatedAmount = lineItem as ILineItemCalculatedAmount;
                    if (calculatedAmount != null && calculatedAmount.IsSalesTaxUpToDate)
                    {
                        amount += calculatedAmount.SalesTax;
                    }
                    else
                    {
                        Money lineItemSalesTax = CalculateLineItemSalesTax(lineItem, market, GetExtendedPrice(lineItem, currency), taxValues);
                        ValidateSalesTax(lineItemSalesTax);

                        if (calculatedAmount != null)
                        {
                            calculatedAmount.SalesTax = lineItemSalesTax.Amount;
                            calculatedAmount.IsSalesTaxUpToDate = true;
                        }
                        amount += lineItemSalesTax.Amount;
                    }
                }
            }
            return new Money(amount, currency);
        }


        protected override Money CalculateShippingTax(ILineItem lineItem, IMarket market, IOrderAddress shippingAddress, Money basePrice)
        {
            IEnumerable<ITaxValue> taxValues = GetTaxValues(lineItem, market, shippingAddress);
            if (!taxValues.Any())
            {
                return new Money(decimal.Zero, basePrice.Currency);
            }

            bool flag = lineItem is ILineItemCalculatedAmount calculatedAmount ? calculatedAmount.PricesIncludeTax : market != null && market.PricesIncludeTax;
            decimal num = (decimal)taxValues.Where(x => x.TaxType == TaxType.ShippingTax).Sum(x => x.Percentage);
            var amount = basePrice.Amount * num / (flag ? num + new decimal(100) : new decimal(100));
            return new Money(Math.Round(amount, 2, MidpointRounding.AwayFromZero), basePrice.Currency);
        }

        private Money CalculateLineItemSalesTax(
            ILineItem lineItem,
            IMarket market,
            Money basePrice,
            IEnumerable<ITaxValue> taxValues)
        {
            bool flag = lineItem is ILineItemCalculatedAmount calculatedAmount ? calculatedAmount.PricesIncludeTax : market.PricesIncludeTax;
            decimal num = (decimal)taxValues.Where(x => x.TaxType == TaxType.SalesTax).Sum(x => x.Percentage);
            var amount = basePrice.Amount * num / (flag ? num + new decimal(100) : new decimal(100));
            return new Money(Math.Round(amount, 2, MidpointRounding.AwayFromZero), basePrice.Currency);
        }

        private IEnumerable<ITaxValue> GetTaxValues(
            ILineItem lineItem,
            IMarket market,
            IOrderAddress shippingAddress)
        {
            Validator.ThrowIfNull(nameof(lineItem), lineItem);
            Validator.ThrowIfNull(nameof(market), market);
            IEnumerable<ITaxValue> taxValues = Enumerable.Empty<ITaxValue>();
            if (shippingAddress == null)
            {
                return taxValues;
            }

            return GetTaxValues(GetTaxCategoryName(lineItem), market.DefaultLanguage.Name, shippingAddress);
        }

        public decimal GetTaxPercentage(ILineItem lineItem, IMarket market, IOrderAddress shippingAddress, TaxType taxType)
        {
            var taxValues = GetTaxValues(lineItem, market, shippingAddress).ToList();

            if (!taxValues.Any())
            {
                return 0;
            }

            var taxValue = taxValues.FirstOrDefault(x => x.TaxType  == taxType);
            return taxValue != null ? (decimal) taxValue.Percentage : 0;
        }

        public decimal GetTaxPercentage(IMarket market, IOrderAddress shippingAddress, TaxType taxType)
        {
            var taxValues = base.GetTaxValues("General Sales",market.DefaultLanguage.Name, shippingAddress).ToList();

            if (!taxValues.Any())
            {
                return 0;
            }

            var taxValue = taxValues.FirstOrDefault(x => x.TaxType == taxType);
            return taxValue != null ? (decimal)taxValue.Percentage : 0;
        }

        private int GetTaxCategoryId(ILineItem lineItem)
        {
            if (!lineItem.TaxCategoryId.HasValue)
            {
                return (lineItem.GetEntryContent(_referenceConverter, _contentRepository) as IPricing)?.TaxCategoryId ?? 0;
            }
            return lineItem.TaxCategoryId.Value;
        }
    }
}
