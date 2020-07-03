using Atata;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Base;

namespace Foundation.SystemTests.PageObjectModels.CommerceSite.Checkout
{
    using _ = CheckoutPage;

    public class CheckoutPage : BaseCommercePage<_>
    {
        [FindById("svea-checkout-iframe")]
        public Frame<SveaPaymentFramePage, _> PaymentFrame { get; private set; }

        [FindByXPath("//p[contains(text(),'Total for cart')]/following-sibling::p")]
        public Text<_> TotalAmount { get; private set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByContent(TermMatch.Contains, "Svea Checkout")]
        public Label<_> SveaCheckout { get; private set; }
    }
}
