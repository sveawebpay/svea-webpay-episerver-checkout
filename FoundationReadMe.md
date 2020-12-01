## Changes to the code

#### The Currency values of both arguments must match 
To solve the error ´The Currency values of both arguments must match´ when changing to SWE market, update the price filter to only fetch prices with current currency.

In `Foundation.Commerce.Extensions.EntryContentBaseExtensions.cs` add following in the top of the file

```CSharp
private static readonly Lazy<ICurrencyService> CurrencyService =
            new Lazy<ICurrencyService>(() => ServiceLocator.Current.GetInstance<CurrencyService>());
```
And in method `Prices` in the same file, when creating PriceFilter, change to following
```CSharp

            var priceFilter = new PriceFilter
            {
                CustomerPricing = new[] { CustomerPricing.AllCustomers },
                Currencies = new Currency[] { CurrencyService.Value.GetCurrentCurrency() }
            };
```

#### Update price when changing quantity on checkout
When changing quantity on a line item, price in Svea window isn't updated. That's because the cart isn't saved before inializing the payment. To solve this, add `orderRepository.Save(CartWithValidationIssues.Cart);` directly after `_cartService.ChangeQuantity()` has been called. In `Foundation.Features.Checkout.CheckoutController.cs` and the method `ChangeCartItem`


## Changes in web.config  
To solve the error found in console:   
`Refused to load the script ‘https://checkoutapistage.svea.com/merchantscript/build/index.js?v=04413584f1c78f11b29c2c8153501919’ because it violates the following Content Security Policy directive: “script-src ‘self’ ‘unsafe-inline’ ‘unsafe-eval’ https://dc.services.visualstudio.com https://az416426.vo.msecnd.net https://code.jquery.com https://maxcdn.bootstrapcdn.com https://www.facebook.com https://dl.episerver.net”. Note that ‘script-src-elem’ was not explicitly set, so ‘script-src’ is used as a fallback.`

Update customHeaders with name `Content-Security-Policy` to allow fetching content and script from https://checkoutapistage.svea.com/

```xml
    <httpProtocol>
      <customHeaders>
        <remove name="X-Powered-By" />
       <add name="Content-Security-Policy" value="default-src 'self' https://checkoutapistage.svea.com/ ws: wss: data:; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://checkoutapistage.svea.com/ https://dc.services.visualstudio.com https://az416426.vo.msecnd.net https://code.jquery.com https://maxcdn.bootstrapcdn.com *.facebook.com *.facebook.net *.episerver.net *.bing.com *.virtualearth.net; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com *.episerver.net *.bing.com; font-src 'self' https://fonts.gstatic.com data:; connect-src 'self' https://dc.services.visualstudio.com ws: wss: *.bing.com *.virtualearth.net; img-src 'self' data: http: https:; child-src 'self' https://checkoutapistage.svea.com/ *.powerbi.com *.vimeo.com *.youtube.com *.facebook.com;" />
        <add name="X-XSS-Protection" value="1; mode=block" />
        <add name="X-Content-Type-Options" value="nosniff " />
      </customHeaders>
    </httpProtocol>
```
