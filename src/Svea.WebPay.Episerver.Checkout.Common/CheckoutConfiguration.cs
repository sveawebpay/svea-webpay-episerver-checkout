using System;
using System.Collections.Generic;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public class CheckoutConfiguration : ConnectionConfiguration
    {
        public Uri PushUri { get; set; }
        public Uri TermsUri { get; set; }
        public Uri CheckoutUri { get; set; }
        public Uri ConfirmationUri { get; set; }
        public Uri CheckoutValidationCallbackUri { get; set; }
        public List<long> ActivePartPaymentCampaigns { get; set; }
        public long? PromotedPartPaymentCampaign { get; set; }
    }
}
