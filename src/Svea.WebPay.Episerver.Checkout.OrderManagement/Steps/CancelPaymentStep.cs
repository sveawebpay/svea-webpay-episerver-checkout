using EPiServer.Commerce.Order;
using EPiServer.Logging;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.Episerver.Checkout.Common.Helpers;
using Svea.WebPay.Episerver.Checkout.OrderManagement.Extensions;
using Svea.WebPay.SDK.PaymentAdminApi;

using System;
using System.Linq;
using System.Threading.Tasks;

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

        public override async Task<PaymentStepResult> Process(IPayment payment, IOrderForm orderForm, IOrderGroup orderGroup, IShipment shipment)
        {
            var paymentStepResult = new PaymentStepResult();

            if (payment.TransactionType == TransactionType.Void.ToString())
            {
                try
                {
                    var previousPayment = orderForm.Payments.FirstOrDefault(x => x.IsSveaWebPayPayment());

                    if (long.TryParse(orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString(), out var orderId))
                    {
                        var order = await SveaWebPayClient.PaymentAdmin.GetOrder(orderId).ConfigureAwait(false);
                        var (isValid, errorMessage) = ActionsValidationHelper.ValidateOrderAction(order, OrderActionType.CanCancelOrder);
                        if (!isValid)
                        {
                            AddNoteAndSaveChanges(orderGroup, payment.TransactionType, errorMessage);
                            AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Cancel is not possible on this order: {orderId}");
                            return paymentStepResult;
                        }

                        var cancelRequest = _requestFactory.GetCancelRequest();
                        await order.Actions.Cancel(cancelRequest).ConfigureAwait(false);
                        payment.Status = PaymentStatus.Processed.ToString();
                        AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Payment {orderId} has been cancelled at Svea WebPay");
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
                return await Successor.Process(payment, orderForm, orderGroup, shipment).ConfigureAwait(false);
            }

            return paymentStepResult;
        }
    }
}