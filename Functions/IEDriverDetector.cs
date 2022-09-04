using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebDriverUpdateDetector;

public static class IEDriverDetector
{
    private const string IEDiverStorageUrl = "https://selenium-release.storage.googleapis.com/";

    [FunctionName("IEDriverDetector")]
    public static void Run([TimerTrigger("0 0 10,22 * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        var configuration = Configuration.GetConfiguration();

        try
        {
            RunCore(configuration, log);
        }
        catch (Exception exception)
        {
            try
            {
                Mail.Send(configuration,
                    "[WebDriver Update v4] Unhandled Exception occured in IEDriver update detector",
                    exception.ToString());
            }
            catch { }
            throw;
        }

        log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
    }

    private static void RunCore(IConfiguration configuration, ILogger log)
    {
        var driverVersions = GetIEDriverVersionns();

        var table = AzureTableStorage.Connect(configuration);
        var knownVersions = table.Query<WebDriverVersion>()
            .Where(row => row.PartitionKey == "IEDriver")
            .Select(row => row.RowKey)
            .ToHashSet();

        var newVersions = driverVersions
            .Where(ver => !knownVersions.Contains(ver))
            .ToArray();

        if (newVersions.Any())
        {
            Mail.Send(configuration,
                "[WebDriver Update v4] Detect newer version of IEDriver",
                $"Detected new versions are: {string.Join(", ", newVersions)}\n" +
                $"\n" +
                $"See: {IEDiverStorageUrl}index.html");
        }

        foreach (var newVersion in newVersions)
        {
            table.AddEntity(new WebDriverVersion(driver: "IEDriver", newVersion));
        }
    }

    private static string[] GetIEDriverVersionns()
    {
        var xdoc = XDocument.Load(IEDiverStorageUrl);
        var xmlns = "http://doc.s3.amazonaws.com/2006-03-01";
        var driverVersions = xdoc.Descendants(XName.Get("Key", xmlns))
            .Select(xe => xe.Value.ToLower())
            .Where(val => Regex.IsMatch(val, @"/iedriverserver_win32_.+\.zip$"))
            .Select(val => val.Split('/')[0])
            .ToArray();
        return driverVersions;
    }
}
