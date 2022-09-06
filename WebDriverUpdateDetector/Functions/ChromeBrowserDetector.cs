using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebDriverUpdateDetector;

public static class ChromeBrowserDetector
{
    private const string ChromeBrowserPackageUrl = "https://dl.google.com/linux/chrome/deb/dists/stable/main/binary-amd64/Packages";

    [FunctionName(nameof(ChromeBrowserDetector))]
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
                    "Unhandled Exception occured in Chrome Browser update detector",
                    exception.ToString());
            }
            catch { }
            throw;
        }

        log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
    }

    private static async ValueTask RunCoreAsync(IConfiguration configuration, ILogger log)
    {
        using var httpClient = new HttpClient();
        using var stream = await httpClient.GetStreamAsync(ChromeBrowserPackageUrl);
        var browserVersions = await GetChromeBrowserVersionsAsync(stream);

        var table = AzureTableStorage.Connect(configuration);
        var knownVersions = table.Query<WebDriverVersion>()
            .Where(row => row.PartitionKey == "ChromeBrowser")
            .Select(row => row.RowKey)
            .ToHashSet();

        var newVersions = browserVersions
            .Where(ver => !knownVersions.Contains(ver))
            .ToArray();

        if (newVersions.Any())
        {
            await Mail.SendAsync(configuration,
                "Detect newer version of Chrome Browser",
                $"Detected new versions are: {string.Join(", ", newVersions)}\n");
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

    internal static async ValueTask<IEnumerable<string>> GetChromeBrowserVersionsAsync(Stream stream)
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
