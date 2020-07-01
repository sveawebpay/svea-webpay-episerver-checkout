using EPiServer.Commerce.Order;

using Svea.WebPay.Episerver.Checkout.Common;

namespace Svea.WebPay.Episerver.Checkout.OrderManagement.Extensions
{
    internal static class PaymentExtensions
    {
        internal static bool IsSveaWebPayPayment(this IPayment payment)
        {
            return payment?.PaymentMethodName?.StartsWith(Constants.SveaWebPaySystemKeyword) ?? false;
        }
    }
}
