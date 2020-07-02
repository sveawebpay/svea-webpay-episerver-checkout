using Atata;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Base;

namespace Foundation.SystemTests.PageObjectModels.CommerceSite.ThankYou
{
    using _ = ThankYouPage;

    public class ThankYouPage : BaseCommercePage<_>
    {
        [FindByContent(TermMatch.Contains, "Tack för din beställning!")]
        public Text<_> ThankYouMessage { get; private set; }

        [FindByContent(TermMatch.Contains, "Order ID:")]
        public H2<_> OrderId { get; private set; }
    }
}
