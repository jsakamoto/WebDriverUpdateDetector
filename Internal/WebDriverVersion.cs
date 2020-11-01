using Microsoft.Azure.Cosmos.Table;

namespace WebDriverUpdateDetector
{
    internal class WebDriverVersion : TableEntity
    {
        public WebDriverVersion()
        {
        }

        public WebDriverVersion(string driver, string version)
        {
            PartitionKey = driver;
            RowKey = version;
        }

        public override string ToString() => $"{PartitionKey}, v.{RowKey}";
    }
}
