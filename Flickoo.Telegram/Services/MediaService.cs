using Flickoo.Telegram.Interfaces;
using Telegram.Bot;

namespace Flickoo.Telegram.Services
{
    public class MediaService : IMediaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MediaService> _logger;
        private readonly ITelegramBotClient _botClient;
        private string _mainDirectory = Path.Combine(@"..", "MediaFolder");

        public MediaService(HttpClient httpClient,
            ILogger<MediaService> logger,
            ITelegramBotClient botClient)
        {
            _httpClient = httpClient;
            _logger = logger;
            _botClient = botClient;
        }

        public string GetProductMediaPath(long chatId)
        {
            var userDirectory = Path.Combine(_mainDirectory, chatId.ToString());

            Directory.CreateDirectory(userDirectory);

            return userDirectory;
        }

        public async Task<bool> SaveProductMediaFile(Stream fileStream, string fileName, long chatId)
        {
            var userDirectory = GetProductMediaPath(chatId);

            var filePath = Path.Combine(userDirectory, fileName);

            try
            {
                using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(file);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving media file: {ex.Message}");
                return false;
            }
        }

        public string GetProductMediaFilePath(long userId, string fileName)
        {
            return Path.Combine(_mainDirectory, userId.ToString(), fileName);
            
           
        }
    }
}
