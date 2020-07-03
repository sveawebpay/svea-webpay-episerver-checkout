﻿using Atata;

namespace Foundation.SystemTests.PageObjectModels.Payment
{
    [ControlDefinition("*[@data-testid='payment-plan-view']", ComponentTypeName = "PaymentPlan Item")]
    public class PaymentPlanItem<TOwner> : Control<TOwner> where TOwner : PageObject<TOwner>
    {
        public ControlList<Label<TOwner>, TOwner> Options { get; private set; }
    }
}
