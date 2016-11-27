using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types

public static async Task<CloudTable> ConnectAzureTableStorage()
{
    var connStr = ConfigurationManager.AppSettings["StorageConnectionString"];
    var storageAccount = CloudStorageAccount.Parse(connStr);
    var tableClient = storageAccount.CreateCloudTableClient();
    var table = tableClient.GetTableReference("WevDriverVersions");
    await table.CreateIfNotExistsAsync();
    return table;
}