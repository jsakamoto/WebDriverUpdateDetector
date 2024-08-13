using Azure;
using Azure.Data.Tables;

namespace WebDriverUpdateDetector.Internal;

internal class WebDriverVersion : ITableEntity
{
    public string PartitionKey { get; set; } = "";

    public string RowKey { get; set; } = "";

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public WebDriverVersion()
    {
    }

    public WebDriverVersion(string driver, string version)
    {
        this.PartitionKey = driver;
        this.RowKey = version;
    }

    public override string ToString() => $"{this.PartitionKey}, v.{this.RowKey}";
}
