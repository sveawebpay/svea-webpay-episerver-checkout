using Atata;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Return
{
    using _ = NewLineItemFramePage;

    public class NewLineItemFramePage : Page<_>
    {
        [FindById("ctl01_OriginalLineItems")]
        public Select<_> Item { get; private set; }

        [FindById("ctl01_ReturnQuantity")]
        public TextInput<_> Quantity { get; private set; }

        [FindById("ctl01_btnSave")]
        public Button<_> Confirm { get; private set; }
    }
}
