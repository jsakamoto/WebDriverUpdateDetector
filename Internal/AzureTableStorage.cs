using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebDriverUpdateDetector
{
    internal static class AzureTableStorage
    {
        public static async Task<CloudTable> ConnectAsync()
        {
            var connStr = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            var storageAccount = CloudStorageAccount.Parse(connStr);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("WevDriverVersions");
            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}
