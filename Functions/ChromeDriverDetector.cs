using System;
using System.Linq;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text.RegularExpressions;

namespace WebDriverUpdateDetector
{
    public static class ChromeDriverDetector
    {
        [FunctionName("ChromeDriverDetector")]
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
                        "[WebDriver Update v2] Unhandled Exception occured in ChromeDriver update detector",
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

            // Retrieve record about ChromeDriver version which last checked.
            var result = await table.ExecuteAsync(TableOperation.Retrieve<WebDriverVersion>("", "ChromeDriver"));
            var verInfo = result.Result as WebDriverVersion ?? new WebDriverVersion("ChromeDriver") { LatestVersion = "0.0.0" };
            var storedVersion = verInfo.LatestVersion;
            log.Info($"Stored version is {storedVersion}");

            // Retrieve latest version information via release site.
            var latestVersion = default(string);
            var url = "https://chromedriver.storage.googleapis.com/";
            var httpClient = new HttpClient();
            using (var responseStream = await httpClient.GetStreamAsync(url))
            {
                var xdoc = XDocument.Load(responseStream);
                var xmlns = "http://doc.s3.amazonaws.com/2006-03-01";
                var versions = from contentSrc in xdoc.Descendants(XName.Get("Contents", xmlns))
                               let content = new
                               {
                                   Key = contentSrc.Element(XName.Get("Key", xmlns)).Value,
                                   LastModified = DateTime.Parse(contentSrc.Element(XName.Get("LastModified", xmlns)).Value)
                               }
                               let match = Regex.Match(content.Key, @"^(?<ver>[^/]+)/chromedriver_win32.zip$", RegexOptions.IgnoreCase)
                               where match.Success
                               orderby content.LastModified descending
                               select match.Groups["ver"].Value;
                latestVersion = versions.FirstOrDefault();
                log.Info($"Latest version is {latestVersion}");
            }

            // Check the driver was updated or not.    
            if (storedVersion != latestVersion)
            {
                log.Info($"Detect new version.");

                // Notify by e-mail.
                Mail.Send(
                    "[WebDriver Update v2] Detect newer version of ChromeDriver",
                    $"Stored version is {storedVersion}\n" +
                    $"Latest version is {latestVersion}\n" +
                    $"See: {url}index.html");

                // Update the record in Azure Storage Table.
                verInfo.LatestVersion = latestVersion;
                await table.ExecuteAsync(TableOperation.InsertOrReplace(verInfo));
            }


            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
