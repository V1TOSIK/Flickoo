﻿using Flickoo.Telegram.enums;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot;

namespace Flickoo.Telegram.Interfaces
{
    public interface IUserService
    {
        Task MyProfile(ITelegramBotClient botClient,
            long chatId,
            string userName,
            CancellationToken cancellationToken);

        Task<UserSessionState> CreateAccount(ITelegramBotClient botClient,
            long chatId,
            UserSession session,
            CancellationToken cancellationToken);

        Task<UserSessionState> UpdateAccount(ITelegramBotClient botClient,
            long chatId,
            UserSession session,
            CancellationToken cancellationToken);

        Task<bool> AddUnRegisteredUser(ITelegramBotClient botClient,
            long chatId,
            string name,
            CancellationToken cancellationToken);
    }
}
