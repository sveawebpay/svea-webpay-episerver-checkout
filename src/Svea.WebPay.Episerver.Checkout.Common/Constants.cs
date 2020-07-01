namespace Svea.WebPay.Episerver.Checkout.Common
{
    public static class Constants
    {
        public static readonly string SveaWebPaySystemKeyword = "SveaWebPay";

        public static readonly string SveaWebPayCheckoutSystemKeyword = SveaWebPaySystemKeyword + "Checkout";

        // Payment method property fields
        public static readonly string SveaWebPaySerializedMarketOptions = "SveaWebPaySerializedMarketOptions";
        
        // Purchase order meta fields
        public static readonly string SveaWebPayOrderIdField = "SveaWebPayOrderIdField";

        public static readonly string SveaWebPayPayeeReference = "SveaWebPayPayeeReference";
        public static readonly string Culture = "Culture";
    }
}