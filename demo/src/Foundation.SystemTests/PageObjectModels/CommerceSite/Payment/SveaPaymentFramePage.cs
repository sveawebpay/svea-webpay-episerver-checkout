﻿using Atata;
using Foundation.SystemTests.PageObjectModels.Payment;

namespace Foundation.SystemTests.PageObjectModels
{
    using _ = SveaPaymentFramePage;

    public class SveaPaymentFramePage : Page<_>
    {
        public EntityBlock<_> Entity { get; private set; }

        public DetailsB2BIdentificationBlock<_> B2BIdentification { get; private set; }

        public DetailsB2BAnonymousBlock<_> B2BAnonymous { get; private set; }

        public DetailsB2CIdentificationBlock<_> B2CIdentification { get; private set; }

        public DetailsB2CAnonymousBlock<_> B2CAnonymous { get; private set; }

        public PaymentMethodsBlock<_> PaymentMethods { get; private set; }

        [Wait(1, TriggerEvents.BeforeAndAfterClick)]
        [FindByCss("button[data-testid='submit-button']")]
        public Button<_> Submit { get; private set; }
    }
}