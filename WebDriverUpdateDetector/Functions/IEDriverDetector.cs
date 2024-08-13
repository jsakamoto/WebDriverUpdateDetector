using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace WebDriverUpdateDetector;

public class IEDriverDetector
{
    private const string IEDriverChangeLogUrl = "https://raw.githubusercontent.com/SeleniumHQ/selenium/trunk/cpp/iedriverserver/CHANGELOG";

    private const string SeleniumReleasePageUrl = "https://github.com/SeleniumHQ/selenium/releases";

    private readonly HttpClient _httpClient;

    private readonly AzureTableStorage _storage;

    private readonly Mail _mail;

    private readonly ILogger _logger;

    public IEDriverDetector(HttpClient httpClient, AzureTableStorage storage, Mail mail, ILoggerFactory loggerFactory)
    {
        this._httpClient = httpClient;
        this._storage = storage;
        this._mail = mail;
        this._logger = loggerFactory.CreateLogger<IEDriverDetector>();
    }

    [Function(nameof(IEDriverDetector))]
    public async Task Run([TimerTrigger("0 0 10,22 * * *")] TimerInfo myTimer)
    {
        this._logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

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
                    subject: "Unhandled Exception occured in IEDriver update detector",
                    body: exception.ToString());
            }
            catch { }
            throw;
        }

        this._logger.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
    }

    private async ValueTask RunCoreAsync()
    {
        var driverVersions = await this.GetIEDriverVersionsAsync();

        var table = this._storage.GetTableClient();
        var knownVersions = table.Query<WebDriverVersion>()
            .Where(row => row.PartitionKey == "IEDriver")
            .Select(row => row.RowKey)
            .ToHashSet();

        var newVersions = driverVersions
            .Where(ver => !knownVersions.Contains(ver))
            .ToArray();

        if (newVersions.Any())
        {
            await this._mail.SendAsync(
                subject: "[Chrome IEDriver] Newer versions are detected",
                body: $"Detected new versions are: {string.Join(", ", newVersions)}\n" +
                      $"\n" +
                      $"See: {SeleniumReleasePageUrl}/index.html");
        }

        foreach (var newVersion in newVersions)
        {
            await table.AddEntityAsync(new WebDriverVersion(driver: "IEDriver", newVersion));
        }
    }

    internal async ValueTask<string[]> GetIEDriverVersionsAsync()
    {
        var changeLog = await this._httpClient.GetStringAsync(IEDriverChangeLogUrl);

        var driverVersions = Regex.Matches(changeLog, @"^v(?<number>[\d\.]+)\r?$", RegexOptions.Multiline)
            .Select(m => m.Groups["number"].Value)
            .ToArray();
        return driverVersions;
    }
}
