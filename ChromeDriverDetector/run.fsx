#r "System.Xml.Linq"
#r "System.Configuration.dll"
#r "System.Net.Http.dll"
#r "Microsoft.WindowsAzure.Storage.dll"
open System
open System.Configuration
open System.Net
open System.Net.Http
open System.Net.Mail
open System.Text.RegularExpressions;
open System.Xml.Linq
open Microsoft.WindowsAzure.Storage // Namespace for CloudStorageAccount
open Microsoft.WindowsAzure.Storage.Table // Namespace for Table storage types

let SendMail subject body =
    let appSettings = ConfigurationManager.AppSettings
    use smtpClient = new SmtpClient()
    smtpClient.Host <- appSettings.["Smtp.Host"]
    smtpClient.Port <- appSettings.["Smtp.Port"] |> Int32.Parse
    smtpClient.Credentials <- new NetworkCredential(appSettings.["Smtp.UserName"], appSettings.["Smtp.Password"])
    smtpClient.Send(appSettings.["NotifyMail.From"], appSettings.["NotifyMail.To"], subject, body)

type WebDriverVersion(name, latestVer) = 
    inherit TableEntity("", name)
    new () = WebDriverVersion("", "")
    member val LatestVersion = latestVer with get, set

let Run(myTimer: TimerInfo, log: TraceWriter) =

    let connStr = ConfigurationManager.AppSettings.["StorageConnectionString"]
    let storageAccount = CloudStorageAccount.Parse(connStr)
    let tableClient = storageAccount.CreateCloudTableClient()
    let table = tableClient.GetTableReference("WevDriverVersions")
    table.CreateIfNotExists() |> ignore
    

    let result = table.Execute(TableOperation.Retrieve<WebDriverVersion>("", "ChromeDriver"))
    let verInfo = match result.Result with
                | null -> new WebDriverVersion("ChromeDriver", "0.0.0" )
                | _ -> result.Result :?> WebDriverVersion
    let storedVersion = verInfo.LatestVersion


    let url = "https://chromedriver.storage.googleapis.com/"
    let httpClient = new HttpClient()
    let responseStream = httpClient.GetStreamAsync(url).Result

    let xdoc = XDocument.Load(responseStream)
    let xmlns = "http://doc.s3.amazonaws.com/2006-03-01"
    let valueOf (name:string) (element:XElement) = element.Element(XName.Get(name, xmlns)).Value
    let latestVerOrNone = 
        xdoc.Descendants(XName.Get("Contents", xmlns))
        |> Seq.sortByDescending (valueOf "LastModified" >> DateTime.Parse)
        |> Seq.map (valueOf "Key")
        |> Seq.map (fun key -> Regex.Match(key, "^(?<ver>[^/]+)/chromedriver_win32.zip$", RegexOptions.IgnoreCase))
        |> Seq.filter (fun m -> m.Success)
        |> Seq.map (fun m -> m.Groups.["ver"].Value)
        |> Seq.tryHead

    let latestVersion  = 
        match latestVerOrNone with
        | Some ver -> ver
        | _ -> ""

    if latestVersion <> storedVersion then
        // Notify by e-mail
        let subject = "[WebDriver Update] Detect newer version of ChromeDriver"
        let body = 
            (sprintf "Stored version is %s\n" storedVersion) +
            (sprintf "Latest version is %s\n" latestVersion) +
            (sprintf "See: %sindex.html" url)
        SendMail subject body

        // Update the record in Azure Storage Table.
        verInfo.LatestVersion <- latestVersion
        table.Execute(TableOperation.InsertOrReplace(verInfo)) |> ignore
