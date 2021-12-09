using Svea.WebPay.SDK;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public class Prices
    {
        public MinorUnit UnitPrice { get; }
        public MinorUnit TaxRate { get; }
        public MinorUnit TotalDiscountAmount { get; }
        public MinorUnit TotalAmount { get; }
        public MinorUnit TotalTaxAmount { get; }

        public Prices(MinorUnit unitPrice, MinorUnit taxRate, MinorUnit totalDiscountAmount, MinorUnit totalAmount, MinorUnit totalTaxAmount)
        {
            UnitPrice = unitPrice;
            TaxRate = taxRate;
            TotalDiscountAmount = totalDiscountAmount;
            TotalAmount = totalAmount;
            TotalTaxAmount = totalTaxAmount;
        }
    }
}