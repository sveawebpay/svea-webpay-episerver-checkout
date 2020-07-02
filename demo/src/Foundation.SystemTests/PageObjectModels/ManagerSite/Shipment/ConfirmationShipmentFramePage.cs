using Atata;
using Foundation.SystemTests.PageObjectModels.ManagerSite.Base;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Shipment
{
    using _ = ConfirmationShipmentFramePage;

    public class ConfirmationShipmentFramePage : BaseManagerPage<_>
    {
        [FindByContent(TermMatch.Contains, "OK")]
        public Button<_> Confirm { get; private set; }
    }
}
