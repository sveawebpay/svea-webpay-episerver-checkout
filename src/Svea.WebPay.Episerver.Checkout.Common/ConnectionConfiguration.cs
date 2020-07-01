using System;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public class ConnectionConfiguration
    {
        public string MarketId { get; set; }
        public string MerchantId { get; set; }
        public string Secret { get; set; }
        public Uri CheckoutApiUri { get; set; }
        public Uri PaymentAdminApiUri { get; set; }
    }
}
