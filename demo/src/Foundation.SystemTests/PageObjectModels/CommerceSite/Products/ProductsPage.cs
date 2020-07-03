using Atata;
using Foundation.SystemTests.PageObjectModels.CommerceSite.Base;

namespace Foundation.SystemTests.PageObjectModels.CommerceSite.Products
{
    using _ = ProductsPage;

    public class ProductsPage : BaseCommercePage<_>
    {
        [FindByClass("category-page__products")]
        public ControlList<ProductItem<_>, _> ProductList { get; private set; }

        [Wait(1, TriggerEvents.BeforeClick)]
        [FindByClass("addToCart")]
        public Clickable<_> AddToCart { get; private set; }

    }
}
