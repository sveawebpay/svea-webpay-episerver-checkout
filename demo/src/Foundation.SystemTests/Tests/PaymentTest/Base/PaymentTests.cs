using Atata;
using Microsoft.Extensions.Configuration;
using Foundation.SystemTests.PageObjectModels;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Base;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Checkout;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Products;
using Foundation.SystemTests.PageObjectModels.ManagerSite.Base;
using Foundation.SystemTests.Services;
using Foundation.SystemTests.Test.Helpers;
using Foundation.SystemTests.Tests.Base;
using Foundation.SystemTests.Tests.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Svea.WebPay.SDK;
using OpenQA.Selenium;

namespace Foundation.SystemTests.Tests.PaymentTest
{
    public abstract class PaymentTests : TestBase
    {
        protected decimal _totalAmount;
        protected string _totalAmountStr;
        protected string _shippingAmount;
        protected string _currency;
        protected string _orderId;

        protected SveaWebPayClient _sveaClient;
        protected readonly IConfigurationRoot _config;

        protected PaymentTests(string driverAlias) : base(driverAlias) 
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
#if DEBUG
                .AddUserSecrets("12d0917f-ffaa-469e-8b43-aa5abc9d7b65")
#endif
                .AddEnvironmentVariables()
                .Build();

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            var checkoutApihttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_config.GetSection("SveaApiUrls").GetSection("CheckoutApiUri").Value)
            };
            var paymentAdminApiHttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_config.GetSection("SveaApiUrls").GetSection("PaymentAdminApiUri").Value)
            };

            _sveaClient = new SveaWebPayClient(
                checkoutApihttpClient,
                paymentAdminApiHttpClient,
                new Credentials(
                    _config.GetSection("Credentials").GetSection("MerchantId").Value,
                    _config.GetSection("Credentials").GetSection("Secret").Value
                )
            );
        }

        #region Method Helpers

        public HomeCommercePage GoToCommerceHomePage()
        {
            return Go.To<HomeCommercePage>(url: _config.GetSection("FoundationCommerce").GetSection("Url").Value)
                .Market.Click()
                .SwedenMarket.ClickAndGo<HomeCommercePage>();
        }

        public ProductsPage SelectProducts(Product[] products)
        {
            return GoToCommerceHomePage()
                .Clothing.Hover()
                .Shoes.ClickAndGo<ProductsPage>()
                .Do(x => 
                {
                    foreach (var product in products)
                    {
                        var index = x.ProductList.IndexOf(y => !products.Any(p => p.Name == y.Name.Content.Value) && (y.Price.IsPresent)).Value;

                        x.ProductList[index].Price.StorePrice(out var price, " ");
                        x.ProductList[index].Name.StoreValue(out var name);

                        product.UnitPrice = price;
                        product.Name = name;

                        for(int i = 0; i < product.Quantity; i++)
                        {
                            x
                            .ProductList[index].Hover()
                            .ProductList[index].AddToCart.IsVisible.WaitTo.BeTrue()
                            .ProductList[index].AddToCart.Click()
                            .Notification.IsVisible.WaitTo.Within(15).BeTrue();
                        }
                    }
                });
        }

        public CheckoutPage GoToCheckoutPage(Product[] products)
        {
            return SelectProducts(products)
                .Notification.IsVisible.WaitTo.Within(10).BeFalse()
                .Cart.Click()
                .ContinueToCheckout.ClickAndGo()
                .ContinueAsGuest.IsVisible.WaitTo.BeTrue()
                .ContinueAsGuest.ClickAndGo<CheckoutPage>()
                .RefreshPage()
                .SveaCheckout.Click()
                .TotalAmount.StoreAmount(out _totalAmountStr, ".")
                .TotalAmount.StorePrice(out _totalAmount, ".");
        }

        public SveaPaymentFramePage GoToSveaPaymentFrame(Product[] products)
        {
            return GoToCheckoutPage(products)
                .SveaCheckout.Click()
                .PaymentFrame.SwitchTo<SveaPaymentFramePage>();
        }

        protected HomeCommercePage GoToThankYouPage(Product[] products, Checkout.Option checkout = Checkout.Option.Identification, Entity.Option entity = Entity.Option.Private, PaymentMethods.Option paymentMethod = PaymentMethods.Option.Card)
        {
            var page = GoToSveaPaymentFrame(products);

            try
            {
                page.IdentifyEntity(checkout, entity);
            }
            catch (StaleElementReferenceException)
            {
                page.RefreshPage()
                    .SwitchToFrame<SveaPaymentFramePage>(By.Id("svea-checkout-iframe"))
                    .IdentifyEntity(checkout, entity);
            }

            page.Pay(checkout, entity, paymentMethod, _totalAmountStr);

            try
            {
                return page
                    .SwitchToRoot<HomeCommercePage>()
                    .ToggleSearch.IsVisible.WaitTo.BeTrue();
            }
            catch (StaleElementReferenceException)
            {
                return Go.To<HomeCommercePage>();
            }
        }

        public ManagerPage GoToManagerPage()
        {
            return Go.To<HomeManagerPage>(url: _config.GetSection("ManagerCommerce").GetSection("Url").Value)
                .UserName.Set(TestDataService.ManagerUsername)
                .Password.Set(TestDataService.ManagerPassword)
                .Login.ClickAndGo();
        }

        #endregion

        protected static IEnumerable TestData(bool singleProduct = true)
        {
            var data = new List<object>();

            if (singleProduct)
                data.Add(new[]
                {
                    new Product { Quantity = 1 }
                });
            else
                data.Add(new[]
                {
                    new Product { Quantity = 3 },
                    new Product { Quantity = 2 }
                });

            yield return data.ToArray();
        }
    }
}
