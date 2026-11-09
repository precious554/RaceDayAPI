using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services
{
    public class AzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobStorageService(
            BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        // Upload an image
        public async Task<string> UploadImageAsync(
            IFormFile file,
            string containerName)
        {
            var containerClient =
                _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync();

            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{extension}";

            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

            return blobClient.Uri.ToString();
        }

        // Retrieve an image from a private container
        public async Task<BlobDownloadStreamingResult?> DownloadImageAsync(
            string imageUrl)
        {
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
                return null;

            string fileName = Path.GetFileName(uri.AbsolutePath);

            string[] allowedContainers =
            {
                "profile-pictures",
                "event-banners"
            };

            string? containerName = allowedContainers.FirstOrDefault(
                name => uri.AbsolutePath.Contains(
                    $"/{name}/",
                    StringComparison.OrdinalIgnoreCase));

            if (containerName == null || string.IsNullOrWhiteSpace(fileName))
                return null;

            var containerClient =
                _blobServiceClient.GetBlobContainerClient(containerName);

            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return null;

            var download = await blobClient.DownloadStreamingAsync();

            return download.Value;
        }
    }
}