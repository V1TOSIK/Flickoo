using Flickoo.Api.Interfaces.Services;
using Minio;
using Minio.DataModel.Args;

namespace Flickoo.Api.Services
{
    public class MinioService : IStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<MinioService> _logger;
        private readonly string _bucketName = "flickoo-media";
        public MinioService(ILogger<MinioService> logger, IConfiguration config)
        {
            _logger = logger;

            _minioClient = new MinioClient()
            .WithEndpoint(config["Minio:Endpoint"])
            .WithCredentials(config["Minio:AccessKey"], config["Minio:SecretKey"])
            .Build();

            EnsureBucketExists().Wait();
        }

        private async Task EnsureBucketExists()
        {
            var found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if (found)
            {
                _logger.LogInformation($"Bucket '{_bucketName}' already exists.");
            }
            else
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                _logger.LogInformation($"Bucket '{_bucketName}' created.");
            }
        }

        public async Task<string?> UploadMediaAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var objectName = $"{fileName}";

                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType));

                var url = $"http://localhost:9000/{_bucketName}/{objectName}";
                _logger.LogInformation($"File uploaded to MinIO: {url}");

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload media to MinIO");
                return null;
            }
        }

        public async Task<bool> DeleteMediaAsync(string fileName)
        {
            try
            {
                await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName));
                _logger.LogInformation($"File deleted from MinIO: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete media from MinIO");
                return false;
            }
        }
    }
}
