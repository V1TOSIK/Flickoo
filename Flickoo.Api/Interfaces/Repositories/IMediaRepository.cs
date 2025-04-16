using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces.Repositories
{
    public interface IMediaRepository
    {
        Task<IEnumerable<string?>> GetMediaUrlsAsync(long productId);
        Task<bool> AddMediaAsync(Media media);
        Task<bool> DeleteMediaAsync(long productId);
    }
}
