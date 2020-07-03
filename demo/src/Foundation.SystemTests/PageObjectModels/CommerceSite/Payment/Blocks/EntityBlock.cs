﻿using Atata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Foundation.SystemTests.PageObjectModels.Payment
{
    [ControlDefinition("div[@data-testid='country-identification-view']", ComponentTypeName = "Entity Block")]
    public class EntityBlock<TOwner> : Control<TOwner> where TOwner : PageObject<TOwner>
    {
        [FindByContent("Företag")]
        public Text<TOwner> IsCompany { get; set; }

        [FindByContent("Privatperson")]
        public Text<TOwner> IsPrivate { get; set; }

        [FindByCss("button[data-testid='anonymous-toggle']")]
        public Button<TOwner> ToggleIdentification { get; private set; }
    }
}
