using Atata;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Return
{
    using _ = CreateRefundFramePage;

    public class CreateRefundFramePage : Page<_>
    {
        [FindById("ctl01_tbAmount")]
        public TextInput<_> Amount { get; private set; }

        [FindById("ctl01_btnSave")]
        public Button<_> Confirm { get; private set; }
    }
}
