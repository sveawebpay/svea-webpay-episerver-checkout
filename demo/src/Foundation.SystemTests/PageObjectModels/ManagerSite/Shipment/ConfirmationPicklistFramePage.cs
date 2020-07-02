using Atata;
using Foundation.SystemTests.PageObjectModels.ManagerSite.Base;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Shipment
{
    using _ = ConfirmationPickListFramePage;

    public class ConfirmationPickListFramePage : BaseManagerPage<_>
    {
        [FindById("ctl01_Shipments_TrackingNumber_0")]
        public TextInput<_> TrackingNumber { get; private set; }

        [FindById("ctl01_btnSave")]
        public Clickable<_> Confirm { get; private set; }
    }
}
