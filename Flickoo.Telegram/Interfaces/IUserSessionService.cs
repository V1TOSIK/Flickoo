using Flickoo.Telegram.SessionModels;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Flickoo.Telegram.Interfaces
{
    public interface IUserSessionService
    {
        Task<bool> UserSessionCheck(ITelegramBotClient botClient,
            long chatId,
            Message msg,
            CancellationToken cancellationToken);

        Task<bool> HandleUserCommand(ITelegramBotClient botClient,
            Message command,
            long chatId,
            CancellationToken cancellationToken);

        Task<bool> RegisterUser(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        Task<bool> UpdateUser(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken);

        UserSession GetUserSession(long chatId);

        void ResetSession(long chatId);
    }
}
