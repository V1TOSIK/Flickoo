using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types;
using Telegram.Bot;
using Flickoo.Telegram.DTOs;

namespace Flickoo.Telegram.Interfaces
{
    public interface IProductSessionService
    {
        Task<bool> ProductSessionCheck(ITelegramBotClient botClient,
            long chatId,
            Message msg,
            CancellationToken cancellationToken);

        Task<bool> HandleProductCommand(ITelegramBotClient botClient,
            Message command,
            long chatId,
            CancellationToken cancellationToken);
        Task<bool> AddProduct(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        Task<bool> UpdateProduct(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        Task SendNextLikedProduct(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken);

        Task SendNextProduct(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken);

        ProductSession GetProductSession(long chatId);

        void ResetSession(long chatId);


    }
}
