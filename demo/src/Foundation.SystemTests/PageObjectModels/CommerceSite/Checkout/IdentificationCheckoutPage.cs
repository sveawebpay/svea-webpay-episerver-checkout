using Atata;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Base;

namespace Foundation.SystemTests.PageObjectModels.CommerceSite.Checkout
{
    using _ = IdentificationCheckoutPage;

    public class IdentificationCheckoutPage : BaseCommercePage<_>
    {
        [FindByClass("jsContinueCheckoutMethod")]
        public Button<CheckoutPage, _> ContinueAsGuest { get; private set; }
    }
}
