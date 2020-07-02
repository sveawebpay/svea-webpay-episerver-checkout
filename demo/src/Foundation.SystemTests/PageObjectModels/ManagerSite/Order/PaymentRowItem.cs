using Atata;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Order
{
    [ControlDefinition("tr", ComponentTypeName = "Row")]
    public class PaymentRowItem<TOwner> : TableRow<TOwner> where TOwner : Page<TOwner>
    {
        [FindFirst]
        public CheckBox<TOwner> CheckBox { get; private set; }

        [FindByClass("serverGridInner", Index = 2)]
        public Text<TOwner> Name { get; private set; }

        [FindByClass("serverGridInner", Index = 3)]
        public Text<TOwner> TransactionType { get; private set; }

        [FindByClass("serverGridInner", Index = 4)]
        public Text<TOwner> Amount { get; private set; }

        [FindByClass("serverGridInner", Index = 5)]
        public Text<TOwner> Status { get; private set; }


    }
}