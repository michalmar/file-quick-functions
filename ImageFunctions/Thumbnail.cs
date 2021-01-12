// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

// Learn how to locally debug an Event Grid-triggered function:
//    https://aka.ms/AA30pjh

// Use for local testing:
//   https://{ID}.ngrok.io/runtime/webhooks/EventGrid?functionName=Thumbnail

using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Sas;


namespace ImageFunctions
{
    public static class Thumbnail
    {
        private static readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static readonly string BLOB_STORAGE_NAME = Environment.GetEnvironmentVariable("AzureWebJobsStorageName");
        private static readonly string BLOB_STORAGE_KEY = Environment.GetEnvironmentVariable("AzureWebJobsStorageKey");
        private static readonly string BLOB_STORAGE_CONTAINER = Environment.GetEnvironmentVariable("THUMBNAIL_CONTAINER_NAME");
        private static readonly int BLOB_SAS_LENTGTH = Int32.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorageSASTokenLenghtHours"));
        
        private static string GetBlobNameFromUrl(string bloblUrl)
        {
            var uri = new Uri(bloblUrl);
            var blobClient = new BlobClient(uri);
            return blobClient.Name;
        }

        private static string GenSASToken(string blobName)
        {

            // Create a URI to the storage account
            Uri accountUri = new Uri("https://" + BLOB_STORAGE_NAME + ".blob.core.windows.net/");

            // Create BlobServiceClient from the account URI
            BlobServiceClient blobServiceClient = new BlobServiceClient(accountUri);

            // Get reference to the container
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(BLOB_STORAGE_CONTAINER);

            string sasBlobUri = "";

            if (container.Exists())
            {
                // Set the expiration time and permissions for the container.
                // In this case, the start time is specified as a few 
                // minutes in the past, to mitigate clock skew.
                // The shared access signature will be valid immediately.
                BlobSasBuilder sas = new BlobSasBuilder
                {
                    Resource = "c",
                    BlobContainerName = BLOB_STORAGE_CONTAINER,
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(BLOB_SAS_LENTGTH)
                };

                sas.SetPermissions(BlobContainerSasPermissions.All);

                // Create StorageSharedKeyCredentials object by reading
                // the values from the configuration (appsettings.json)
                StorageSharedKeyCredential storageCredential =
                    new StorageSharedKeyCredential(BLOB_STORAGE_NAME, BLOB_STORAGE_KEY);

                // Create a SAS URI to the storage account
                UriBuilder sasUri = new UriBuilder(accountUri);
                sasUri.Query = sas.ToSasQueryParameters(storageCredential).ToString();


                // Create the URI using the SAS query token.
                sasBlobUri = container.Uri + "/" + blobName + sasUri.Query;
            }
                return sasBlobUri;
        }



        [FunctionName("Thumbnail")]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            [Blob("{data.url}", FileAccess.Read)] Stream input,
            ILogger log)
        {
            try
            {
                if (input != null)
                {
                    var createdEvent = ((JObject)eventGridEvent.Data).ToObject<StorageBlobCreatedEventData>();
                    var extension = Path.GetExtension(createdEvent.Url);

                    var thumbContainerName = Environment.GetEnvironmentVariable("THUMBNAIL_CONTAINER_NAME");
                    var blobServiceClient = new BlobServiceClient(BLOB_STORAGE_CONNECTION_STRING);
                    var blobContainerClient = blobServiceClient.GetBlobContainerClient(thumbContainerName);
                    var blobName = GetBlobNameFromUrl(createdEvent.Url);

                    await blobContainerClient.UploadBlobAsync(blobName, input);


                    // create Short (AKA) URL
                    var tmp_short_url = "http://aka.file1";
                    
                    //Store URL in Azure Tables
                    //init table storage
                    var table = TableService.GetTableReference(BLOB_STORAGE_CONNECTION_STRING, "ShortLinks");
                    if (table == null)
                    {
                        log.LogInformation($"table null: {createdEvent.Url}");
                        throw new ArgumentNullException(nameof(table));
                    }
                    LinkEntity newlink = new LinkEntity()
                                {
                                    PartitionKey = blobName,
                                    RowKey = blobName,
                                    ShareFileName = blobName,
                                    ShortLink = tmp_short_url,
                                    SASLink = GenSASToken(blobName),
                                };
                    
                    TableService.AddObject(table, newlink);              
                }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                throw;
            }
        }
    }
}
