using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System.Runtime.InteropServices;
using static Foundation.SystemTests.Tests.Base.Drivers;

namespace Foundation.SystemTests.Services
{

    public static class DriverOptionsFactory
    {
        public static DriverOptions GetDriverOptions(Driver driver)
        {
            switch (driver)
            {
                case Driver.Chrome:

                    var chromeOptions = new ChromeOptions { AcceptInsecureCertificates = true };
                    chromeOptions.AddArguments("--incognito", "--disable-infobars", "--disable-notifications", "disable-extensions");

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        chromeOptions.AddArgument("--disable-dev-shm-usage");
                        chromeOptions.AddArgument("--no-sandbox");
                        chromeOptions.AddArgument("--headless");
                    }

                    return chromeOptions;

                case Driver.Firefox:

                    var firefoxOptions = new FirefoxOptions { AcceptInsecureCertificates = true };
                    firefoxOptions.AddArgument("-private");
                    firefoxOptions.SetPreference("dom.webnotifications.enabled", false);
                    firefoxOptions.SetPreference("dom.webnotifications.enabled", false);

                    return firefoxOptions;

                case Driver.InternetExplorer:

                    return new InternetExplorerOptions
                    {
                        AcceptInsecureCertificates = true,
                        BrowserCommandLineArguments = "",
                        EnsureCleanSession = true,
                        RequireWindowFocus = false
                    };

                default:

                    throw new NotFoundException("This driver is not in the list of handled web drivers");
            }
        }
    }
}
