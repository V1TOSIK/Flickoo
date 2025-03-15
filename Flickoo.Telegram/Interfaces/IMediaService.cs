namespace Flickoo.Telegram.Interfaces
{
    public interface IMediaService
    {
        string GetProductMediaPath(long chatId);
        Task<bool> SaveProductMediaFile(Stream fileStream, string fileName, long chatId);
        string GetProductMediaFilePath(long userId, string fileName);
    }
}
