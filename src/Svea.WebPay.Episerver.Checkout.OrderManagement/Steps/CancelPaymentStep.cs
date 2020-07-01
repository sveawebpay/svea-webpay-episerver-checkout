using EPiServer.Commerce.Order;
using EPiServer.Logging;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.Episerver.Checkout.OrderManagement.Extensions;

using System;
using System.Linq;

using TransactionType = Mediachase.Commerce.Orders.TransactionType;

namespace Svea.WebPay.Episerver.Checkout.OrderManagement.Steps
{
    public class CancelPaymentStep : PaymentStep
    {
        private readonly IMarket _market;
        private readonly IRequestFactory _requestFactory;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CancelPaymentStep));

        public CancelPaymentStep(IPayment payment, IMarket market, SveaWebPayClientFactory sveaWebPayClientFactory, IRequestFactory requestFactory) : base(payment, market, sveaWebPayClientFactory)
        {
            _requestFactory = requestFactory;
            _market = market;
        }

        public override PaymentStepResult Process(IPayment payment, IOrderForm orderForm, IOrderGroup orderGroup, IShipment shipment)
        {
            var paymentStepResult = new PaymentStepResult();

            if (payment.TransactionType == TransactionType.Void.ToString())
            {
                try
                {
                    var previousPayment = orderForm.Payments.FirstOrDefault(x => x.IsSveaWebPayPayment());

                    if (long.TryParse(orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString(), out var orderId))
                    {
                        var order = AsyncHelper.RunSync(() => SveaWebPayClient.PaymentAdmin.GetOrder(orderId));
                        if (order.Actions.Cancel == null)
                        {
                            AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Cancel is not possible on this order {orderId}");
                            return paymentStepResult;
                        }

                        var cancelRequest = _requestFactory.GetCancelRequest();
                        AsyncHelper.RunSync(() => order.Actions.Cancel(cancelRequest));
                        payment.Status = PaymentStatus.Processed.ToString();
                        AddNoteAndSaveChanges(orderGroup, payment.TransactionType, "Order cancelled at Svea WebPay");
                        return paymentStepResult;

                    }

                    return paymentStepResult;
                }
                catch (Exception ex)
                {
                    payment.Status = PaymentStatus.Failed.ToString();
                    paymentStepResult.Message = ex.Message;
                    paymentStepResult.Status = false;
                    AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Error occurred {ex.Message}");
                    Logger.Error(ex.Message, ex);
                }
            }

            if (Successor != null)
            {
                return Successor.Process(payment, orderForm, orderGroup, shipment);
            }

            return paymentStepResult;
        }
    }
}