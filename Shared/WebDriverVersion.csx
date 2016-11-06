using Microsoft.WindowsAzure.Storage.Table;

class WebDriverVersion : TableEntity
{
    public string Name { get; set; } = "";

    public string LatestVersion { get; set; } = "";

    public WebDriverVersion()
    {
    }

    public WebDriverVersion(string name)
    {
        this.PartitionKey = "";
        this.RowKey = name;
    }
}
