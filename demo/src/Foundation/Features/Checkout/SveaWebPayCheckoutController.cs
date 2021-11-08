using EPiServer.Commerce.Order;
using EPiServer.Logging;

using Foundation.Features.Checkout.Services;

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

        public SveaWebPayCheckoutController(
            ICartService cartService,
            CheckoutService checkoutService,
            IOrderRepository orderRepository)
        {
            _cartService = cartService;
            _checkoutService = checkoutService;
            _orderRepository = orderRepository;
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
            var purchaseOrder = _checkoutService.GetOrCreatePurchaseOrder(orderGroupId, orderId, out var status);
            if (purchaseOrder == null)
            {
                return new StatusCodeResult(status, this);
            }

            return new StatusCodeResult(status, this);
        }
    }
}