using EPiServer.ServiceLocation;

using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.Interfaces;

using Svea.WebPay.Episerver.Checkout.Common;

using System;
using System.Linq;

namespace Svea.WebPay.Episerver.Checkout.CommerceManager.Apps.Order.Payments.Plugins.SveaWebPayCheckout
{
    public partial class ConfigurePayment : System.Web.UI.UserControl, IGatewayControl
    {
        private IMarketService _marketService;
        private ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private PaymentMethodDto _paymentMethodDto;

        protected void Page_Load(object sender, EventArgs e)
        {
            _marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();

            if (IsPostBack || _paymentMethodDto?.PaymentMethodParameter == null)
                return;

            var markets = _paymentMethodDto.PaymentMethod.First().GetMarketPaymentMethodsRows();
            if (markets == null || markets.Length == 0)
            {
                pnl_marketselected.Visible = true;
                pnl_parameters.Visible = false;
                return;
            }



            var market = _marketService.GetMarket(markets.First().MarketId);
            var checkoutConfiguration = GetConfiguration(market.MarketId, _paymentMethodDto.PaymentMethod.First().LanguageId);
            BindConfigurationData(checkoutConfiguration);
            BindMarketData(markets);
        }


        protected void marketDropDownList_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            var checkoutConfiguration = GetConfiguration(new MarketId(marketDropDownList.SelectedValue), _paymentMethodDto.PaymentMethod.First().LanguageId);
            BindConfigurationData(checkoutConfiguration);
            ConfigureUpdatePanelContentPanel.Update();
        }

        protected void CountryList_CountryMoved(object sender, EventArgs e)
        {
        }


        public void SaveChanges(object dto)
        {
            if (!Visible)
            {
                return;
            }

            var paymentMethod = dto as PaymentMethodDto;
            if (paymentMethod == null)
            {
                return;
            }
            var currentMarket = marketDropDownList.SelectedValue;

            
            var configuration = new CheckoutConfiguration
            {
                MarketId = currentMarket,
                CheckoutApiUri = !string.IsNullOrWhiteSpace(txtCheckoutApiUri.Text) ? new Uri(txtCheckoutApiUri.Text) : null,
                PaymentAdminApiUri = !string.IsNullOrWhiteSpace(txtPaymentAdminApiUri.Text) ? new Uri(txtPaymentAdminApiUri.Text) : null,
                MerchantId = txtMerchantId.Text,
                Secret = txtSecret.Text
            };

            try
            {
                configuration.PushUri = !string.IsNullOrWhiteSpace(txtPushUri.Text)
                    ? new Uri(txtPushUri.Text)
                    : null;
                configuration.TermsUri =
                    !string.IsNullOrWhiteSpace(txtTermsUri.Text) ? new Uri(txtTermsUri.Text) : null;
                configuration.CheckoutUri = !string.IsNullOrWhiteSpace(txtCheckoutUri.Text)
                    ? new Uri(txtCheckoutUri.Text)
                    : null;
                configuration.ConfirmationUri = !string.IsNullOrWhiteSpace(txtConfirmationUri.Text)
                    ? new Uri(txtConfirmationUri.Text)
                    : null;
                configuration.CheckoutValidationCallbackUri =
                    !string.IsNullOrWhiteSpace(txtCheckoutValidationCallbackUri.Text) ? new Uri(txtCheckoutValidationCallbackUri.Text) : null;

                configuration.ActivePartPaymentCampaigns = txtActivePartPaymentCampaigns.Text?.Split(';')
                    .Select(long.Parse).ToList();

                if(long.TryParse(txtPromotedPartPaymentCampaign.Text, out long promotedPartPaymentCampaign))
                {
                    configuration.PromotedPartPaymentCampaign = promotedPartPaymentCampaign;
                }
                else
                {
                    configuration.PromotedPartPaymentCampaign = null;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                _checkoutConfigurationLoader.SetConfiguration(configuration, paymentMethod, currentMarket);
            }
        }

        public void LoadObject(object dto)
        {
            var paymentMethod = dto as PaymentMethodDto;
            if (paymentMethod == null)
            {
                return;
            }
            _paymentMethodDto = paymentMethod;
        }

        public string ValidationGroup { get; set; }

        private void BindMarketData(PaymentMethodDto.MarketPaymentMethodsRow[] markets)
        {
            marketDropDownList.DataSource = markets.Select(m => m.MarketId);
            marketDropDownList.DataBind();
        }

        public void BindConfigurationData(CheckoutConfiguration checkoutConfiguration)
        {
            txtCheckoutApiUri.Text = checkoutConfiguration.CheckoutApiUri?.ToString();
            txtPaymentAdminApiUri.Text = checkoutConfiguration.PaymentAdminApiUri?.ToString();
            txtMerchantId.Text = checkoutConfiguration.MerchantId;
            txtSecret.Text = checkoutConfiguration.Secret;

            txtPushUri.Text = checkoutConfiguration.PushUri?.ToString();
            txtTermsUri.Text = checkoutConfiguration.TermsUri?.ToString();
            txtCheckoutUri.Text = checkoutConfiguration.CheckoutUri?.ToString();
            txtConfirmationUri.Text = checkoutConfiguration.ConfirmationUri?.ToString();
            txtCheckoutValidationCallbackUri.Text = checkoutConfiguration.CheckoutValidationCallbackUri?.ToString();
            txtActivePartPaymentCampaigns.Text =
                checkoutConfiguration.ActivePartPaymentCampaigns != null &&
                checkoutConfiguration.ActivePartPaymentCampaigns.Any()
                    ? string.Join(";", checkoutConfiguration.ActivePartPaymentCampaigns)
                    : null;
            txtPromotedPartPaymentCampaign.Text = checkoutConfiguration.PromotedPartPaymentCampaign.ToString();
        }


    
        private CheckoutConfiguration GetConfiguration(MarketId marketId, string languageId)
        {
            try
            {
                return _checkoutConfigurationLoader.GetConfiguration(marketId, languageId);
            }
            catch
            {
                return new CheckoutConfiguration();
            }
        }
    }
}