using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WebDriverUpdateDetector.Internal;
using WebDriverUpdateDetector.Services;

namespace WebDriverUpdateDetector;

public class ChromeDriverDetector
{
    private const string ChromeDiverVersionUrl = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json";

    private readonly HttpClient _httpClient;

    private readonly IAzureTableStorage _storage;

    private readonly IMail _mail;

    private readonly ILogger _logger;

    public ChromeDriverDetector(HttpClient httpClient, IAzureTableStorage storage, IMail mail, ILoggerFactory loggerFactory)
    {
        this._httpClient = httpClient;
        this._storage = storage;
        this._mail = mail;
        this._logger = loggerFactory.CreateLogger<ChromeDriverDetector>();
    }

    [Function(nameof(ChromeDriverDetector))]
    public async Task Run([TimerTrigger("0 0 10,22 * * *")] TimerInfo myTimer)
    {
        var version = typeof(ChromeDriverDetector).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        this._logger.LogInformation($"ChromeDriverDetector {version} executed at: {DateTime.Now}");

        try
        {
            await this.RunCoreAsync();
        }
        catch (Exception exception)
        {
            this._logger.LogError(exception, exception.Message);
            try
            {
                await this._mail.SendAsync(
                    subject: "Unhandled Exception occured in ChromeDriver update detector",
                    body: exception.ToString());
            }
            catch { }
            throw;
        }

        this._logger.LogInformation($"ChromeDriverDetector {version} finished at: {DateTime.Now}");
    }

    private async ValueTask RunCoreAsync()
    {
        var driverVersions = await this.GetChromeDriverVersionsAsync();

        var table = this._storage.GetTableClient();
        var knownVersions = table.Query<WebDriverVersion>()
            .Where(row => row.PartitionKey == "ChromeDriver")
            .Select(row => row.RowKey)
            .ToHashSet();

        var newVersions = driverVersions
            .Where(ver => !knownVersions.Contains(ver))
            .ToArray();

        if (newVersions.Any())
        {
            await this._mail.SendAsync(
                subject: "[Chrome Driver] Newer versions are detected",
                body: $"Detected new versions are: {string.Join(", ", newVersions)}\n" +
                      $"\n" +
                      $"See: {ChromeDiverVersionUrl}");
        }

        foreach (var newVersion in newVersions)
        {
            table.AddEntity(new WebDriverVersion(driver: "ChromeDriver", newVersion));
        }
    }

    internal async ValueTask<IEnumerable<string>> GetChromeDriverVersionsAsync()
    {
        var versionInfo = await this._httpClient.GetFromJsonAsync<ChromeDriverVersionInfo>(ChromeDiverVersionUrl);
        if (versionInfo == null) throw new InvalidOperationException("Failed to get ChromeDriver version info.");
        return new[] { versionInfo.Channels.Stable.Version, versionInfo.Channels.Beta.Version }.Distinct();
    }
}
