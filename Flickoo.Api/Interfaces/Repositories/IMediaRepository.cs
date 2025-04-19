using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces.Repositories
{
    public interface IMediaRepository
    {
        Task<IEnumerable<string?>> GetMediaUrlsAsync(long productId);
        Task<bool> IsMediaExistAsync(long productId, string url);
        string GetFileNameFromUrlAsync(string url);
        Task<bool> AddMediaAsync(Media media);
        Task<bool> DeleteMediaAsync(long productId);
    }
}
