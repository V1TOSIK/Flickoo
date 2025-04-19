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
        public async Task<IEnumerable<string?>> GetMediaUrlsAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogWarning("Invalid product ID provided.");
                return Enumerable.Empty<string>();
            }

            var medias = await _dbContext.Medias
                .AsNoTracking()
                .Where(m => m.ProductId == productId)
                .Select(m => m.Url)
                .ToListAsync();

            if (medias == null || !medias.Any())
            {
                _logger.LogWarning($"No media found for product ID: {productId}");
                return Enumerable.Empty<string>();
            }
            _logger.LogInformation($"Retrieved {medias.Count} media URLs for product ID: {productId}");
            return medias;
        }

        public async Task<bool> IsMediaExistAsync(long productId, string url)
        {
            if (productId < 0 || string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Invalid product ID or URL provided.");
                return false;
            }
            var mediaExist = await _dbContext.Medias
                .AnyAsync(m => m.ProductId == productId && m.Url == url);
            if (mediaExist)
            {
                _logger.LogInformation($"Media exists for product ID: {productId} and URL: {url}");
            }
            else
            {
                _logger.LogWarning($"No media found for product ID: {productId} and URL: {url}");
            }
            return mediaExist;
        }

        public string GetFileNameFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Invalid URL provided.");
                return string.Empty;
            }
            var splitedUrl = url.Split('/');
            if (splitedUrl.Length == 0)
            {
                _logger.LogWarning($"Failed to split URL: {url}");
                return string.Empty;
            }
            var fileName = $"{splitedUrl[splitedUrl.Length - 2]}/{splitedUrl[splitedUrl.Length - 1]}";

            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning($"Failed to extract file name from URL: {url}");
                return string.Empty;
            }
            _logger.LogInformation($"Extracted file name: {fileName} from URL: {url}");
            return fileName;
        }

        public async Task<bool> AddMediaAsync(Media media)
        {
            if (media == null)
            {
                _logger.LogWarning("Null media object provided.");
                return false;
            }

            try
            {
                var mediaExist = await _dbContext.Medias.AnyAsync(m => m.Url == media.Url);

                if (mediaExist)
                {
                    _logger.LogInformation("AddMediaAsync: media is exist");
                    return true;
                }
                await _dbContext.Medias.AddAsync(media);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Media added for product ID: {media.ProductId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add media");
                return false;
            }
        }

        public async Task<bool> DeleteMediaAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogWarning("Invalid product ID provided.");
                return false;
            }
            try
            {
                var deleteResult = await _dbContext.Medias
                    .Where(m => m.ProductId == productId)
                    .ExecuteDeleteAsync();

                if (deleteResult == 0)
                {
                    _logger.LogWarning($"No media found for product ID: {productId}");
                    return false;
                }
                _logger.LogInformation($"Media deleted for product ID: {productId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete media");
                return false;
            }
        }

    }
}
