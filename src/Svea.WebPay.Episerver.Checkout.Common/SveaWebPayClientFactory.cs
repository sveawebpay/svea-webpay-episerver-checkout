using EPiServer.ServiceLocation;

using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;

using Svea.WebPay.SDK;

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace Svea.WebPay.Episerver.Checkout.Common
{
    /// <summary>
    /// Factory methods to create an instance of Svea WebPay Client
    /// Initializes it for a specific payment method and a specific market (since the API settings might vary)
    /// </summary>
    [ServiceConfiguration(typeof(ISveaWebPayClientFactory), Lifecycle = ServiceInstanceScope.Singleton)]
    public class SveaWebPayClientFactory : ISveaWebPayClientFactory
    {
        protected static readonly ConcurrentDictionary<string, HttpClient> HttpClientCache = new ConcurrentDictionary<string, HttpClient>();
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        
        public SveaWebPayClientFactory(ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public virtual ISveaClient Create(IMarket market, string languageId)
        {
            CheckoutConfiguration checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(market.MarketId, languageId);
            return GetSveaWebPayClient(checkoutConfiguration);
        }

        public virtual ISveaClient Create(PaymentMethodDto paymentMethodDto, MarketId marketMarketId)
        {
            return Create(_checkoutConfigurationLoader.GetConfiguration(paymentMethodDto, marketMarketId));
        }

        public virtual ISveaClient Create(ConnectionConfiguration connectionConfiguration)
        {
            return GetSveaWebPayClient(connectionConfiguration);
        }

        private static ISveaClient GetSveaWebPayClient(ConnectionConfiguration connectionConfiguration)
        {
            var checkoutKey = $"{connectionConfiguration.CheckoutApiUri}:{connectionConfiguration.MerchantId}:{connectionConfiguration.Secret}";
            var paymentAdminKey = $"{connectionConfiguration.PaymentAdminApiUri}:{connectionConfiguration.MerchantId}:{connectionConfiguration.Secret}";

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false
            };

            var checkoutApiHttpClient = HttpClientCache.GetOrAdd(checkoutKey, k =>
            {
                var client = new HttpClient(handler)
                {
                    BaseAddress = connectionConfiguration.CheckoutApiUri,
                    Timeout = TimeSpan.FromMinutes(10)
                };

                var sp = ServicePointManager.FindServicePoint(connectionConfiguration.CheckoutApiUri);
                sp.ConnectionLeaseTimeout = 60 * 1000; // 1 minute
                return client;
            });

            var paymentAdminApiHttpClient = HttpClientCache.GetOrAdd(paymentAdminKey, k =>
            {
                var client = new HttpClient(handler)
                {
                    BaseAddress = connectionConfiguration.PaymentAdminApiUri,
                    Timeout = TimeSpan.FromMinutes(10)
                };

                var sp = ServicePointManager.FindServicePoint(connectionConfiguration.PaymentAdminApiUri);
                sp.ConnectionLeaseTimeout = 60 * 1000; // 1 minute
                return client;
            });

            return new SveaWebPayClient(checkoutApiHttpClient, paymentAdminApiHttpClient, new Credentials(connectionConfiguration.MerchantId, connectionConfiguration.Secret));
        }
    }
}