using EPiServer.Commerce.Order;
using EPiServer.Logging;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus;

using Svea.WebPay.Episerver.Checkout.Common;

using System;
using System.Linq;

using TransactionType = Mediachase.Commerce.Orders.TransactionType;

namespace Svea.WebPay.Episerver.Checkout.OrderManagement.Steps
{
    public class CreditPaymentStep : PaymentStep
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CreditPaymentStep));
        private readonly IRequestFactory _requestFactory;
        private readonly IMarket _market;

        public CreditPaymentStep(IPayment payment, IMarket market, SveaWebPayClientFactory sveaWebPayClientFactory, IRequestFactory requestFactory)
            : base(payment, market, sveaWebPayClientFactory)
        {
            _requestFactory = requestFactory;
            _market = market;
        }

        public override PaymentStepResult Process(IPayment payment, IOrderForm orderForm, IOrderGroup orderGroup, IShipment shipment)
        {
            var paymentStepResult = new PaymentStepResult();

            if (payment.TransactionType == TransactionType.Credit.ToString())
            {
                try
                {
                    var orderIdString = orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
                    if (!string.IsNullOrEmpty(orderIdString) && long.TryParse(orderIdString, out var orderId))
                    {
                        if (orderGroup is IPurchaseOrder purchaseOrder)
                        {
                            var returnForm = purchaseOrder.ReturnForms.FirstOrDefault(x => ((OrderForm)x).Status == ReturnFormStatus.Complete.ToString() && ((OrderForm)x).ObjectState == MetaObjectState.Modified);

                            if (returnForm != null)
                            {
                                var transactionDescription = string.IsNullOrWhiteSpace(returnForm.ReturnComment)
                                    ? "Crediting payment."
                                    : returnForm.ReturnComment;

                                var paymentOrder = AsyncHelper.RunSync(() => SveaWebPayClient.PaymentAdmin.GetOrder(orderId));

                                if (paymentOrder.Deliveries.FirstOrDefault() != null)
                                {
                                    if (paymentOrder.Deliveries.First().Actions.CreditNewRow != null)
                                    {
                                        var pollingTimeout = TimeSpan.FromSeconds(3);
                                        var creditNewOrderRowRequest = _requestFactory.GetCreditNewOrderRowRequest(payment, shipment, transactionDescription, pollingTimeout);
                                        var creditResponseObject = AsyncHelper.RunSync(() => paymentOrder.Deliveries.First().Actions.CreditNewRow(creditNewOrderRowRequest));
                                        payment.ProviderTransactionID = creditResponseObject?.Resource?.CreditId;
                                    }
                                    else if (paymentOrder.Deliveries.First().Actions.CreditAmount != null)
                                    {
                                        var creditAmountRequest = _requestFactory.GetCreditAmountRequest(payment, shipment);
                                        var creditResponseObject = AsyncHelper.RunSync(() => paymentOrder.Deliveries.First().Actions.CreditAmount(creditAmountRequest));
                                        payment.ProviderTransactionID = creditResponseObject.CreditId;
                                    }
                                    else if (paymentOrder.Actions.CancelAmount != null)
                                    {
                                        var cancelAmountRequest = _requestFactory.GetCancelAmountRequest(paymentOrder, payment, shipment);
                                        AsyncHelper.RunSync(() => paymentOrder.Actions.CancelAmount(cancelAmountRequest));
                                    }
                                 
                                    payment.Status = PaymentStatus.Processed.ToString();
                                    AddNoteAndSaveChanges(orderGroup, payment.TransactionType, $"Credited with {payment.Amount}");

                                    paymentStepResult.Status = true;
                                }
                            }
                        }

                        return paymentStepResult;
                    }
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
            else if (Successor != null)
            {
                return Successor.Process(payment, orderForm, orderGroup, shipment);
            }

            return paymentStepResult;
        }
    }
}
