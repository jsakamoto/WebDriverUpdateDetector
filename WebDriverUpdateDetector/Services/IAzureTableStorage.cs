using Azure.Data.Tables;

namespace WebDriverUpdateDetector.Services;

public interface IAzureTableStorage
{
    TableClient GetTableClient();
}
