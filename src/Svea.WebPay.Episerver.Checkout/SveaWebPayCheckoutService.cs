using EPiServer.Commerce.Order;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.SDK.CheckoutApi;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Svea.WebPay.Episerver.Checkout
{
    [ServiceConfiguration(typeof(ISveaWebPayCheckoutService))]
    public class SveaWebPayCheckoutService : SveaWebPayService, ISveaWebPayCheckoutService
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly ICurrentMarket _currentMarket;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(SveaWebPayCheckoutService));
        private readonly IMarketService _marketService;
        private readonly IOrderRepository _orderRepository;
        private readonly IRequestFactory _requestFactory;
        private readonly ISveaWebPayClientFactory _sveaWebPayClientFactory;
        private PaymentMethodDto _paymentMethodDto;

        public SveaWebPayCheckoutService(
            ICurrentMarket currentMarket,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IMarketService marketService,
            IOrderRepository orderRepository,
            ISveaWebPayClientFactory sveaWebPayClientFactory,
            IRequestFactory requestFactory) : base(orderRepository)
        {
            _currentMarket = currentMarket;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _marketService = marketService;
            _orderRepository = orderRepository;
            _sveaWebPayClientFactory = sveaWebPayClientFactory;
            _requestFactory = requestFactory;
        }

        public PaymentMethodDto PaymentMethodDto => _paymentMethodDto ?? (_paymentMethodDto =
                                                        PaymentManager.GetPaymentMethodBySystemName(
                                                            Constants.SveaWebPayCheckoutSystemKeyword,
                                                            ContentLanguage.PreferredCulture.Name, true));


        public virtual Data CreateOrUpdateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null)
        {
            var allLineItems = orderGroup.GetAllLineItems();
            if (allLineItems == null || !allLineItems.Any())
            {
                return null;
            }

            if (long.TryParse(orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString(), out var orderId))
            {
                return UpdateOrder(orderGroup, currentLanguage, orderId, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData);
            }

            return CreateOrder(orderGroup, currentLanguage, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData);
        }

        public virtual Data CreateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null)
        {
            var market = _currentMarket.GetCurrentMarket();
            var sveaWebPayClient = _sveaWebPayClientFactory.Create(market, currentLanguage.TwoLetterISOLanguageName);

            try
            {
                var orderRequest = _requestFactory.GetOrderRequest(orderGroup, market, PaymentMethodDto, currentLanguage, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData);
                var order = AsyncHelper.RunSync(() => sveaWebPayClient.Checkout.CreateOrder(orderRequest));
                orderGroup.Properties[Constants.Culture] = currentLanguage.TwoLetterISOLanguageName;
                orderGroup.Properties[Constants.SveaWebPayOrderIdField] = order.OrderId;
                orderGroup.Properties[Constants.SveaWebPayPayeeReference] = orderRequest.ClientOrderNumber;
                _orderRepository.Save(orderGroup);
                return order;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
        }

        public virtual Data UpdateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, long orderId, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null)
        {
            var market = _currentMarket.GetCurrentMarket();
            var sveaWebPayClient = _sveaWebPayClientFactory.Create(market, currentLanguage.TwoLetterISOLanguageName);

            var order = AsyncHelper.RunSync(() => sveaWebPayClient.Checkout.GetOrder(orderId));
            if (order.Status != CheckoutOrderStatus.Created)
            {
                return CreateOrder(orderGroup, currentLanguage, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData);
            }

            try
            {
                var updateOrderRequest = _requestFactory.GetUpdateOrderRequest(orderGroup, market, PaymentMethodDto, currentLanguage, includeTaxOnLineItems, temporaryReference, merchantData);
                order = AsyncHelper.RunSync(() => sveaWebPayClient.Checkout.UpdateOrder(orderId, updateOrderRequest));
                orderGroup.Properties[Constants.Culture] = currentLanguage.TwoLetterISOLanguageName;
                return order;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
        }

        public virtual Data GetOrder(IOrderGroup orderGroup)
        {
            var sveaWebPayOrderId = orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
            if (!string.IsNullOrWhiteSpace(sveaWebPayOrderId) && long.TryParse(sveaWebPayOrderId, out var orderId))
            {
                var market = _marketService.GetMarket(orderGroup.MarketId);
                return GetOrder(orderId, market, orderGroup.Properties[Constants.Culture]?.ToString());
            }

            return null;
        }

        public virtual Data GetOrder(long orderId, IMarket market, string languageId)
        {
            var sveaWebPayClient = _sveaWebPayClientFactory.Create(market, languageId);
            try
            {
                return AsyncHelper.RunSync(() => sveaWebPayClient.Checkout.GetOrder(orderId));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
        }

        public CheckoutConfiguration LoadCheckoutConfiguration(IMarket market, string languageId)
        {
            return _checkoutConfigurationLoader.GetConfiguration(market.MarketId, languageId);
        }
    }
}