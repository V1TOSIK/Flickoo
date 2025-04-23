using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace Flickoo.Telegram.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserSessionService> _logger;
        private readonly IKeyboards _keyboards;
        private readonly Dictionary<long, UserSession> _userSessions = [];
        public UserSessionService(IUserService userService,
            ILogger<UserSessionService> logger,
            IKeyboards keyboards)
        {
            _userService = userService;
            _logger = logger;
            _keyboards = keyboards;
        }
        public async Task<bool> UserSessionCheck(ITelegramBotClient botClient, long chatId, string userName, Message msg, CancellationToken cancellationToken)
        {
            var session = GetUserSession(chatId);

            if (string.IsNullOrEmpty(session.Action))
                return await HandleUserCommand(botClient, msg, userName, chatId, cancellationToken);

            if (session.Action == "Create")
                return await RegisterUser(botClient, msg, chatId, cancellationToken);

            if (session.Action == "Update")
                return await UpdateUser(botClient, msg, chatId, cancellationToken);

            else
                return false;
        }

        public async Task<bool> HandleUserCommand(ITelegramBotClient botClient,
            Message command,
            string userName,
            long chatId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                return false;
            }

            var session = GetUserSession(chatId);

            switch (command.Text.ToLower())
            {
                case "👤":
                    await _userService.MyProfile(botClient, chatId, userName, cancellationToken);
                    return true;

                case "створити акаунт":
                    session.UserName = command.Chat.Username ?? "";
                    session.Action = "Create";
                    session.State = await _userService.CreateAccount(botClient,
                        chatId,
                        session,
                        cancellationToken);

                    if (_userSessions[chatId].State == UserSessionState.Idle)
                        ResetSession(chatId);

                    return true;

                case "оновити дані":
                    session.Action = "Update";
                    session.State = await _userService.UpdateAccount(botClient,
                        chatId,
                        session,
                        cancellationToken);
                    if (_userSessions[chatId].State == UserSessionState.Idle)
                        ResetSession(chatId);
                    return true;

                default:
                    return false;
            }

        }

        public UserSession GetUserSession(long chatId)
        {
            if (!_userSessions.TryGetValue(chatId, out var session))
            {
                session = new UserSession();
                _userSessions[chatId] = session;
            }
            return session;
        }

        public void ResetSession(long chatId)
        {
            if (_userSessions.ContainsKey(chatId))
            {
                var session = _userSessions[chatId];
                session.State = UserSessionState.Idle;
                session.UserName = string.Empty;
                session.NickName = string.Empty;
                session.LocationName = string.Empty;
                session.Action = string.Empty;

                _userSessions.Remove(chatId);
            }
        }

        public async Task<bool> RegisterUser(ITelegramBotClient botClient, Message msg, long chatId, CancellationToken cancellationToken)
        {
            var session = GetUserSession(chatId);

            switch (session.State)
            {
                case UserSessionState.WaitingForUserName:
                    if (msg.Text == "назад")
                    {
                        await CancelAction(botClient, chatId, "Реєстрацію скасовано", cancellationToken);
                        return true;
                    }
                    session.NickName = msg.Text ?? "";
                    session.State = await _userService.CreateAccount(botClient, chatId, session, cancellationToken);
                    return true;

                case UserSessionState.WaitingForLocation:
                    if (msg.Text == "назад")
                    {
                        await CancelAction(botClient, chatId, "Реєстрацію скасовано", cancellationToken);
                        return true;
                    }
                    session.LocationName = msg.Text ?? "";
                    session.State = await _userService.CreateAccount(botClient, chatId, session, cancellationToken);
                    ResetSession(chatId);
                    return true;
                default:
                    return false;
            }
        }

        public async Task<bool> UpdateUser(ITelegramBotClient botClient, Message msg, long chatId, CancellationToken cancellationToken)
        {
            var session = GetUserSession(chatId);

            switch (session.State)
            {
                case UserSessionState.WaitingForUserName:
                    if (msg.Text == "назад")
                    {
                        await CancelAction(botClient, chatId, "Реєстрацію скасовано", cancellationToken);
                        return true;
                    }
                    session.NickName = msg.Text ?? "";
                    session.State = await _userService.UpdateAccount(botClient, chatId, session, cancellationToken);
                    return true;

                case UserSessionState.WaitingForLocation:
                    if (msg.Text == "назад")
                    {
                        await CancelAction(botClient, chatId, "Реєстрацію скасовано", cancellationToken);
                        return true;
                    }
                    session.LocationName = msg.Text ?? "";
                    session.State = await _userService.UpdateAccount(botClient, chatId, session, cancellationToken);
                    ResetSession(chatId);
                    return true;

                default:
                    return false;
            }
        }

        private async Task CancelAction(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            await _keyboards.SendMainKeyboard(botClient, chatId, messageText, cancellationToken);
            ResetSession(chatId);
        }
    }
}

