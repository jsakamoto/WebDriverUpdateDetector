using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace WebDriverUpdateDetector;

public class ChromeBrowserDetector
{
    private const string ChromeBrowserPackageUrl = "https://dl.google.com/linux/chrome/deb/dists/stable/main/binary-amd64/Packages";

    private readonly HttpClient _httpClient;

    private readonly AzureTableStorage _storage;

    private readonly Mail _mail;

    private readonly ILogger _logger;

    public ChromeBrowserDetector(HttpClient httpClient, AzureTableStorage storage, Mail mail, ILoggerFactory loggerFactory)
    {
        this._httpClient = httpClient;
        this._storage = storage;
        this._mail = mail;
        this._logger = loggerFactory.CreateLogger<ChromeBrowserDetector>();
    }

    [Function(nameof(ChromeBrowserDetector))]
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
                    subject: "Unhandled Exception occured in Chrome Browser update detector",
                    body: exception.ToString());
            }
            catch { }
            throw;
        }

        this._logger.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
    }

    private async ValueTask RunCoreAsync()
    {
        using var stream = await this._httpClient.GetStreamAsync(ChromeBrowserPackageUrl);
        var browserVersions = await GetChromeBrowserVersionsAsync(stream);

        var table = this._storage.GetTableClient();
        var knownVersions = table.Query<WebDriverVersion>()
            .Where(row => row.PartitionKey == "ChromeBrowser")
            .Select(row => row.RowKey)
            .ToHashSet();

        var newVersions = browserVersions
            .Where(ver => !knownVersions.Contains(ver))
            .ToArray();

        if (newVersions.Any())
        {
            await this._mail.SendAsync(
                subject: "[Chrome Browser] Newer versions are detected",
                body: $"Detected new versions are: {string.Join(", ", newVersions)}\n");
        }

        foreach (var newVersion in newVersions)
        {
            table.AddEntity(new WebDriverVersion(driver: "ChromeBrowser", newVersion));
        }
    }

    private enum ReadingState
    {
        Leading,
        FoundStablePackage,
        EndOfReading
    }

    internal async ValueTask<IEnumerable<string>> GetChromeBrowserVersionsAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);

        var browserVersions = new List<string>();
        var state = ReadingState.Leading;
        while (!reader.EndOfStream && state != ReadingState.EndOfReading)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;
            switch (state)
            {
                case ReadingState.Leading:
                    if (Regex.IsMatch(line, "^Package:[ ]*google-chrome-stable$")) state = ReadingState.FoundStablePackage;
                    break;
                case ReadingState.FoundStablePackage:
                    if (line.StartsWith("Package:")) { state = ReadingState.EndOfReading; break; }
                    var m = Regex.Match(line, @"^Version:[ ]*(?<ver>[\d\.]+)");
                    if (m.Success)
                    {
                        browserVersions.Add(m.Groups["ver"].Value);
                        state = ReadingState.EndOfReading;
                    }
                    break;
                default: break;
            }
        }
        return browserVersions;
    }
}
