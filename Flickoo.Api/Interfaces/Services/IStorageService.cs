namespace Flickoo.Api.Interfaces.Services
{
    public interface IStorageService
    {
        Task<string?> UploadMediaAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteMediaAsync(string fileName);
    }
}
