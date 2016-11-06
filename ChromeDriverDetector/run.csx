#r "System.Xml.Linq"
#load "..\Shared\WebDriverVersion.csx"
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types

public static async Task Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function started at: {DateTime.Now}");

    // Connect Azure Table Storage.
    var connStr = ConfigurationManager.AppSettings["StorageConnectionString"];
    var storageAccount = CloudStorageAccount.Parse(connStr);
    var tableClient = storageAccount.CreateCloudTableClient();
    var table = tableClient.GetTableReference("WevDriverVersions");
    await table.CreateIfNotExistsAsync();

    // Retrieve record about ChromeDriver version which last checked.
    var result = await table.ExecuteAsync(TableOperation.Retrieve<WebDriverVersion>("", "ChromeDriver"));
    var verInfo = result.Result as WebDriverVersion;
    log.Info($"Stored version is {verInfo.LatestVersion}");    

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
    if (verInfo.LatestVersion != latestVersion) {
        
        // Update the record in Azure Storage Table.
        log.Info($"Detect new version.");
        verInfo.LatestVersion = latestVersion;
        await table.ExecuteAsync(TableOperation.InsertOrReplace(verInfo));

        // Notify by e-mail.
        var smtpClient = new SmtpClient{
            Host = ConfigurationManager.AppSettings["Smtp.Host"],
            Port = int.Parse(ConfigurationManager.AppSettings["Smtp.Port"]),
            Credentials = new NetworkCredential(
                ConfigurationManager.AppSettings["Smtp.UserName"],
                ConfigurationManager.AppSettings["Smtp.Password"]
            )
        };
        var mailFrom = ConfigurationManager.AppSettings["NotifyMail.From"];
        var mailTo = ConfigurationManager.AppSettings["NotifyMail.To"];
        smtpClient.Send(
            mailFrom, 
            mailTo,
            "[WebDriver Update] Detect newer version of ChromeDriver", 
            $"See: {url}index.html");
        smtpClient.Dispose();
    }

    log.Info($"C# Timer trigger function finished at: {DateTime.Now}");    
}