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
using System.Threading.Tasks;

namespace Svea.WebPay.Episerver.Checkout
{
    [ServiceConfiguration(typeof(ISveaWebPayCheckoutService))]
    public class SveaWebPayCheckoutService : SveaWebPayService, ISveaWebPayCheckoutService
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(SveaWebPayCheckoutService));
        private readonly IMarketService _marketService;
        private readonly IOrderRepository _orderRepository;
        private readonly IRequestFactory _requestFactory;
        private readonly ISveaWebPayClientFactory _sveaWebPayClientFactory;
        private PaymentMethodDto _paymentMethodDto;

        public SveaWebPayCheckoutService(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IMarketService marketService,
            IOrderRepository orderRepository,
            ISveaWebPayClientFactory sveaWebPayClientFactory,
            IRequestFactory requestFactory) : base(orderRepository)
        {
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


        public virtual async Task<Data> CreateOrUpdateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null)
        {
            var allLineItems = orderGroup.GetAllLineItems();
            if (allLineItems == null || !allLineItems.Any())
            {
                return null;
            }

            if (long.TryParse(orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString(), out var orderId) && orderGroup.Properties[Constants.Culture]?.ToString() == currentLanguage.TwoLetterISOLanguageName)
            {
                return await UpdateOrder(orderGroup, currentLanguage, orderId, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData).ConfigureAwait(false);
            }

            return await CreateOrder(orderGroup, currentLanguage, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData).ConfigureAwait(false);
        }

        public virtual async Task<Data> CreateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null)
        {
            var market = _marketService.GetMarket(orderGroup.MarketId);
            var sveaWebPayClient = _sveaWebPayClientFactory.Create(market, currentLanguage.TwoLetterISOLanguageName);

            try
            {
                var orderRequest = _requestFactory.GetOrderRequest(orderGroup, market, PaymentMethodDto, currentLanguage, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData);
                var order = await sveaWebPayClient.Checkout.CreateOrder(orderRequest).ConfigureAwait(false);
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

        public virtual async Task<Data> UpdateOrder(IOrderGroup orderGroup, CultureInfo currentLanguage, long orderId, bool includeTaxOnLineItems, string temporaryReference = null, IList<Presetvalue> presetValues = null, IdentityFlags identityFlags = null, Guid? partnerKey = null, string merchantData = null)
        {
            var market = _marketService.GetMarket(orderGroup.MarketId);
            var sveaWebPayClient = _sveaWebPayClientFactory.Create(market, currentLanguage.TwoLetterISOLanguageName);

            var order = await sveaWebPayClient.Checkout.GetOrder(orderId).ConfigureAwait(false);
            if (order.Status != CheckoutOrderStatus.Created)
            {
                return await CreateOrder(orderGroup, currentLanguage, includeTaxOnLineItems, temporaryReference, presetValues, identityFlags, partnerKey, merchantData).ConfigureAwait(false);
            }

            try
            {
                var updateOrderRequest = _requestFactory.GetUpdateOrderRequest(orderGroup, market, PaymentMethodDto, currentLanguage, includeTaxOnLineItems, temporaryReference, merchantData);
                order = await sveaWebPayClient.Checkout.UpdateOrder(orderId, updateOrderRequest).ConfigureAwait(false);
                orderGroup.Properties[Constants.Culture] = currentLanguage.TwoLetterISOLanguageName;
                return order;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
        }

        public virtual async Task<Data> GetOrder(IOrderGroup orderGroup)
        {
            var sveaWebPayOrderId = orderGroup.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
            if (!string.IsNullOrWhiteSpace(sveaWebPayOrderId) && long.TryParse(sveaWebPayOrderId, out var orderId))
            {
                var market = _marketService.GetMarket(orderGroup.MarketId);
                return await GetOrder(orderId, market, orderGroup.Properties[Constants.Culture]?.ToString()).ConfigureAwait(false);
            }

            return null;
        }

        public virtual async Task<Data> GetOrder(long orderId, IMarket market, string languageId)
        {
            var sveaWebPayClient = _sveaWebPayClientFactory.Create(market, languageId);
            try
            {
                return await sveaWebPayClient.Checkout.GetOrder(orderId).ConfigureAwait(false);
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