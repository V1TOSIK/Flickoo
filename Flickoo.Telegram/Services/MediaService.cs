using Flickoo.Telegram.Interfaces;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Flickoo.Telegram.Services
{
    public class MediaService : IMediaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MediaService> _logger;

        public MediaService(HttpClient httpClient,
            ILogger<MediaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }


        public async Task<string> GetMediaIdFromMsg(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken)
        {
            if (msg == null)
            {
                _logger.LogWarning("Пусте повідомлення.");
                return "";
            }

            if (msg.Type == MessageType.Photo)
            {
                var photo = msg.Photo?.LastOrDefault();
                if (photo == null)
                {
                    _logger.LogWarning("Пусте фото.");
                    await botClient.SendMessage(chatId, "Не вдалося зберегти фото", cancellationToken: cancellationToken);
                    return "";
                }
                return photo.FileId;

            }
            else if (msg.Type == MessageType.Video)
            {
                var video = msg.Video;
                if (video == null)
                {
                    _logger.LogWarning("Пусте відео.");
                    await botClient.SendMessage(chatId, "Не вдалося зберегти відео", cancellationToken: cancellationToken);
                    return "";
                }
                return video.FileId;
            }
            else
            {
                _logger.LogWarning("Повiдомлення не є медіа типу");
                await botClient.SendMessage(chatId, "Не вдалося зберегти медіа", cancellationToken: cancellationToken);
                return "";
            }
        }

        private async Task<IAlbumInputMedia?> GetMediaById(ITelegramBotClient botClient,
            string? mediaId,
            CancellationToken cancellationToken)
        {
            if (mediaId != null)
            {
                var file = await botClient.GetFile(mediaId);
                var filePath = file.FilePath;
                string extension = Path.GetExtension(filePath) ?? "";


                if (extension.EndsWith(".mp4"))
                    return new InputMediaVideo(mediaId);

                else if (extension.EndsWith(".jpg") || mediaId.EndsWith(".png"))
                    return new InputMediaPhoto(mediaId);

                else
                    _logger.LogWarning("Невідомий формат файлу");
            }
            return null;
        }

        public async Task<List<IAlbumInputMedia>> GetMediaGroup(ITelegramBotClient botClient,
            List<string?> mediaIds,
            CancellationToken cancellationToken)
        {
            var mediaGroup = new List<IAlbumInputMedia>();

            foreach (var media in mediaIds)
            {
                var mediaId = await GetMediaById(botClient, media, cancellationToken);
                if (mediaId != null)
                    mediaGroup.Add(mediaId);
            }

            return mediaGroup;
        }
    }
}
