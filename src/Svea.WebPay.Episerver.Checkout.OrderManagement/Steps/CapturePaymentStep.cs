using EPiServer.Commerce.Order;
using EPiServer.Logging;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.Episerver.Checkout.Common.Helpers;
using Svea.WebPay.SDK.PaymentAdminApi;

using System;

using Constants = Svea.WebPay.Episerver.Checkout.Common.Constants;
using TransactionType = Mediachase.Commerce.Orders.TransactionType;

namespace Svea.WebPay.Episerver.Checkout.OrderManagement.Steps
{
    public class CapturePaymentStep : PaymentStep
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CapturePaymentStep));
        private readonly IMarket _market;
        private readonly IRequestFactory _requestFactory;

        public CapturePaymentStep(
            IPayment payment,
            IMarket market,
            SveaWebPayClientFactory sveaWebPayClientFactory,
            IRequestFactory requestFactory) : base(payment, market, sveaWebPayClientFactory)
        {
            _market = market;
            _requestFactory = requestFactory;
        }

        public override PaymentStepResult Process(IPayment payment, IOrderForm orderForm, IOrderGroup orderGroup, IShipment shipment)
        {
            var paymentStepResult = new PaymentStepResult();

            if (payment.TransactionType == TransactionType.Capture.ToString())
            {
                var orderIdString = orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
                if (!string.IsNullOrEmpty(orderIdString) && long.TryParse(orderIdString, out var orderId))
                {
                    try
                    {
                        if (shipment == null)
                        {
                            throw new InvalidOperationException("Can't find correct shipment");
                        }

                        var paymentOrder = AsyncHelper.RunSync(() => SveaWebPayClient.PaymentAdmin.GetOrder(orderId));
                        var (isValid, errorMessage) = ActionsValidationHelper.ValidateOrderAction(paymentOrder, OrderActionType.CanDeliverOrder);
                        if (!isValid)
                        {
                            AddNoteAndSaveChanges(orderGroup, payment.TransactionType, errorMessage);
                            AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Deliver Order/Capture is not possible on this order {orderId}");
                            return paymentStepResult;
                        }

                        var deliveryRequest = _requestFactory.GetDeliveryRequest(payment, _market, shipment, paymentOrder);
                        var pollingTimeout = new PollingTimeout(15);
                        var order = AsyncHelper.RunSync(() => paymentOrder.Actions.DeliverOrder(deliveryRequest, pollingTimeout));

                        AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Order delivered at Svea WebPay: {order.ResourceUri.AbsoluteUri}");
                        paymentStepResult.Status = true;

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

                return paymentStepResult;
            }

            if (Successor != null)
            {
                return Successor.Process(payment, orderForm, orderGroup, shipment);
            }

            return paymentStepResult;
        }
    }
}