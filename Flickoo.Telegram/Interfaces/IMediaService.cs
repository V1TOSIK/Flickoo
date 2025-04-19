using Telegram.Bot.Types;
using Telegram.Bot;
using Flickoo.Telegram.DTOs.Media;

namespace Flickoo.Telegram.Interfaces
{
    public interface IMediaService
    {
        string GetMediaTypeFromMsgAsync(Message msg,
            CancellationToken cancellationToken);

        Task<Stream> GetMediaFileFromMsgAsync(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        Task<List<IAlbumInputMedia>> GetMediaFromUrlsByProductIdAsync(ITelegramBotClient botClient,
            long productId,
            CancellationToken cancellationToken);

        Task<bool> UploadMediaAsync(ITelegramBotClient botClient,
            MediaRequest mediaRequest,
            long productId,
            CancellationToken cancellationToken);
    }
}
