using Atata;

namespace Foundation.SystemTests.PageObjectModels.CommerceSite.Products
{
    [ControlDefinition(ContainingClass = "product-tile-grid", ComponentTypeName = "Product Item")]
    public class ProductItem<TOwner> : Control<TOwner> where TOwner : PageObject<TOwner>
    {
        [FindByClass("price__discount")]
        public Text<TOwner> Price { get; private set; }

        [FindByClass("product-tile-grid__title")]
        public Link<TOwner> Name { get; private set; }

        [FindByClass("addToCart")]
        public Clickable<TOwner> AddToCart { get; private set; }
    }
}
