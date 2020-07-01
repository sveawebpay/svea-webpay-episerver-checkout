using EPiServer.Commerce.Order;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;

using Svea.WebPay.Episerver.Checkout.Common;


namespace Svea.WebPay.Episerver.Checkout.OrderManagement.Steps
{
    public abstract class AuthorizePaymentStepBase : PaymentStep
    {
        protected AuthorizePaymentStepBase(IPayment payment, IMarket market, ISveaWebPayClientFactory sveaWebPayClientFactory) : base(payment, market, sveaWebPayClientFactory)
        {
        }

        public override PaymentStepResult Process(IPayment payment, IOrderForm orderForm, IOrderGroup orderGroup, IShipment shipment)
        {
            if (payment.TransactionType != TransactionType.Authorization.ToString())
            {
                var paymentStepResult = new PaymentStepResult();
                if (Successor != null)
                {
                    paymentStepResult = Successor.Process(payment, orderForm, orderGroup, shipment);
                    paymentStepResult.Status = true;
                    paymentStepResult.Status = Successor != null && paymentStepResult.Status;
                }

                return paymentStepResult;
            }

            return ProcessAuthorization(payment, orderGroup);
        }

        public abstract PaymentStepResult ProcessAuthorization(IPayment payment, IOrderGroup orderGroup);
    }
}
