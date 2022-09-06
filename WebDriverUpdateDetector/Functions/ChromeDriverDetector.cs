using System.Xml.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebDriverUpdateDetector;

public static class ChromeDriverDetector
{
    private const string ChromeDiverStorageUrl = "https://chromedriver.storage.googleapis.com/";

    [FunctionName(nameof(ChromeDriverDetector))]
    public static async Task Run([TimerTrigger("0 0 10,22 * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        var configuration = Configuration.GetConfiguration();

        try
        {
            await RunCoreAsync(configuration, log);
        }
        catch (Exception exception)
        {
            try
            {
                await Mail.SendAsync(configuration,
                    "Unhandled Exception occured in ChromeDriver update detector",
                    exception.ToString());
            }
            catch { }
            throw;
        }

        log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
    }

    private static async ValueTask RunCoreAsync(IConfiguration configuration, ILogger log)
    {
        var driverVersions = GetChromeDriverVersionns();

        var table = AzureTableStorage.Connect(configuration);
        var knownVersions = table.Query<WebDriverVersion>()
            .Where(row => row.PartitionKey == "ChromeDriver")
            .Select(row => row.RowKey)
            .ToHashSet();

        var newVersions = driverVersions
            .Where(ver => !knownVersions.Contains(ver))
            .ToArray();

        if (newVersions.Any())
        {
            await Mail.SendAsync(configuration,
                "Detect newer version of ChromeDriver",
                $"Detected new versions are: {string.Join(", ", newVersions)}\n" +
                $"\n" +
                $"See: {ChromeDiverStorageUrl}index.html");
        }

        foreach (var newVersion in newVersions)
        {
            table.AddEntity(new WebDriverVersion(driver: "ChromeDriver", newVersion));
        }
    }

    private static string[] GetChromeDriverVersionns()
    {
        var xdoc = XDocument.Load(ChromeDiverStorageUrl);
        var xmlns = "http://doc.s3.amazonaws.com/2006-03-01";
        var driverVersions = xdoc.Descendants(XName.Get("Key", xmlns))
            .Select(xe => xe.Value.ToLower())
            .Where(val => val.EndsWith("/chromedriver_win32.zip"))
            .Select(val => val.Split('/')[0])
            .ToArray();
        return driverVersions;
    }
}
