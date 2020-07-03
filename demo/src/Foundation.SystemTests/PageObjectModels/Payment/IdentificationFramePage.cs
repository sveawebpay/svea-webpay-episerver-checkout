﻿using Atata;
using Foundation.SystemTests.PageObjectModels.Base.Attributes;

namespace Foundation.SystemTests.PageObjectModels.Payment
{
    using _ = IdentificationFramePage;

    [WaitForLoader]
    public class IdentificationFramePage : Page<_>
    {
        [FindById("email")] public EmailInput<_> Email { get; private set; }

        [FindById("px-submit")] public Button<_> Next { get; private set; }

        [FindById("msisdn")] public TelInput<_> PhoneNumber { get; private set; }
    }
}