using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using Foundation.Commerce.Markets;
using Foundation.Commerce.Order.Services;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using Svea.WebPay.Episerver.Checkout;
using Svea.WebPay.Episerver.Checkout.Common;
using System;
using System.ComponentModel;

namespace Foundation.Commerce.Order.Payments
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
            if(cart != null)
            {
                var market = _marketService.GetMarket(cart.MarketId);

                var currentLanguage = _languageService.GetCurrentLanguage();
                CheckoutConfiguration = _sveaWebPayCheckoutService.LoadCheckoutConfiguration(market, currentLanguage.TwoLetterISOLanguageName);

                VerifyCartHasShippingCountry(cart);
                var paymentOrder = _sveaWebPayCheckoutService.CreateOrUpdateOrder(cart, _languageService.GetCurrentLanguage());
                HtmlSnippet = paymentOrder.Gui.Snippet;
                _isInitalized = true;
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