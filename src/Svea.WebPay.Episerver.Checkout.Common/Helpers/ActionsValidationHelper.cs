using System;
using System.Linq;

using Svea.WebPay.SDK.PaymentAdminApi.Models;

namespace Svea.WebPay.Episerver.Checkout.Common.Helpers
{
    public static class ActionsValidationHelper
    {
        public static Tuple<bool, string> ValidateOrderAction(Order order, string orderAction)
        {
            if (order == null)
            {
                return Tuple.Create(false, "Payment Order does not exist");
            }

            if (orderAction == null)
            {
                return Tuple.Create(true, (string)null);
            }

            if (!order.AvailableActions.Contains(orderAction))
            {
                return Tuple.Create(false, $"Operation {orderAction} not available");
            }

            return Tuple.Create(true, (string)null);
        }

        public static Tuple<bool, string> ValidateOrderRowAction(Order order, long orderRowId, string orderRowAction)
        {
            var validateOrderAction = ValidateOrderAction(order, null);
            if (!validateOrderAction.Item1)
            {
                return validateOrderAction;
            }

            var orderRow = order.OrderRows.FirstOrDefault(row => row.OrderRowId == orderRowId);
            if (orderRow == null)
            {
                return Tuple.Create(false, $"Order row {orderRowId} does not exist");
            }

            if (orderRowAction == null)
            {
                return Tuple.Create(true, (string)null);
            }

            if (!orderRow.AvailableActions.Contains(orderRowAction))
            {
                return Tuple.Create(false, $"Operation {orderRowAction} not available");
            }

            return Tuple.Create(true, (string)null);
        }

        public static Tuple<bool, string> ValidateDeliveryAction(Order order, long deliveryId, string deliveryAction)
        {
            var validateOrderAction = ValidateOrderAction(order, null);
            if (!validateOrderAction.Item1)
            {
                return validateOrderAction;
            }

            var delivery = order.Deliveries.FirstOrDefault(dlv => dlv.Id == deliveryId);

            if (delivery == null)
            {
                return Tuple.Create(false, $"Delivery {deliveryId} does not exist");
            }

            if (deliveryAction == null)
            {
                return Tuple.Create(true, (string)null);
            }

            if (!delivery.AvailableActions.Contains(deliveryAction))
            {
                return Tuple.Create(false, $"Operation {deliveryAction} not available");
            }

            return Tuple.Create(true, (string)null);
        }
    }
}