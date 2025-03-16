using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces;
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

        public async Task<IEnumerable<MediaFile>> GetMediaByProductIdAsync(long productId)
        {
            var mediaList = await _dbContext.MediaFiles.Where(media => media.ProductId == productId).ToListAsync();
            return mediaList;
        }

        public async Task AddMediaAsync(string mediaUrl, long productId)
        {
            if (string.IsNullOrEmpty(mediaUrl))
            {
                _logger.LogError("MediaFile is null or empty");
                return;
            }
            
            var mediaType = mediaUrl.EndsWith(".mp4") ? enums.MediaType.VideoMp4 :
                mediaUrl.EndsWith(".jpeg") ? enums.MediaType.ImageJpeg :
                mediaUrl.EndsWith(".png") ? enums.MediaType.ImagePng : enums.MediaType.unknown;

            var media = new MediaFile
            {
                Url = mediaUrl,
                TypeOfMedia = mediaType,
                ProductId = productId
            };
            await _dbContext.MediaFiles.AddAsync(media);
            await _dbContext.SaveChangesAsync();
        }
    }
}
