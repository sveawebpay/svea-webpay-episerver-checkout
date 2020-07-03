using Atata;
using Foundation.SystemTests.PageObjectModels.ManagerSite.Base;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Order
{
    [ControlDefinition("tr", ComponentTypeName = "Row")]
    public class OrderRowItem<TOwner> : TableRow<TOwner> where TOwner : BaseManagerPage<TOwner>
    {
        [FindFirst]
        public CheckBox<TOwner> CheckBox { get; private set; }

        [FindFirst]
        public Link<OrderFramePage, TOwner> Link { get; private set; }
    }
}