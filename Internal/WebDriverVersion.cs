using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebDriverUpdateDetector
{
    internal class WebDriverVersion : TableEntity
    {
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
}
