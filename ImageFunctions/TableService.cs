using Microsoft.Azure.Cosmos.Table;
using System;

namespace ImageFunctions
{
    public static class TableService
    {
        public static CloudTable GetTableReference(string storageConnString, string tableName, bool createIfNotExists = false)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(storageConnString);
            CloudTableClient client = account.CreateCloudTableClient();

            var table = client.GetTableReference(tableName);

            if (createIfNotExists)
            {
                table.CreateIfNotExists();
            }

            return table;
        }

        public static void FillWithSampleData(CloudTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            LinkEntity[] links = new LinkEntity[] { 
                new LinkEntity()
                {
                    PartitionKey = "1",
                    RowKey = "1",
                    ShareFileName = "file1",
                    ShortLink = "http://aka.file1",
                    SASLink = "http://file1",
                },
                new LinkEntity()
                {
                    PartitionKey = "2",
                    RowKey = "2",
                    ShareFileName = "file2",
                    ShortLink = "http://aka.file2",
                    SASLink = "http://file2",
                },
                new LinkEntity()
                {
                    PartitionKey = "3",
                    RowKey = "3",
                    ShareFileName = "file3",
                    ShortLink = "http://aka.file3",
                    SASLink = "http://file3",
                },
                new LinkEntity()
                {
                    PartitionKey = "4",
                    RowKey = "4",
                    ShareFileName = "file4",
                    ShortLink = "http://aka.file4",
                    SASLink = "http://file4",
                },
            };

            foreach (var lnk in links)
            {
                AddObject(table, lnk);
            }
        }

        public static void AddObject<T>(CloudTable table, T value) where T : ITableEntity
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            TableOperation operation = TableOperation.InsertOrReplace(value);
            table.Execute(operation);
        }

        public static string StorageConnectionString
        {
            get { return Environment.GetEnvironmentVariable(TableService.StorageConnectionStringVariableName); }
        }

        /// <summary>
        /// Environment variable name for the storage connection string.
        /// </summary>
        public const string StorageConnectionStringVariableName = "StorageConnectionString";
    }
}
