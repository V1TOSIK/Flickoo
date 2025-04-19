using Flickoo.Api.Entities;
using Flickoo.Api.enums;
using Flickoo.Api.Interfaces.Repositories;
using Flickoo.Api.Interfaces.Services;

namespace Flickoo.Api.Services
{
    public class MediaService : IMediaService
    {
        private readonly IStorageService _storageService;
        private readonly IMediaRepository _mediaRepository;
        private readonly ILogger<MediaService> _logger;
        public MediaService(IStorageService storageService,
            IMediaRepository mediaRepository,
            ILogger<MediaService> logger)
        {
            _storageService = storageService;
            _mediaRepository = mediaRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<string?>> GetMediaUrlsAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogWarning("Invalid product ID provided.");
                return Enumerable.Empty<string>();
            }
            var urls = await _mediaRepository.GetMediaUrlsAsync(productId);

            if (urls == null)
            {
                _logger.LogWarning($"No media found for product ID: {productId}");
                return Enumerable.Empty<string>();
            }

            return urls;
        }

        public async Task<string?> UploadMediaAsync(Stream fileStream, string fileName, string contentType, long productId)
        {
            if (fileStream == null || string.IsNullOrEmpty(fileName))
            {
                _logger.LogError("File stream or file name is null or empty");
                return null;
            }

            var mediaType = MediaType.Unknown;

            if (contentType == "image/jpeg" || contentType == "image/jpg")
                mediaType = MediaType.Jpeg;

            else if (contentType == "image/png")
                mediaType = MediaType.Png;

            else if (contentType == "video/mp4")
            {
                mediaType = MediaType.Mp4;
            }
            else
            {
                _logger.LogError("Unsupported file type");
                return null;
            }

            var mediaUrl = await _storageService.UploadMediaAsync(fileStream, fileName, contentType);

            if (mediaUrl == null)
            {
                _logger.LogError("Failed to upload media");
                return null;
            }

            var mediaResponse = await _mediaRepository.AddMediaAsync(new Media
            {
                Url = mediaUrl,
                TypeOfMedia = mediaType,
                ProductId = productId
            });

            if (!mediaResponse)
            {
                _logger.LogError("Failed to save media to database");
                return null;
            }

            return mediaUrl;
        }
        public async Task<bool> DeleteMediaAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogWarning("Invalid product ID provided.");
                return false;
            }
            var mediaUrls = await _mediaRepository.GetMediaUrlsAsync(productId);

            foreach (var url in mediaUrls)
            {
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning($"Invalid media URL found for product ID: {productId}");
                    continue;
                }

                var fileName = _mediaRepository.GetFileNameFromUrlAsync(url);

                if (string.IsNullOrEmpty(fileName))
                {
                    _logger.LogWarning($"Invalid file name extracted from URL: {url}");
                    continue;
                }
                var storageResponse = await _storageService.DeleteMediaAsync(fileName);
                if (!storageResponse)
                {
                    _logger.LogError($"Failed to delete media from storage: {fileName}");
                    return false;
                }
            }
                var dbResponse = await _mediaRepository.DeleteMediaAsync(productId);
                if (!dbResponse)
                {
                    _logger.LogError($"Failed to delete media from database for product ID: {productId}");
                    return false;
                }
            return true;
        }
    }
}
