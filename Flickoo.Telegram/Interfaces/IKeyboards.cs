using Flickoo.Telegram.DTOs.User;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Flickoo.Telegram.Interfaces
{
    public interface IKeyboards
    {
        Task SendMainKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken);
        Task SendAddProductKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken);
        Task SendReductProductButtons(ITelegramBotClient botClient, long chatId, long productId, string messageText, CancellationToken cancellationToken);
        Task SendLikeFilterButtons(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken);
        Task SendMediaKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken);
        Task SendCategoriesInlineButtons(ITelegramBotClient botClient, long chatId, string messageText, bool withAllCategoryButton, CancellationToken cancellationToken);
        Task SendMyProfileKeyboard(ITelegramBotClient botClient, long chatId, GetUserResponse user, string messageText, CancellationToken cancellationToken);
        Task SendMyProfileRegKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken);
        Task SendCancelKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken);
        Task SendCurrencyKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken);
    }
}
