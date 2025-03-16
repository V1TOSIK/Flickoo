using Flickoo.Api.DTOs;
using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces
{
    public interface IMediaRepository
    {
        public Task<IEnumerable<MediaFile>> GetMediaByProductIdAsync(long productId);
        public Task AddMediaAsync(string mediaUrl, long productId);

    }
}
