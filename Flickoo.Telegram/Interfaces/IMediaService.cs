using Telegram.Bot.Types;
using Telegram.Bot;

namespace Flickoo.Telegram.Interfaces
{
    public interface IMediaService
    {
        Task<string> GetMediaIdFromMsg(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        Task<List<IAlbumInputMedia>> GetMediaGroup(ITelegramBotClient botClient,
            List<string?> mediaIds,
            CancellationToken cancellationToken);
    }
}
