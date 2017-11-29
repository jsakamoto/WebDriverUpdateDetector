using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebDriverUpdateDetector
{
    public static class MSWebDriverDetector
    {
        [FunctionName("MSWebDriverDetector")]
        public static async Task Run([TimerTrigger("0 0 0,12 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function started at: {DateTime.Now}");

            try
            {
                await RunCore(log);
            }
            catch (Exception exception)
            {
                try
                {
                    Mail.Send(
                        "[WebDriver Update v2] Unhandled Exception occured in MSWebDriver update detector",
                        exception.ToString());
                }
                catch { }
                throw;
            }

            log.Info($"C# Timer trigger function finished at: {DateTime.Now}");
        }

        public static async Task RunCore(TraceWriter log)
        {
            // Connect Azure Table Storage.
            var table = await AzureTableStorage.ConnectAsync();

            // Retrieve record about MSWebDriver version which last checked.
            var result = await table.ExecuteAsync(TableOperation.Retrieve<WebDriverVersion>("", "MSWebDriver"));
            var verInfo = result.Result as WebDriverVersion ?? new WebDriverVersion("MSWebDriver") { LatestVersion = "0" };
            var storedVersion = verInfo.LatestVersion;
            log.Info($"Stored version is {storedVersion}");

            // Retrieve latest version information via release site.
            var latestVersion = default(string);
            var url = "https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/";
            using (var httpClient = new HttpClient())
            {
                var content = await httpClient.GetStringAsync(url);
                var verStrs = Regex.Matches(content, @"<a[ \t]+[^>]*href=""[^""]*MicrosoftWebDriver\.[a-z0-9]+""[^>]*>(?<ver>[^<]+)</a>")
                    .Cast<Match>()
                    .Select(m => m.Groups["ver"].Value)
                    .Where(verStr => verStr != "Insiders")
                    .ToArray();
                latestVersion = verStrs
                    .Select(verStr => Regex.Match(verStr, @"^Release (?<verNum>\d+)$"))
                    .Select(m => int.Parse(m.Groups["verNum"].Value))
                    .OrderByDescending(verNum => verNum)
                    .FirstOrDefault()
                    .ToString();

                log.Info($"Latest version is {latestVersion}");
            }

            // Check the driver was updated or not.    
            if (storedVersion != latestVersion)
            {
                log.Info($"Detect new version.");

                // Notify by e-mail.
                Mail.Send(
                    "[WebDriver Update v2] Detect newer version of MSWebDriver",
                    $"Stored version is {storedVersion}\n" +
                    $"Latest version is {latestVersion}\n" +
                    $"See: {url}");

                // Update the record in Azure Storage Table.
                verInfo.LatestVersion = latestVersion;
                await table.ExecuteAsync(TableOperation.InsertOrReplace(verInfo));
            }
        }
    }
}
