using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace WebDriverUpdateDetector;

internal class AzureTableStorage
{
    public static TableClient Connect(IConfiguration configuration)
    {
        var tableServiceClient = new TableServiceClient(configuration["AzureWebJobsStorage"]);
        var tableClient = tableServiceClient.GetTableClient(tableName: "WebDriverVersions");
        tableClient.CreateIfNotExists();
        return tableClient;
    }
}
