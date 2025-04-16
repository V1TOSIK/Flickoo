namespace Flickoo.Api.Interfaces.Services
{
    public interface IMediaService
    {
        Task<IEnumerable<string?>> GetMediaUrlsAsync(long productId);
        Task<string?> UploadMediaAsync(Stream fileStream, string fileName, string contentType, long productId);
        Task<bool> DeleteMediaAsync(long productId);
    }
}
