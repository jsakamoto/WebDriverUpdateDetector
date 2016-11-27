#r "System.Xml.Linq"
#load "..\Shared\AzureTableStorage.csx"
#load "..\Shared\WebDriverVersion.csx"
#load "..\Shared\SendMail.csx"
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function started at: {DateTime.Now}");
    
    try
    {
        await RunCore(log);
    }
    catch (Exception exception)
    {
        try {
            SendMail(
                "[WebDriver Update] Unhandled Exception occured in IEDriver update detector",
                exception.ToString());
        }
        catch {}
        throw;
    }

    log.Info($"C# Timer trigger function finished at: {DateTime.Now}");    
}

public static async Task RunCore(TraceWriter log)
{
    // Connect Azure Table Storage.
    var table = await ConnectAzureTableStorage();

    // Retrieve record about IEDriver version which last checked.
    var result = await table.ExecuteAsync(TableOperation.Retrieve<WebDriverVersion>("", "IEDriver"));
    var verInfo = result.Result as WebDriverVersion ?? new WebDriverVersion("IEDriver") { LatestVersion = "0.0.0" };
    var storedVersion = verInfo.LatestVersion;
    log.Info($"Stored version is {storedVersion}");

    // Retrieve latest version information via release site.
    var latestVersion = default(string);
    var url = "https://selenium-release.storage.googleapis.com/";
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
                       let match = Regex.Match(content.Key, @"/IEDriverServer_Win32_(?<ver>.+)\.zip$", RegexOptions.IgnoreCase)
                       where match.Success
                       orderby content.LastModified descending
                       select match.Groups["ver"].Value;
        latestVersion = versions.FirstOrDefault();
        log.Info($"Latest version is {latestVersion}");    
    }

    // Check the driver was updated or not.    
    if (storedVersion != latestVersion) {
        log.Info($"Detect new version.");
        
        // Notify by e-mail.
        SendMail(
            "[WebDriver Update] Detect newer version of IEDriver",
            $"Stored version is {storedVersion}\n"+
            $"Latest version is {latestVersion}\n"+ 
            $"See: {url}index.html");

        // Update the record in Azure Storage Table.
        verInfo.LatestVersion = latestVersion;
        await table.ExecuteAsync(TableOperation.InsertOrReplace(verInfo));
    }
}