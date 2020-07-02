using Atata;
using NUnit.Framework;
using OpenQA.Selenium.Chrome;
using System;
using Foundation.SystemTests.Services;
using LogLevel = Atata.LogLevel;
using static Foundation.SystemTests.Tests.Base.Drivers;

namespace Foundation.SystemTests.Tests.Base
{
    [TestFixture(DriverAliases.Chrome)]
    public abstract class TestBase
    {
        private readonly string _driverAlias;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            AtataContext.GlobalConfiguration.
                UseChrome().
                    WithOptions(DriverOptionsFactory.GetDriverOptions(Driver.Chrome) as ChromeOptions).
                UseVerificationTimeout(TimeSpan.FromSeconds(3)).
                UseElementFindTimeout(TimeSpan.FromSeconds(15)).
                UseWaitingTimeout(TimeSpan.FromSeconds(30)).
                AddNUnitTestContextLogging().
                WithMinLevel(LogLevel.Trace).
                TakeScreenshotOnNUnitError().
                    AddScreenshotFileSaving().
                        WithFolderPath(() => $@"Logs\{AtataContext.BuildStart:yyyy-MM-dd HH_mm_ss}").
                        WithFileName(screenshotInfo => $"{AtataContext.Current.TestName} - {screenshotInfo.PageObjectFullName}").
                UseTestName(() => $"[{_driverAlias}]{TestContext.CurrentContext.Test.Name}");
        }

        [SetUp]
        public void SetUp()
        {
#if DEBUG
            AtataContext.Configure()
                .UseDriver(_driverAlias)
            .Build();
            AtataContext.Current.Driver.Maximize();
#elif DEV
            AtataContext.Configure()
                .UseDriver(_driverAlias)
            .Build();
            AtataContext.Current.Driver.Maximize();
#elif RELEASE
            
            AtataContext.Configure()
                .UseChrome()
                .WithOptions(DriverOptionsFactory.GetDriverOptions(Driver.Chrome) as ChromeOptions)
                .Build();
            AtataContext.Current.Driver.Maximize();
#endif
        }

        protected TestBase(string driverAlias) => _driverAlias = driverAlias;

        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                TestContext.Out.WriteLine(PageSource());
            }

            AtataContext.Current?.CleanUp();
        }


        [OneTimeTearDown]
        public void GlobalDown()
        {
            foreach (Driver driverType in Enum.GetValues(typeof(Driver)))
                WebDriverCleanerService.KillWebDriverProcess(WebDriverCleanerService.DriverNames[driverType]);
        }

        public static string PageSource()
        {
            return $"------ Start Page content ------"
                + Environment.NewLine
                + Environment.NewLine
                + AtataContext.Current.Driver.PageSource
                + Environment.NewLine
                + Environment.NewLine
                + "------ End Page content ------";
        }
    }
}
