using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces.Repositories
{
    public interface IMediaRepository
    {
        Task<IEnumerable<Media>> GetMediaByProductIdAsync(long productId);
        Task<bool> AddProductMediasAsync(long productId, List<string> mediaUrls);
        Task<bool> UpdateProductMediasAsync(long productId, List<string> mediaUrls);
        Task<bool> DeleteProductMediasAsync(long productId);
    }
}
