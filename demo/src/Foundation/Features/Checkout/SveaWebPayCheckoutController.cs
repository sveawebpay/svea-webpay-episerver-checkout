using EPiServer.Commerce.Order;
using EPiServer.Logging;

using Foundation.Features.Checkout.Services;

using Svea.WebPay.Episerver.Checkout;
using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.SDK.CheckoutApi;

using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;

namespace Foundation.Features.Checkout
{
    [RoutePrefix("api/sveawebpay")]
    public class SveaWebPayCheckoutController : ApiController
    {
        private readonly ICartService _cartService;
        private readonly CheckoutService _checkoutService;
        private readonly IOrderRepository _orderRepository;
        private static readonly ILogger _log = LogManager.GetLogger(typeof(SveaWebPayCheckoutController));

        private readonly ISveaWebPayCheckoutService _sveaWebPayCheckoutService;

        public SveaWebPayCheckoutController(
            ICartService cartService,
            CheckoutService checkoutService,
            IOrderRepository orderRepository,
            ISveaWebPayCheckoutService sveaWebPayCheckoutService)
        {
            _cartService = cartService;
            _checkoutService = checkoutService;
            _orderRepository = orderRepository;
            _sveaWebPayCheckoutService = sveaWebPayCheckoutService;
        }

        [HttpGet]
        [Route("validation/{orderGroupId}/{orderId?}")]
        public IHttpActionResult Validation(int orderGroupId, long? orderId)
        {
            var cart = _orderRepository.Load<ICart>(orderGroupId);

            if (orderId != null)
            {
                // GET Request may contain an orderId
            }
            var validationIssues = _cartService.ValidateCart(cart);

            if (validationIssues.Any())
            {
                var response = new CheckoutValidationCallbackResponse(false, string.Join(",", validationIssues.Select(issue => issue.Value.Select(i => i.ToString()))));
                return Content(HttpStatusCode.PreconditionFailed, response);
            }

            return Ok(new CheckoutValidationCallbackResponse(true));
        }

        [HttpPost]
        [Route("push/{orderGroupId}/{orderId?}")]
        public IHttpActionResult Push(int orderGroupId, long orderId)
        {
            var purchaseOrder = GetOrCreatePurchaseOrder(orderGroupId, orderId, out var status);
            if (purchaseOrder == null)
            {
                return new StatusCodeResult(status, this);
            }

            return new StatusCodeResult(status, this);
        }

        private IPurchaseOrder GetOrCreatePurchaseOrder(int orderGroupId, long sveaWebPayOrderId, out HttpStatusCode status)
        {
            // Check if the order has been created already
            var purchaseOrder = _sveaWebPayCheckoutService.GetPurchaseOrderBySveaWebPayOrderId(sveaWebPayOrderId.ToString());
            if (purchaseOrder != null)
            {
                status = HttpStatusCode.OK;
                return purchaseOrder;
            }

            // Check if we still have a cart and can create an order
            var cart = _orderRepository.Load<ICart>(orderGroupId);
            if (cart == null)
            {
                _log.Log(Level.Information, $"Purchase order or cart with orderId {orderGroupId} not found");
                status = HttpStatusCode.NotFound;
                return null;
            }

            var cartSveaWebPayOrderId = cart.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
            if (cartSveaWebPayOrderId == null || !cartSveaWebPayOrderId.Equals(sveaWebPayOrderId.ToString()))
            {
                _log.Log(Level.Information, $"cart: {orderGroupId} with svea webpay order id {cartSveaWebPayOrderId} does not equal svea webpay order id {sveaWebPayOrderId} sent in the request");
                status = HttpStatusCode.Conflict;
                return null;
            }

            var order = _sveaWebPayCheckoutService.GetOrder(cart);
            if (!order.Status.Equals(CheckoutOrderStatus.Final))
            {
                // Won't create order, Svea webpay checkout not complete
                _log.Log(Level.Information, $"Svea webpay order id {cartSveaWebPayOrderId} not completed");
                status = HttpStatusCode.NotFound;
                return null;
            }

            purchaseOrder = _checkoutService.CreatePurchaseOrderForSveaWebPay(sveaWebPayOrderId, order, cart);
            status = HttpStatusCode.OK;
            return purchaseOrder;
        }
    }
}
