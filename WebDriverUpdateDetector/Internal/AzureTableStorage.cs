using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using WebDriverUpdateDetector.Services;

namespace WebDriverUpdateDetector;

internal class AzureTableStorage : IAzureTableStorage
{
    private readonly IConfiguration _configuration;

    public AzureTableStorage(IConfiguration configuration)
    {
        this._configuration = configuration;
    }

    public TableClient GetTableClient()
    {
        var tableServiceClient = new TableServiceClient(this._configuration["AzureWebJobsStorage"]);
        var tableClient = tableServiceClient.GetTableClient(tableName: "WebDriverVersions");
        tableClient.CreateIfNotExists();
        return tableClient;
    }
}
