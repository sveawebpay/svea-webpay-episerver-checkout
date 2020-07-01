using EPiServer.Commerce.Order;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.Episerver.Checkout.Common.Extensions;
using Svea.WebPay.SDK;

using System.Linq;

namespace Svea.WebPay.Episerver.Checkout.OrderManagement.Steps
{
    public abstract class PaymentStep
    {
        protected PaymentStep Successor;

        protected PaymentStep(IPayment payment, IMarket market, ISveaWebPayClientFactory sveaWebPayClientFactory)
        {
            MarketId = market.MarketId;
            PaymentMethod = PaymentManager.GetPaymentMethod(payment.PaymentMethodId);

            if (PaymentMethod != null)
            {
                SveaWebPayClient = sveaWebPayClientFactory.Create(PaymentMethod, market.MarketId);
            }
        }

        protected PaymentMethodDto PaymentMethod { get; set; }
        public ISveaClient SveaWebPayClient { get; }
        public MarketId MarketId { get; }


        public void SetSuccessor(PaymentStep successor)
        {
            Successor = successor;
        }

        public abstract PaymentStepResult Process(IPayment payment, IOrderForm orderForm, IOrderGroup orderGroup,
            IShipment shipment);

        public void AddNoteAndSaveChanges(IOrderGroup orderGroup, string transactionType, string noteMessage)
        {
            var noteTitle = $"{PaymentMethod.PaymentMethod.FirstOrDefault()?.Name} {transactionType.ToLower()}";
            orderGroup.AddNote(noteTitle, $"Payment {transactionType.ToLower()}: {noteMessage}");
        }
    }
}