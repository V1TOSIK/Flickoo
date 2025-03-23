using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Threading;

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
            var session = GetUserSession(chatId);

            if (session.State != UserSessionState.Idle)
            {
                switch (session.State)
                {
                    case UserSessionState.CreateWaitingForUserName:
                        if (msg.Text == "назад")
                        {
                            session.State = UserSessionState.Idle;
                            SetUserSession(chatId, session);
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            ResetSession(chatId);
                            return true;
                        }
                        session.UserName = msg.Text ?? "";
                        session.State = await _userService.CreateAccount(botClient, chatId, session.UserName, session.LocationName, cancellationToken);
                        SetUserSession(chatId, session);
                        return true;

                    case UserSessionState.CreateWaitingForLocation:
                        if (msg.Text == "назад")
                        {
                            session.State = UserSessionState.Idle;
                            SetUserSession(chatId, session);
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            ResetSession(chatId);
                            return true;
                        }
                        session.LocationName = msg.Text ?? "";
                        session.State = await _userService.CreateAccount(botClient, chatId, session.UserName, session.LocationName, cancellationToken);
                        ResetSession(chatId);
                        SetUserSession(chatId, session);
                        return true;

                    case UserSessionState.UpdateWaitingForUserName:
                        if (msg.Text == "назад")
                        {
                            session.State = UserSessionState.Idle;
                            SetUserSession(chatId, session);
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення скасовано");
                            ResetSession(chatId);
                            return true;
                        }
                        session.UserName = msg.Text ?? "";
                        session.State = await _userService.UpdateAccount(botClient, chatId, session.UserName, session.LocationName, cancellationToken);
                        SetUserSession(chatId, session);
                        return true;
                    case UserSessionState.UpdateWaitingForLocation:
                        if (msg.Text == "назад")
                        {
                            session.State = UserSessionState.Idle;
                            SetUserSession(chatId, session);
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення скасовано");
                            ResetSession(chatId);
                            return true;
                        }
                        session.LocationName = msg.Text ?? "";
                        session.State = await _userService.UpdateAccount(botClient, chatId, session.UserName, session.LocationName, cancellationToken);
                        ResetSession(chatId);
                        SetUserSession(chatId, session);
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
                        ResetSession(chatId);

                    return true;

                case "оновити дані":
                    _userSessions[chatId].State = await _userService.UpdateAccount(botClient,
                        chatId,
                        userSession.UserName,
                        userSession.LocationName,
                        cancellationToken);
                    if (_userSessions[chatId].State == UserSessionState.Idle)
                        ResetSession(chatId);
                    return true;

                default:
                    return false;
            }

        }
        public async Task<bool> CheckUserExist(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

        public UserSession SetUserSession(long chatId, UserSession userSession)
        {
            if (_userSessions.ContainsKey(chatId))
                _userSessions[chatId] = userSession;
            else
                _userSessions.Add(chatId, userSession);
            return userSession;
        }

        public void ResetSession(long chatId)
        {
            if (_userSessions.ContainsKey(chatId))
                _userSessions.Remove(chatId);
        }
    }
}

