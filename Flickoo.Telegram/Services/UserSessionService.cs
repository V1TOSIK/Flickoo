using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace Flickoo.Telegram.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserService _userService;
        private readonly MainKeyboard _mainKeyboard;
        private readonly ILogger<UserSessionService> _logger; // deleted ILogger<UserSessionService> logger when it was not used in 91 row, text null check
        private readonly Dictionary<long, UserSession> _userSessions = new();
        public UserSessionService(IUserService userService,
            ILogger<UserSessionService> logger,
            MainKeyboard mainKeyboard)
        {
            _userService = userService;
            _logger = logger;
            _mainKeyboard = mainKeyboard;
        }
        public async Task<bool> UserSessionCheck(ITelegramBotClient botClient, long chatId, Message msg, CancellationToken cancellationToken)
        {
            if (!_userSessions.ContainsKey(chatId))
                _userSessions[chatId] = new UserSession();

            if (_userSessions[chatId].State != UserSessionState.Idle)
            {
                switch (_userSessions[chatId].State)
                {
                    case UserSessionState.CreateWaitingForUserName:
                        if (msg.Text == "назад")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            return true;
                        }
                        _userSessions[chatId].UserName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.CreateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        return true;

                    case UserSessionState.CreateWaitingForLocation:
                        if (msg.Text == "назад")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            return true;
                        }
                        _userSessions[chatId].LocationName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.CreateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        return true;

                    case UserSessionState.UpdateWaitingForUserName:
                        if (msg.Text == "назад")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення скасовано");
                            return true;
                        }
                        _userSessions[chatId].UserName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.UpdateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        return true;
                    case UserSessionState.UpdateWaitingForLocation:
                        if (msg.Text == "назад")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення скасовано");
                            return true;
                        }
                        _userSessions[chatId].LocationName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.UpdateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        return true;
                }
            }
            else
                return await HandleUserCommand(botClient, msg, chatId, _userSessions[chatId], cancellationToken);

            return false;
        }

        public async Task<bool> HandleUserCommand(ITelegramBotClient botClient,
            Message command,
            long chatId,
            UserSession userSession,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                return false;
            }
            switch (command.Text.ToLower())
            {
                case "мій профіль":
                    await _userService.MyProfile(botClient, chatId, cancellationToken);
                    return true;

                case "створити акаунт":
                    _userSessions[chatId].State = await _userService.CreateAccount(botClient,
                        chatId,
                        userSession.UserName ?? "",
                    userSession.LocationName,
                        cancellationToken);

                    if (_userSessions[chatId].State == UserSessionState.Idle)
                        _userSessions.Remove(chatId);

                    return true;

                case "оновити дані":
                    _userSessions[chatId].State = await _userService.UpdateAccount(botClient,
                        chatId,
                        userSession.UserName,
                        userSession.LocationName,
                        cancellationToken);
                    if (_userSessions[chatId].State == UserSessionState.Idle)
                        _userSessions.Remove(chatId);
                    return true;

                default:
                    return false;
            }

        }
    }
}

