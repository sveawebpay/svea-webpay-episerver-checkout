﻿using Atata;

namespace Foundation.SystemTests.PageObjectModels.Payment
{
    [ControlDefinition("*[@data-testid='direct-bank-view']", ComponentTypeName = "DirektBank Item")]
    public class DirektBankItem<TOwner> : Control<TOwner> where TOwner : PageObject<TOwner>
    {
        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByCss("label[data-testid='YDirectBank_TDirectBank_ODBNORDEASE_S']")]
        public Clickable<TOwner> Nordea { get; private set; }
    }
}
