using Telegram.Bot.Types;
using Telegram.Bot;
using Flickoo.Telegram.DTOs.Media;

namespace Flickoo.Telegram.Interfaces
{
    public interface IMediaService
    {
        string GetMediaTypeFromMsgAsync(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        Task<Stream> GetMediaFileFromMsgAsync(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        Task<List<IAlbumInputMedia>> GetMediaFromUrlsByProductIdAsync(ITelegramBotClient botClient,
            long productId,
            CancellationToken cancellationToken);

        Task<bool> UploadMediasAsync(ITelegramBotClient botClient,
            IEnumerable<MediaRequest> mediaRequests,
            long productId,
            CancellationToken cancellationToken);

        Task<bool> UpdateProductMediasAsync(ITelegramBotClient botClient,
            IEnumerable<MediaRequest> mediaRequest,
            long productId,
            CancellationToken cancellationToken);
    }
}
