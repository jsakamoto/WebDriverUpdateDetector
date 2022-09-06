using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebDriverUpdateDetector;

public static class IEDriverDetector
{
    private const string IEDriverChangeLogUrl = "https://raw.githubusercontent.com/SeleniumHQ/selenium/trunk/cpp/iedriverserver/CHANGELOG";

    private const string SeleniumReleasePageUrl = "https://github.com/SeleniumHQ/selenium/releases";

    [FunctionName(nameof(IEDriverDetector))]
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
                    "Unhandled Exception occured in IEDriver update detector",
                    exception.ToString());
            }
            catch { }
            throw;
        }

        log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
    }

    private static async ValueTask RunCoreAsync(IConfiguration configuration, ILogger log)
    {
        var driverVersions = await GetIEDriverVersionsAsync(IEDriverChangeLogUrl);

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
            await Mail.SendAsync(configuration,
                "Detect newer version of IEDriver",
                $"Detected new versions are: {string.Join(", ", newVersions)}\n" +
                $"\n" +
                $"See: {SeleniumReleasePageUrl}index.html");
        }

        foreach (var newVersion in newVersions)
        {
            await table.AddEntityAsync(new WebDriverVersion(driver: "IEDriver", newVersion));
        }
    }

    internal static async ValueTask<string[]> GetIEDriverVersionsAsync(string ieDriverChangeLogUrl)
    {
        using var httpClient = new HttpClient();
        var changeLog = await httpClient.GetStringAsync(ieDriverChangeLogUrl);

        var driverVersions = Regex.Matches(changeLog, @"^v(?<number>[\d\.]+)\r?$", RegexOptions.Multiline)
            .Select(m => m.Groups["number"].Value)
            .ToArray();
        return driverVersions;
    }
}
