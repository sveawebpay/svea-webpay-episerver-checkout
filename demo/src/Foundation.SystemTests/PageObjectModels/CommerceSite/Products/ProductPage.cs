using Atata;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Base;

namespace Foundation.SystemTests.PageObjectModels.CommerceSite.Products
{
    using _ = ProductPage;

    public class ProductPage : BaseCommercePage<_>
    {
        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByClass("jsAddToCart")]
        public Button<_> AddToCart { get; private set; }
    }
}
