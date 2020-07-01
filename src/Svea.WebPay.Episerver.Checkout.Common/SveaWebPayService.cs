using EPiServer.Commerce.Order;

using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;

using System.Linq;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public abstract class SveaWebPayService : ISveaWebPayService
    {
        private readonly IOrderRepository _orderRepository;

        protected SveaWebPayService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public IPurchaseOrder GetPurchaseOrderBySveaWebPayOrderId(string orderId)
        {
            OrderSearchOptions searchOptions = new OrderSearchOptions
            {
                CacheResults = false,
                StartingRecord = 0,
                RecordsToRetrieve = 1,
                Classes = new System.Collections.Specialized.StringCollection { "PurchaseOrder" },
                Namespace = "Mediachase.Commerce.Orders"
            };

            var parameters = new OrderSearchParameters
            {
                SqlMetaWhereClause = $"META.{Constants.SveaWebPayOrderIdField} LIKE '{orderId}'"
            };

            var purchaseOrder = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions)?.FirstOrDefault();

            if (purchaseOrder != null)
            {
                return _orderRepository.Load<IPurchaseOrder>(purchaseOrder.OrderGroupId);
            }
            return null;
        }
    }
}