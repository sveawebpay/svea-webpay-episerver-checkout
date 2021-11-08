using Atata;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Checkout;

namespace Foundation.SystemTests.PageObjectModels.CommerceSite.Base
{
    [WaitForDocumentReadyState(Timeout = 10)]
    public abstract class BaseCommercePage<TOwner> : Page<TOwner>
        where TOwner : BaseCommercePage<TOwner>
    {
        [FindByClass("market-selector")]
        public Clickable<TOwner> Market { get; private set; }

        [FindByContent("Sweden")]
        public Clickable<TOwner> SwedenMarket { get; private set; }

        [Wait(1, TriggerEvents.BeforeHover)]
        [FindByContent(TermMatch.Contains, "Clothing")]
        public Link<TOwner> Clothing { get; private set; }

        [FindByContent(TermMatch.Contains, "Shoes")]
        public Link<TOwner> Shoes { get; private set; }

        [FindByCss("*[data-notify='container']")]
        public Control<TOwner> Notification { get; private set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindById("js-cart")]
        public Clickable<TOwner> Cart { get; private set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindById("checkoutBtnId")]
        public Button<IdentificationCheckoutPage, TOwner> ContinueToCheckout { get; private set; }

        [FindById("js-searchbutton")]
        public Clickable<TOwner> ToggleSearch { get; private set; }

    }
}
