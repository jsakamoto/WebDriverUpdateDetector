using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;

namespace WebDriverUpdateDetector
{
    internal class AzureTableStorage
    {
        public static CloudTable Connect(IConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration["AzureWebJobsStorage"]);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("WebDriverVersions");
            table.CreateIfNotExists();
            return table;
        }
    }
}
