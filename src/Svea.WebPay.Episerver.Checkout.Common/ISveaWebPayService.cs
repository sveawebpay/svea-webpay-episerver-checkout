using EPiServer.Commerce.Order;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    public interface ISveaWebPayService
    {
        IPurchaseOrder GetPurchaseOrderBySveaWebPayOrderId(string orderId);
        //IPurchaseOrder GetByPayeeReference(string payeeReference);
    }
}
