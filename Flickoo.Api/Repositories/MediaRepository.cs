using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Repositories
{
    public class MediaRepository : IMediaRepository
    {
        private readonly ILogger<MediaRepository> _logger;
        private readonly FlickooDbContext _dbContext;
        public MediaRepository(ILogger<MediaRepository> logger,
            FlickooDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Media>> GetMediaByProductIdAsync(long productId)
        {
            var mediaList = await _dbContext.Medias
                .Where(media => media.ProductId == productId)
                .ToListAsync();
            if (mediaList == null)
            {
                _logger.LogWarning($"GetMediaByProductIdAsync: No media found for product ID {productId}.");
                return Enumerable.Empty<Media>();
            }
            else
            {
                _logger.LogInformation($"GetMediaByProductIdAsync: Media retrieved for product ID {productId}.");

                return mediaList;
            }
        }

        public async Task<bool> AddProductMediasAsync(long productId, List<string> mediaUrls)
        {
            if (mediaUrls == null || !mediaUrls.Any())
            {
                _logger.LogError("AddProductMediasAsync: Media URLs list is null or empty.");
                return false;
            }
            foreach (var mediaUrl in mediaUrls)
            {
                var result = await AddMediaAsync(productId, mediaUrl);
                if (!result)
                {
                    _logger.LogError($"AddProductMediasAsync: Failed to add media URL {mediaUrl} for product ID {productId}.");
                    continue;
                }
            }
            _logger.LogInformation($"AddProductMediasAsync: Media URLs added for product ID {productId}.");
            return true;
        }

        private async Task<bool> AddMediaAsync(long productId, string mediaUrl)
        {
            if (string.IsNullOrEmpty(mediaUrl))
            {
                _logger.LogError("MediaFile is null or empty");
                return false;
            }

            var mediaType = mediaUrl.EndsWith(".mp4") ? enums.MediaType.VideoMp4 :
                mediaUrl.EndsWith(".jpeg") ? enums.MediaType.ImageJpeg :
                mediaUrl.EndsWith(".png") ? enums.MediaType.ImagePng :
                enums.MediaType.Unknown;

            var product = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                _logger.LogWarning($"AddMediaAsync: Product with ID {productId} not found.");
                return false;
            }

            var media = new Media
            {
                Url = mediaUrl,
                TypeOfMedia = mediaType,
                ProductId = productId,
                Product = product,
            };
            await _dbContext.Medias.AddAsync(media);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"AddMediaAsync: Media added for product ID {productId}.");
            return true;
        }

        public async Task<bool> UpdateProductMediasAsync(long productId, List<string> mediaUrls)
        {
            if (mediaUrls == null || !mediaUrls.Any())
            {
                _logger.LogError("UpdateProductMediasAsync: Media URLs list is null or empty.");
                return false;
            }
            var existingMedia = await GetMediaByProductIdAsync(productId);
            if (existingMedia == null || !existingMedia.Any())
            {
                _logger.LogWarning($"UpdateProductMediasAsync: No existing media found for product ID {productId}.");
                return false;
            }
            foreach (var media in existingMedia)
            {
                _dbContext.Medias.Remove(media);
            };
            await _dbContext.SaveChangesAsync();
            foreach (var mediaUrl in mediaUrls)
            {
                var result = await AddMediaAsync(productId, mediaUrl);
                if (!result)
                {
                    _logger.LogError($"UpdateProductMediasAsync: Failed to update media URL {mediaUrl} for product ID {productId}.");
                    continue;
                }
            }
            _logger.LogInformation($"UpdateProductMediasAsync: Media URLs updated for product ID {productId}.");
            return true;
        }

        public async Task<bool> DeleteProductMediasAsync(long productId)
        {
            if (productId == 0)
            {
                _logger.LogError("DeleteProductMediasAsync: Invalid product ID provided.");
                return false;
            }
            var existingMedia = await GetMediaByProductIdAsync(productId);
            if (existingMedia == null || !existingMedia.Any())
            {
                _logger.LogWarning($"DeleteProductMediasAsync: No existing media found for product ID {productId}.");
                return false;
            }
            foreach (var media in existingMedia)
            {
                _dbContext.Medias.Remove(media);
            }
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"DeleteProductMediasAsync: Media deleted for product ID {productId}.");
            return true;
        }
    }
}
