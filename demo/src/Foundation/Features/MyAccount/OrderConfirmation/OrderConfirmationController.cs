using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Editor;
using EPiServer.Web.Mvc.Html;
using EPiServer.Web.Routing;

using Foundation.Commerce.Customer.Services;
using Foundation.Features.Checkout.Services;
using Foundation.Features.MyAccount.AddressBook;
using Foundation.Infrastructure.Services;

using Svea.WebPay.Episerver.Checkout;
using Svea.WebPay.Episerver.Checkout.Common;

using System.Web.Mvc;

namespace Foundation.Features.MyAccount.OrderConfirmation
{
    public class OrderConfirmationController : OrderConfirmationControllerBase<OrderConfirmationPage>
    {
        private readonly ICampaignService _campaignService;
        private readonly CheckoutService _checkoutService;
        private readonly IOrderRepository _orderRepository;
        private readonly ISveaWebPayCheckoutService _sveaWebPayCheckoutService;

        public OrderConfirmationController(
            ICampaignService campaignService,
            CheckoutService checkoutService,
            ConfirmationService confirmationService,
            IAddressBookService addressBookService,
            IOrderRepository orderRepository,
            IOrderGroupCalculator orderGroupCalculator,
            UrlResolver urlResolver,
            ICustomerService customerService,
            ISveaWebPayCheckoutService sveaWebPayCheckoutService) :
            base(confirmationService, addressBookService, orderGroupCalculator, urlResolver, customerService)
        {
            _campaignService = campaignService;
            _checkoutService = checkoutService;
            _orderRepository = orderRepository;
            _sveaWebPayCheckoutService = sveaWebPayCheckoutService;
        }
        public ActionResult Index(OrderConfirmationPage currentPage, string notificationMessage, int? orderNumber)
        {
            IPurchaseOrder order = null;
            if (PageEditing.PageIsInEditMode)
            {
                order = _confirmationService.CreateFakePurchaseOrder();
            }
            else if (orderNumber.HasValue)
            {
                order = _confirmationService.GetOrder(orderNumber.Value);
            }

            if (order == null && orderNumber.HasValue)
            {
                var cart = _orderRepository.Load<ICart>(orderNumber.Value);
                var sveaWebPayOrderId = cart?.Properties[Constants.SveaWebPayOrderIdField];
                if (!string.IsNullOrWhiteSpace(sveaWebPayOrderId?.ToString()))
                {
                    order = long.TryParse(sveaWebPayOrderId.ToString(), out var orderId)
                        ? _checkoutService.GetOrCreatePurchaseOrder(orderNumber.Value, orderId, out var status)
                        : _sveaWebPayCheckoutService.GetPurchaseOrderBySveaWebPayOrderId(sveaWebPayOrderId.ToString());
                }
            }

            if (order != null && order.CustomerId == _customerService.CurrentContactId)
            {
                var viewModel = CreateViewModel(currentPage, order);
                viewModel.NotificationMessage = notificationMessage;

                _campaignService.UpdateLastOrderDate();
                _campaignService.UpdatePoint(decimal.ToInt16(viewModel.SubTotal.Amount));

                return View(viewModel);
            }

            return Redirect(Url.ContentUrl(ContentReference.StartPage));
        }
    }
}