using EPiServer;
using EPiServer.Web.Routing;
using Foundation.Cms.ViewModels.Header;
using Foundation.Commerce.Customer.Services;
using Foundation.Commerce.Customer.ViewModels;
using Foundation.Demo.Models;
using Foundation.Demo.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;

namespace Foundation.Features.Header
{
    public class HeaderController : Controller
    {
        private readonly IHeaderViewModelFactory _headerViewModelFactory;
        private readonly IContentRouteHelper _contentRouteHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IAddressBookService _addressBookService;
        private static string _pluginVersion;
        private static string _sdkVersion;

        public HeaderController(IHeaderViewModelFactory headerViewModelFactory,
            IContentRouteHelper contentRouteHelper,
            IContentLoader contentLoader,
            IAddressBookService addressBookService)
        {
            _headerViewModelFactory = headerViewModelFactory;
            _contentRouteHelper = contentRouteHelper;
            _contentLoader = contentLoader;
            _addressBookService = addressBookService;
        }

        [ChildActionOnly]
        public ActionResult GetHeader(DemoHomePage homePage)
        {
            var content = _contentRouteHelper.Content;
            if (string.IsNullOrWhiteSpace(_pluginVersion))
            {
                var pluginAssembly = typeof(Svea.WebPay.Episerver.Checkout.SveaWebPayCheckoutService).Assembly;
                var pluginVersionInfo = FileVersionInfo.GetVersionInfo(pluginAssembly.Location);
                _pluginVersion = pluginVersionInfo.ProductVersion;
            }

            if (string.IsNullOrWhiteSpace(_sdkVersion))
            {
                var sdkAssembly = typeof(Svea.WebPay.SDK.SveaWebPayClient).Assembly;
                var sdkVersionInfo = FileVersionInfo.GetVersionInfo(sdkAssembly.Location);
                _sdkVersion = sdkVersionInfo.ProductVersion;
            }

            var demoHeaderViewModel = _headerViewModelFactory.CreateHeaderViewModel<DemoHeaderViewModel>(content, homePage);
            demoHeaderViewModel.PluginVersion = _pluginVersion;
            demoHeaderViewModel.SdkVersion = _sdkVersion;

            return PartialView("_Header", demoHeaderViewModel);
        }

        [ChildActionOnly]
        public ActionResult GetHeaderLogoOnly(DemoHomePage homePage)
        {
            return PartialView("_HeaderLogo", homePage);
        }

        public ActionResult GetCountryOptions(string inputName)
        {
            var model = new List<CountryViewModel>() { new CountryViewModel() { Name = "Select", Code = "undefined" } };
            model.AddRange(_addressBookService.GetAllCountries());
            ViewData["Name"] = inputName;
            return PartialView("~/Features/Shared/Foundation/DisplayTemplates/CountryOptions.cshtml", model);
        }
    }
}