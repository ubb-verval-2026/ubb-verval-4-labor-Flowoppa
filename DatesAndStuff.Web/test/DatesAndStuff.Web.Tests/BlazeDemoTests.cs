using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class BlazeDemoTests
{
    private IWebDriver driver;
    private const string BaseURL = "https://blazedemo.com";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
    }

    [Test]
    [TestCase(500)]
    [TestCase(300)]
    public void Demo_CheckFlights_AtLeastThree(double priceThreshold)
    {
        driver.Navigate().GoToUrl(BaseURL);

        new SelectElement(driver.FindElement(By.Name("fromPort"))).SelectByText("Mexico City");
        new SelectElement(driver.FindElement(By.Name("toPort"))).SelectByText("Dublin");
        driver.FindElement(By.XPath("//input[@value='Find Flights']")).Click();

        IWebElement table = driver.FindElement(By.XPath("//table[@class='table']"));

        var rows = table.FindElements(By.XPath("./tbody/tr"));

        foreach (var row in rows)
        {
            var priceText = row.FindElement(By.XPath("./td[6]")).Text;

            if (double.TryParse(priceText.Replace("$", ""), out double price))
            {
                if (price < priceThreshold)
                {
                    ITakesScreenshot ssdriver = (ITakesScreenshot)row;
                    Screenshot screenshot = ssdriver.GetScreenshot();
                    
                    string fileName = $"/home/floppa/FloppaCode/III_II/VerVal/ubb-verval-4-labor-Flowoppa/DatesAndStuff.Web/test/screenshots/{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    screenshot.SaveAsFile(fileName);
                    break;
                }
            }
        }

        int flightCount = rows.Count;

        flightCount.Should().BeGreaterThanOrEqualTo(3);
    }
}
