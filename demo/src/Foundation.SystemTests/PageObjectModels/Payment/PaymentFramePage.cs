using Atata;
using Foundation.SystemTests.PageObjectModels.Base.Attributes;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Base;
using Foundation.SystemTests.PageObjectModels.ManagerSite;

namespace Foundation.SystemTests.PageObjectModels.Payment
{
    using _ = PaymentFramePage;

    [WaitForLoader]
    public class PaymentFramePage : BaseCommercePage<_>
    {
        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByClass("paymentmenu-container")]
        public ControlList<PayexItem<_>, _> PaymentMethods { get; set; }

    }
}