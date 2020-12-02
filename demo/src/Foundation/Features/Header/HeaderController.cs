using EPiServer.Web.Routing;
using Foundation.Features.Home;
using Foundation.Features.MyAccount.AddressBook;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;

namespace Foundation.Features.Header
{
    public class HeaderController : Controller
    {
        private readonly IHeaderViewModelFactory _headerViewModelFactory;
        private readonly IContentRouteHelper _contentRouteHelper;
        private readonly IAddressBookService _addressBookService;
        private static string _pluginVersion;
        private static string _sdkVersion;

        public HeaderController(IHeaderViewModelFactory headerViewModelFactory,
            IContentRouteHelper contentRouteHelper,
            IAddressBookService addressBookService)
        {
            _headerViewModelFactory = headerViewModelFactory;
            _contentRouteHelper = contentRouteHelper;
            _addressBookService = addressBookService;
        }

        [ChildActionOnly]
        public ActionResult GetHeader(HomePage homePage)
        {
            var content = _contentRouteHelper.Content;
            
            if (string.IsNullOrWhiteSpace(_pluginVersion))
            {
                var pluginAssembly = typeof(Startup).Assembly;
                var pluginVersionInfo = FileVersionInfo.GetVersionInfo(pluginAssembly.Location);
                _pluginVersion = pluginVersionInfo.ProductVersion;
            }

            if (string.IsNullOrWhiteSpace(_sdkVersion))
            {
                var sdkAssembly = typeof(Svea.WebPay.SDK.SveaWebPayClient).Assembly;
                var sdkVersionInfo = FileVersionInfo.GetVersionInfo(sdkAssembly.Location);
                _sdkVersion = sdkVersionInfo.ProductVersion;
            }

            var headerViewModel = _headerViewModelFactory.CreateHeaderViewModel(content, homePage);

            headerViewModel.PluginVersion = _pluginVersion;
            headerViewModel.SdkVersion = _sdkVersion;

            return PartialView("_Header", headerViewModel);
        }

        [ChildActionOnly]
        public ActionResult GetHeaderLogoOnly()
        {
            return PartialView("_HeaderLogo", _headerViewModelFactory.CreateHeaderLogoViewModel());
        }

        public ActionResult GetCountryOptions(string inputName)
        {
            var model = new List<CountryViewModel>() { new CountryViewModel() { Name = "Select", Code = "undefined" } };
            model.AddRange(_addressBookService.GetAllCountries());
            ViewData["Name"] = inputName;
            return PartialView("~/Features/Shared/Views/DisplayTemplates/CountryOptions.cshtml", model);
        }
    }
}