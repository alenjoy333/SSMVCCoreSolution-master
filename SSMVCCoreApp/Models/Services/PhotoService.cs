using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SSMVCCoreApp.Models.Abstract;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SSMVCCoreApp.Models.Services
{
    public class PhotoService : IPhotoService
    {
        private CloudStorageAccount _storageAccount;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(IOptions<StorageUtility> storageUtility, ILogger<PhotoService> logger)
        {
            _storageAccount = storageUtility.Value.StorageAccount;
            _logger = logger;
        }

        public async Task<string> UploadPhotoAsync(string category, IFormFile photoToUpload)
        {
            if (photoToUpload == null || photoToUpload.Length == 0)
            {
                return null;
            }
            string categoryLowerCase = category.ToLower().Trim();
            string fullpath = null;
            try
            {
                //creating a blob container
                CloudBlobClient cloudBlobClient = _storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(categoryLowerCase);

                if (await cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                    _logger.LogInformation($"Successfully created a blob storage '{cloudBlobContainer.Name}' container and made it public");

                }

                string imageName = $"ProductPhoto{Guid.NewGuid().ToString()}{Path.GetExtension(photoToUpload.FileName.Substring(photoToUpload.FileName.LastIndexOf("/") + 1))}";

                //upload image to the blob

                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(imageName);
                cloudBlockBlob.Properties.ContentType = photoToUpload.ContentType;
                await cloudBlockBlob.UploadFromStreamAsync(photoToUpload.OpenReadStream());

                fullpath = cloudBlockBlob.Uri.ToString();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading file to blob");
            }
            return fullpath;
        }

        public async Task<bool> DeletePhotoAsync(string category, string photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl))
            {
                return true;
            }
            string categoryLowerCase = category.ToLower().Trim();
            bool deleteFlag = false;

            try
            {
                CloudBlobClient cloudBlobClient = _storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(categoryLowerCase);

                if (cloudBlobContainer.Name == categoryLowerCase)
                {
                    string blobName = photoUrl.Substring(photoUrl.LastIndexOf("/") + 1);
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                    deleteFlag = await cloudBlockBlob.DeleteIfExistsAsync();
                    _logger.LogInformation($"Deleted Image  {photoUrl}");
                    var items = cloudBlobContainer.GetAccountPropertiesAsync();
                    BlobResultSegment resultSegment = await cloudBlobContainer.ListBlobsSegmentedAsync(string.Empty, true, BlobListingDetails.Metadata, null, null, null, null);
                    if (!resultSegment.Results.Any())
                    {
                        await cloudBlobContainer.DeleteIfExistsAsync();
                    }
                }

                //Delete  Container if there is no files 


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deleting photo blob failed");
            }

            return deleteFlag;
        }
    }
}
