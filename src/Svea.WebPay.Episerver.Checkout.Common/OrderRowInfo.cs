using Svea.WebPay.SDK;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public class OrderRowInfo
    {
        public OrderLineType Type { get; set; }
        /// <summary>
        /// Article number, SKU or similar.
        /// </summary>
        public string ArticleNumber { get; set; }
        /// <summary>
        /// Descriptive item name.
        /// </summary>
        /// <remarks>Required</remarks>
        public string Name { get; set; }
        /// <summary>
        /// Non-negative. The item quantity.
        /// </summary>
        /// <remarks>Required</remarks>
        public MinorUnit Quantity { get; set; }
        /// <summary>
        /// Unit used to describe the quantity, e.g. kg, pcs... If defined has to be 1-8 characters
        /// </summary>
        public string QuantityUnit { get; set; }
        /// <summary>
        /// Minor units. Includes tax, excludes discount. (max value: 100000000)
        /// </summary>
        /// <remarks>Required</remarks>
        public MinorUnit UnitPrice { get; set; }
        /// <summary>
        /// Non-negative. In percent, two implicit decimals. I.e 2500 = 25%. (max value: 10000)
        /// </summary>
        /// <remarks>Required</remarks>
        public MinorUnit TaxRate { get; set; }
        /// <summary>
        /// Includes tax and discount. Must match (quantity * unit_price) - total_discount_amount within ±quantity. (max value: 100000000)
        /// </summary>
        /// <remarks>Required</remarks>
        public MinorUnit TotalAmount { get; set; }
        /// <summary>
        /// Non-negative minor units. Includes tax.
        /// </summary>
        public MinorUnit TotalDiscountAmount { get; set; }
        /// <summary>
        /// Must be within ±1 of total_amount - total_amount * 10000 / (10000 + tax_rate). Negative when type is discount.
        /// </summary>
        /// <remarks>Required</remarks>
        public MinorUnit TotalTaxAmount { get; set; }
    }
}