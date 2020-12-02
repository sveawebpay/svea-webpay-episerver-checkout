namespace Svea.WebPay.Episerver.Checkout.Common.Extensions
{
	public static class StringExtensions
    {
	    public static string TrimIfNecessary(this string value, int maxCharLength)
	    {
		    if (string.IsNullOrWhiteSpace(value))
		    {
			    return value;
		    }

		    if (value.Length <= maxCharLength)
		    {
			    return value;
		    }

		    return value.Substring(0, maxCharLength);
	    }
    }
}
