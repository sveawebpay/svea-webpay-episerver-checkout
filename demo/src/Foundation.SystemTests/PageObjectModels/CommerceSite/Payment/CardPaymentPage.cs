﻿using Atata;

namespace Foundation.SystemTests.PageObjectModels.Payment
{
    using _ = CardPaymentPage;

    public class CardPaymentPage : Page<_>
    {
        [FindByDescendantAttribute("value", values: "credit")]
        public Label<_> CreditCard { get; set; }

        [FindByDescendantAttribute("value", values: "debit")]
        public Label<_> DebitCard { get; set; }

        [FindById("CardNumber")] 
        public TextInput<_> CardNumber { get; set; }

        [FindById("Expiry")]
        public TextInput<_> Expiry { get; set; }

        [FindById("CVV")]
        public TextInput<_> Cvc { get; set; }

        [FindById("AmountDiv")]
        public TextInput<_> TotalAmount { get; set; }
        
        [FindById("submit-button")]
        public Button<_> Submit { get; set; }

    }
}