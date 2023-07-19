using System.Net.Http.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebDriverUpdateDetector.Internal;

namespace WebDriverUpdateDetector;

public static class ChromeDriverDetector
{
    private const string ChromeDiverVersionUrl = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json";

    [FunctionName(nameof(ChromeDriverDetector))]
    public static async Task Run([TimerTrigger("0 0 10,22 * * *")] TimerInfo myTimer, ILogger log)
    {
        var version = typeof(ChromeDriverDetector).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        log.LogInformation($"ChromeDriverDetector {version} executed at: {DateTime.Now}");
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

        log.LogInformation($"ChromeDriverDetector {version} finished at: {DateTime.Now}");
    }

    private static async ValueTask RunCoreAsync(IConfiguration configuration, ILogger log)
    {
        var driverVersions = await GetChromeDriverVersionsAsync(ChromeDiverVersionUrl);

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
                $"See: {ChromeDiverVersionUrl}");
        }

        foreach (var newVersion in newVersions)
        {
            table.AddEntity(new WebDriverVersion(driver: "ChromeDriver", newVersion));
        }
    }

    internal static async ValueTask<IEnumerable<string>> GetChromeDriverVersionsAsync(string chromeDiverVersionUrl)
    {
        using var httpClient = new HttpClient();
        var versionInfo = await httpClient.GetFromJsonAsync<ChromeDriverVersionInfo>(chromeDiverVersionUrl);
        if (versionInfo == null) throw new InvalidOperationException("Failed to get ChromeDriver version info.");
        return new[] { versionInfo.Channels.Stable.Version, versionInfo.Channels.Beta.Version }.Distinct();
    }
}
