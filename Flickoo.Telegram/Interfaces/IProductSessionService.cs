using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types;
using Telegram.Bot;
using Flickoo.Telegram.enums;

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
            ProductSession productSession,
            CancellationToken cancellationToken);

        Task<bool> UpdateProduct(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            long productId,
            ProductSessionState productSessionState,
            CancellationToken cancellationToken);
    }
}
