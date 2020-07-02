using Atata;
using Foundation.SystemTests.PageObjectModels.Base;
using Foundation.SystemTests.PageObjectModels.Base.Attributes;

namespace Foundation.SystemTests.PageObjectModels.Payment
{
    using _ = PayexCardFramePage;

    [WaitForLoader]
    public class PayexCardFramePage : Page<_>
    {
        [FindById("panInput")] 
        public TelInput<_> CreditCardNumber { get; set; }

        [FindById(TermMatch.Contains, "cvcInput")]
        public TelInput<_> Cvc { get; set; }

        [FindById("expiryInput")] public TelInput<_> ExpiryDate { get; set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindById("px-submit")] public Button<_> Pay { get; set; }

    }
}