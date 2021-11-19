# Svea.WebPay.Episerver.Checkout

## Links
[Test-Data](https://www.svea.com/globalassets/sweden/foretag/betallosningar/e-handel/integrationspaket-logos-and-doc.-integration-test-instructions-webpay/testuppgifter-webpay_.pdf), In the documentation it says checkoutID, it's the same as later called MerchantId.  
[Svea Payment Admin](https://paymentadminstage.svea.com/)  
[Svea SDK](https://github.com/sveawebpay/svea-sdk-dotnet)


## Table of Contents

- [Pre-installation](#pre-installation)
- [How to get started](#how-to-get-started)
- [Configure Commerce Manager](#configure-commerce-manager)
- [Setup](#setup)
- [Known Issues](#known-issues)
- [Misc](#misc)

---

## Pre-installation
Callbacks from Svea is used both to validate order and when a purchase is completed. So to be able to develop and test, ngrok needs to be used.

The installation assumes that you have Foundation installed, and are using ngrok for callbacks.  
[Foundation](https://github.com/episerver/Foundation)  
Read [FoundationReadMe](https://github.com/sveawebpay/svea-webpay-episerver-checkout/blob/develop/FoundationReadMe.md) to fix code and web.config issues with Foundation.  
[ngrok](https://ngrok.com/)  

---

## How to get started
### Install following NuGet packages
For project Foundation and Foundation.Commerce:
```
Install-Package Svea.WebPay.Episerver.Checkout 
```
For project Foundation.CommerceManager:

```
Install-Package Svea.WebPay.Episerver.Checkout.CommerceManager
```

---

## Configure Commerce Manager
Login into Commerce Manager and open Administration -> Order System -> Payments. Then click New and in Overview tab fill:
- Name: Svea Checkout
- System Keyword: SveaWebPayCheckout
- Language
- Class Name: choose Svea.WebPay.Episerver.Checkout.SveaWebPayCheckoutGateway
- Payment Class: choose MediaChase.Commerce.Orders.OtherPayment
- IsActive: Yes

![image](https://user-images.githubusercontent.com/1358504/86447302-726e1000-bd15-11ea-9cd4-540be26b194c.png)

In Markets tab select market for which this payment will be available.

![image](https://user-images.githubusercontent.com/1358504/86447399-99c4dd00-bd15-11ea-9de2-54a15bf41ffb.png)

**Press OK to save and then configure the newly added payment. Parameters won't be visible before it has been saved.** 

![image](https://user-images.githubusercontent.com/1358504/86447750-1952ac00-bd16-11ea-9d68-52adf775c803.png)

Svea WebPay connection settings
```
CheckoutApi Uri: https://checkoutapistage.svea.com/
PaymentAdminApi Uri: https://paymentadminapistage.svea.com/
MerchantId: {Your merchant id}
Secret: {Your secret}
```

Merchant settings
```
Push Uri: https://{Your domain}/api/sveawebpay/Push/{orderGroupId}/{checkout.order.uri}
Terms Uri: https://{Your domain}/payment-completed.pdf
Checkout Uri: https://{Your domain}/en/checkout/?isGuest=1
ConfirmationUri Uri: https://{Your domain}/en/order-confirmation/?orderNumber={orderGroupId}
Checkout Validation Callback Uri: https://{Your domain}/api/sveawebpay/Validation/{orderGroupId}/{checkout.order.uri}
Active Part Payment Campaigns: {campaignId1;campaignId2;campaignId3}
Promoted Part Payment Campaign: {campaignId}
```

{Your domain} in the URL's should be updated to your host.

---

## Setup
### Payment Method
You need to add a PaymentMethod to the site.
Following is an example of a PaymentMethod using Svea Checkout


```Csharp

using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;

using Foundation.Commerce.Markets;
using Foundation.Features.Checkout.Services;

using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;

using Svea.WebPay.Episerver.Checkout;
using Svea.WebPay.Episerver.Checkout.Common;

using System;
using System.ComponentModel;

namespace Foundation.Features.Checkout.Payments
{
    [ServiceConfiguration(typeof(IPaymentMethod))]
    public class SveaWebPayCheckoutPaymentOption : PaymentOptionBase, IDataErrorInfo
    {
        private readonly ICartService _cartService;
        private readonly ICurrentMarket _currentMarket;
        private readonly LanguageService _languageService;
        private readonly IMarketService _marketService;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IOrderRepository _orderRepository;
        private readonly ISveaWebPayCheckoutService _sveaWebPayCheckoutService;

        private bool _isInitalized;

        public SveaWebPayCheckoutPaymentOption() : this(
            LocalizationService.Current,
            ServiceLocator.Current.GetInstance<ICartService>(),
            ServiceLocator.Current.GetInstance<ICurrentMarket>(),
            ServiceLocator.Current.GetInstance<LanguageService>(),
            ServiceLocator.Current.GetInstance<IMarketService>(),
            ServiceLocator.Current.GetInstance<IOrderGroupFactory>(),
            ServiceLocator.Current.GetInstance<IOrderRepository>(),
            ServiceLocator.Current.GetInstance<IPaymentService>(),
            ServiceLocator.Current.GetInstance<ISveaWebPayCheckoutService>())
        {
        }

        public SveaWebPayCheckoutPaymentOption(
            LocalizationService localizationService,
            ICartService cartService,
            ICurrentMarket currentMarket,
            LanguageService languageService,
            IMarketService marketService,
            IOrderGroupFactory orderGroupFactory,
            IOrderRepository orderRepository,
            IPaymentService paymentService,
            ISveaWebPayCheckoutService sveaWebPayCheckoutService) : base(localizationService, orderGroupFactory, currentMarket, languageService, paymentService)
        {
            _cartService = cartService;
            _currentMarket = currentMarket;
            _languageService = languageService;
            _marketService = marketService;
            _orderGroupFactory = orderGroupFactory;
            _orderRepository = orderRepository;
            _sveaWebPayCheckoutService = sveaWebPayCheckoutService;
        }

        public override IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var payment = orderGroup.CreatePayment(_orderGroupFactory);
            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = PaymentMethodId;
            payment.PaymentMethodName = Constants.SveaWebPayCheckoutSystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.TransactionType = TransactionType.Authorization.ToString();

            return payment;
        }

        public override bool ValidateData() => true;

        public override string SystemKeyword => Constants.SveaWebPayCheckoutSystemKeyword;

        public string this[string columnName] => string.Empty;

        public string Error { get; }

        public CheckoutConfiguration CheckoutConfiguration { get; set; }

        public void InitializeValues()
        {
            InitializeValues(_cartService.DefaultCartName);
        }

        public void InitializeValues(string cartName)
        {
            if (_isInitalized)
            {
                return;
            }

            var cart = _cartService.LoadCart(cartName, true)?.Cart;
            if (cart != null)
            {
                var market = _marketService.GetMarket(cart.MarketId);

                var currentLanguage = _languageService.GetCurrentLanguage();
                CheckoutConfiguration = _sveaWebPayCheckoutService.LoadCheckoutConfiguration(market, currentLanguage.TwoLetterISOLanguageName);

                VerifyCartHasShippingCountry(cart);
                var paymentOrder = AsyncHelper.RunSync(() => _sveaWebPayCheckoutService.CreateOrUpdateOrder(cart, _languageService.GetCurrentLanguage(), true));
                HtmlSnippet = paymentOrder?.Gui.Snippet;
                _isInitalized = paymentOrder != null;
            }
        }

        public void VerifyCartHasShippingCountry(ICart cart)
        {
            var orderAddress = cart.GetFirstShipment().ShippingAddress;
            if (orderAddress == null)
            {
                orderAddress = cart.CreateOrderAddress(Guid.NewGuid().ToString());
                cart.GetFirstShipment().ShippingAddress = orderAddress;
            }

            if (string.IsNullOrWhiteSpace(orderAddress.CountryCode))
            {
                orderAddress.CountryCode = _currentMarket.GetCurrentMarket().DefaultLanguage.ThreeLetterISOLanguageName;
                _orderRepository.Save(cart);
            }
        }

        public string HtmlSnippet { get; private set; }
    }
}


```

Add following methods for creating Purchase Order in e.g. Foundation/Features/Checkout/Services/CheckoutService.cs To be able to use this code you need to constructor inject ICartService.



```CSharp
protected static readonly ConcurrentDictionary<string, DateTime> ProcessingOrdersCache = new ConcurrentDictionary<string, DateTime>();

public IPurchaseOrder GetOrCreatePurchaseOrder(int orderGroupId, long sveaWebPayOrderId, out HttpStatusCode status)
{
    var key = $"{orderGroupId}-{sveaWebPayOrderId}";
    if (ProcessingOrdersCache.TryAdd(key, DateTime.UtcNow))
    {
        // Check if the order has been created already
        var purchaseOrder = _sveaWebPayCheckoutService.GetPurchaseOrderBySveaWebPayOrderId(sveaWebPayOrderId.ToString());
        if (purchaseOrder != null)
        {
            status = HttpStatusCode.OK;
            ProcessingOrdersCache.TryRemove(key, out DateTime value1);
            return purchaseOrder;
        }

        // Check if we still have a cart and can create an order
        var cart = _orderRepository.Load<ICart>(orderGroupId);
        if (cart == null)
        {
            _log.Log(Level.Information, $"Purchase order or cart with orderId {orderGroupId} not found");
            status = HttpStatusCode.NotFound;
            ProcessingOrdersCache.TryRemove(key, out DateTime value2);
            return null;
        }

        var cartSveaWebPayOrderId = cart.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
        if (cartSveaWebPayOrderId == null || !cartSveaWebPayOrderId.Equals(sveaWebPayOrderId.ToString()))
        {
            _log.Log(Level.Information, $"cart: {orderGroupId} with svea webpay order id {cartSveaWebPayOrderId} does not equal svea webpay order id {sveaWebPayOrderId} sent in the request");
            status = HttpStatusCode.Conflict;
            ProcessingOrdersCache.TryRemove(key, out DateTime value3);
            return null;
        }

        var order = AsyncHelper.RunSync(() => _sveaWebPayCheckoutService.GetOrder(cart));
        if (!order.Status.Equals(CheckoutOrderStatus.Final))
        {
            // Won't create order, Svea webpay checkout not complete
            _log.Log(Level.Information, $"Svea webpay order id {cartSveaWebPayOrderId} not completed");
            status = HttpStatusCode.NotFound;
            ProcessingOrdersCache.TryRemove(key, out DateTime value4);
            return null;
        }

        purchaseOrder = CreatePurchaseOrderForSveaWebPay(sveaWebPayOrderId, order, cart);
        status = HttpStatusCode.OK;
        ProcessingOrdersCache.TryRemove(key, out DateTime value5);
        return purchaseOrder;
    }

    _log.Log(Level.Information, $"Already processing orderGroupId: {orderGroupId} orderId: {sveaWebPayOrderId}");
    status = HttpStatusCode.Conflict;
    ProcessingOrdersCache.TryRemove(key, out DateTime value6);
    return null;
}

public IPurchaseOrder CreatePurchaseOrderForSveaWebPay(long sveaWebPayOrderId, Data order, ICart cart)
{
    // Clean up payments in cart on payment provider site.
    foreach (var form in cart.Forms)
    {
        form.Payments.Clear();
    }

    var languageid = cart.Properties[Constants.Culture].ToString();
    var paymentRow = PaymentManager.GetPaymentMethodBySystemName(Constants.SveaWebPayCheckoutSystemKeyword, languageid, cart.MarketId.Value).PaymentMethod.FirstOrDefault();

    var payment = cart.CreatePayment(_orderGroupFactory);
    payment.PaymentType = PaymentType.Other;
    payment.PaymentMethodId = paymentRow.PaymentMethodId;
    payment.PaymentMethodName = Constants.SveaWebPayCheckoutSystemKeyword;
    payment.Amount = cart.GetTotal(_orderGroupCalculator).Amount;

    var isSaleTransaction = order.Payment.PaymentMethodType == PaymentMethodType.DirectBank || order.Payment.PaymentMethodType == PaymentMethodType.Trustly || order.Payment.PaymentMethodType == PaymentMethodType.Swish;

    payment.Status = isSaleTransaction
        ? PaymentStatus.Processed.ToString()
        : PaymentStatus.Pending.ToString();

    payment.TransactionType = isSaleTransaction
        ? TransactionType.Sale.ToString()
        : TransactionType.Authorization.ToString();

    cart.AddPayment(payment, _orderGroupFactory);
    cart.AddNote($"Payed with {order.Payment?.PaymentMethodType?.ToString()}", $"Payed with {order.Payment?.PaymentMethodType?.ToString()}");

    var billingAddress = new AddressModel
    {
        Name = $"{order.BillingAddress.StreetAddress}{order.BillingAddress.PostalCode}{order.BillingAddress.City}",
        FirstName = order.BillingAddress.FullName,
        LastName = order.BillingAddress.LastName,
        Email = order.EmailAddress,
        DaytimePhoneNumber = order.PhoneNumber,
        Line1 = order.BillingAddress.StreetAddress,
        PostalCode = order.BillingAddress.PostalCode,
        City = order.BillingAddress.City,
        CountryCode = order.BillingAddress.CountryCode
    };

    payment.BillingAddress = _addressBookService.ConvertToAddress(billingAddress, cart);

    var shippingAddress = new AddressModel
    {
        Name = $"{order.ShippingAddress.StreetAddress}{order.ShippingAddress.PostalCode}{order.ShippingAddress.City}",
        FirstName = order.ShippingAddress.FullName,
        LastName = order.ShippingAddress.LastName,
        Email = order.EmailAddress,
        DaytimePhoneNumber = order.PhoneNumber,
        Line1 = order.ShippingAddress.StreetAddress,
        PostalCode = order.ShippingAddress.PostalCode,
        City = order.ShippingAddress.City,
        CountryName = order.ShippingAddress.CountryCode,
        CountryCode = cart.GetFirstShipment().ShippingAddress?.CountryCode,
    };

    cart.GetFirstShipment().ShippingAddress = _addressBookService.ConvertToAddress(shippingAddress, cart);

    cart.ProcessPayments(_paymentProcessor, _orderGroupCalculator);

    var totalProcessedAmount = cart.GetFirstForm().Payments.Where(x => x.Status.Equals(PaymentStatus.Processed.ToString())).Sum(x => x.Amount);
    if (totalProcessedAmount != cart.GetTotal(_orderGroupCalculator).Amount)
    {
        throw new InvalidOperationException("Wrong amount");
    }

    _cartService.RequestInventory(cart);

    var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);
    var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
    _orderRepository.Delete(cart.OrderLink);

    if (purchaseOrder == null)
    {
        return null;
    }
    else
    {
        purchaseOrder.Properties[Constants.SveaWebPayOrderIdField] = sveaWebPayOrderId;
        _orderRepository.Save(purchaseOrder);
        return purchaseOrder;
    }
}
```

To initialize Svea checkout when loading the GUI, update GetPaymentMethodViewModels methods in Foundation/Features/Checkout/ViewModels/PaymentMethodViewModelFactory.cs
```CSharp

        public IEnumerable<PaymentMethodViewModel> GetPaymentMethodViewModels()
        {
            var currentMarket = _currentMarket.GetCurrentMarket().MarketId;
            var currentLanguage = _languageService.GetCurrentLanguage().TwoLetterISOLanguageName;
            var availablePaymentMethods = _paymentService.GetPaymentMethodsByMarketIdAndLanguageCode(currentMarket.Value, currentLanguage);
            var availableCustomerGiftCards = _giftCardService.GetCustomerGiftCards(CustomerContext.Current.CurrentContactId.ToString()).Where(g => g.IsActive == true);
            var displayedPaymentMethods = availablePaymentMethods
                .Where(p => _paymentOptions.Any(m => m.PaymentMethodId == p.PaymentMethodId))
                .Select(p => new PaymentMethodViewModel(_paymentOptions.First(m => m.PaymentMethodId == p.PaymentMethodId)) { IsDefault = p.IsDefault })
                .ToList();

            if (displayedPaymentMethods.Any(x => x.SystemKeyword == Constants.SveaWebPayCheckoutSystemKeyword))
            {
                var paymentMethodViewModel = displayedPaymentMethods.FirstOrDefault(x => x.SystemKeyword == Constants.SveaWebPayCheckoutSystemKeyword);
                var sveaWebPayCheckoutPaymentMethod = paymentMethodViewModel?.PaymentOption as SveaWebPayCheckoutPaymentOption;
                sveaWebPayCheckoutPaymentMethod?.InitializeValues();
            }

            if (availableCustomerGiftCards.Any() == false)
            {
                displayedPaymentMethods.RemoveAll(x => x.SystemKeyword == "GiftCardPayment");
            }

            return displayedPaymentMethods;
        }
```

### Endpoints
Add a controller for callbacks, e.g Foundation/Features/Checkout/SveaWebPayCheckoutController.cs
```Csharp

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


```

### Frontend
Add view Foundation\\Features\\Checkout\\_SveaWebPayCheckoutPaymentMethod.cshtml

```html
@model Foundation.Features.Checkout.Payments.SveaWebPayCheckoutPaymentOption

@Html.HiddenFor(model => model.PaymentMethodId)

<br />
<div class="row">
	<div class="col-12">
		<div class="alert alert-info square-box">
			@Html.Raw(Model.HtmlSnippet)
		</div>
	</div>
</div>

```

Add view Foundation\\Features\\MyAccount\\OrderConfirmation\\_SveaWebPayCheckoutConfirmation.cshtml

```html
<div class="quicksilver-well">
	<h4>@Html.Translate("/OrderConfirmation/PaymentDetails")</h4>
	<p>@Model.PaymentMethodName</p>
</div>
```

---
## Misc
* Supported languages: swedish, norwegian, danish, finnish and german
* Supported currencies: SEK, NOK, DKK and EUR
* A common issue when setting up Svea Checkout is that the thank you page isn't displayed after a completed purchase. That's because the Index action in `Foundation.Features.MyAccount.OrderConfirmation.OrderConfirmationController` expects an orderNumber, and the order is created first on callback. Therefore, you will be redirected to the start page and the cart disappears on the callback. This logic is part of the Foundation and needs to be adjusted to suite your needs.
