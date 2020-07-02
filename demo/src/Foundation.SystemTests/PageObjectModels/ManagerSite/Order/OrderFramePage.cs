using Atata;
using Foundation.SystemTests.PageObjectModels.ManagerSite.Return;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Order
{
    using _ = OrderFramePage;

    public class OrderFramePage : Page<_>
    {
        #region Tabs

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByContent("Summary")]
        public ListItem<_> Summary { get; private set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByContent("Details")]
        public ListItem<_> Details { get; private set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByContent("Payments")]
        public ListItem<_> Payments { get; private set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByContent("Returns")]
        public ListItem<_> Returns { get; private set; }

        #endregion Tabs

        #region Summary

        [FindByXPath("//td[text()[contains(.,'SveaWebPayOrderIdField')]]/following-sibling::td")]
        public Text<_> PaymentLink { get; private set; }

        [CloseConfirmBox]
        [FindByContent("Cancel Order")]
        public Button<_> CancelOrder { get; private set; }

        #endregion

        #region Details

        [Wait(5, TriggerEvents.AfterClick)]
        [FindByContent("Complete Shipment")]
        public Button<_> CompleteShipment { get; private set; }

        [Wait(5, TriggerEvents.AfterClick)]
        [FindByContent("Release Shipment")]
        public Button<_> ReleaseShipment { get; private set; }

        [Wait(5, TriggerEvents.AfterClick)]
        [FindByContent("Cancel Shipment")]
        public Button<_> CancelShipment { get; private set; }

        [FindByContent("Create Return")]
        public Button<_> CreateReturn { get; private set; }

        [FindById("ctl03_xmlStruct_EditModeCtrl_btnHolder_SaveButton")]
        public Button<_> SaveChanges { get; private set; }

        [FindById("McCommandHandlerFrameContainer_McCommandHandlerFrameIFrame")]
        public Frame<_> CreateOrEditReturnFrame { get; private set; }

        #endregion

        #region Payment

        [FindById("ctl03_xmlStruct_PaymentsGrid_MyListView_MainGrid")]
        public Table<PaymentRowItem<_>, _> TablePayment { get; private set; }

        #endregion

        #region Returns

        [FindByClass("blockSelected")]
        public ItemsControl<ReturnTableRow<_>, _> ReturnRows { get; private set; }

        [FindById("ctl03_xmlStruct_GeneralInfoCtrl1_lblTotal")]
        public Text<_> OrderTotal { get; private set; }

        [FindById("McCommandHandlerFrameContainer_McCommandHandlerFrameIFrame")]
        public Frame<_> CreateRefundFrame { get; private set; }

        #endregion
    }
}
