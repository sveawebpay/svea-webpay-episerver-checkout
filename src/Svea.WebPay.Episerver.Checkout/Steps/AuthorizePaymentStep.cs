using EPiServer.Commerce.Order;
using EPiServer.Logging;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.Episerver.Checkout.OrderManagement.Steps;

using System;

namespace Svea.WebPay.Episerver.Checkout.Steps
{
    public class AuthorizePaymentStep : AuthorizePaymentStepBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(AuthorizePaymentStep));

        public AuthorizePaymentStep(IPayment payment, IMarket market, SveaWebPayClientFactory sveaWebPayClientFactory) : base(payment, market, sveaWebPayClientFactory)
        {
        }

        public override PaymentStepResult ProcessAuthorization(IPayment payment, IOrderGroup orderGroup)
        {
            var paymentStepResult = new PaymentStepResult();

            var orderId = orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
            if (!string.IsNullOrEmpty(orderId))
            {
                try
                {
                    AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Authorize completed at Svea WebPay");
                    paymentStepResult.Status = true;
                }
                catch (Exception ex)
                {
                    payment.Status = PaymentStatus.Failed.ToString();
                    paymentStepResult.Message = ex.Message;
                    AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Error occurred {ex.Message}");
                    Logger.Error(ex.Message, ex);
                }

                return paymentStepResult;
            }

            return paymentStepResult;
        }
    }
}