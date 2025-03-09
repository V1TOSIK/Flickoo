using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.Interfaces;
using System.Net.Http.Json;
using Flickoo.Telegram.enums;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        public UserService(
            HttpClient httpClient,
            ILogger<TelegramBotService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task MyProfile(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return;
            }

            var response = await _httpClient.GetAsync($"https://localhost:8443/api/User/check/{chatId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var userExists = await response.Content.ReadFromJsonAsync<bool>(cancellationToken: cancellationToken);

                if (userExists)
                {
                    var userResponse = await _httpClient.GetAsync($"https://localhost:8443/api/User/{chatId}", cancellationToken);
                    var user = await userResponse.Content.ReadFromJsonAsync<GetUserResponse>(cancellationToken: cancellationToken);
                    if (user != null)
                    {
                        _logger.LogInformation("Користувача знайдено!");
                        await SendMyProfileKeyboard(botClient, chatId, user, cancellationToken);
                    }
                }
                else
                {
                    _logger.LogWarning("Користувач не зареєстрований!");
                    await SendMyProfileRegKeyboard(botClient, chatId, cancellationToken);
                }

            }
            else
            {
                _logger.LogError("Помилка при перевірці наявності користувача.");
                await botClient.SendMessage(chatId, "Помилка при перевірці наявності користувача.",
                    cancellationToken: cancellationToken);
            }

        }

        private async Task SendMyProfileKeyboard(ITelegramBotClient botClient, long chatId, GetUserResponse user, CancellationToken cancellationToken)
        {
            var profileKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("/updateaccount"),
                new KeyboardButton("/exit")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId,
                $"Username: {user.Username}\n"
                +$"Location: {user.LocationName}",
                replyMarkup: profileKeyboard,
                cancellationToken: cancellationToken);
        }
        public async Task SendMyProfileRegKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var registrationKeyboard = new ReplyKeyboardMarkup(new[]
            {
                    new KeyboardButton("/createaccount"),
                    new KeyboardButton("/exit")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, 
                "Ви ще не зареєстровані." +
                "\nБажаєте створити акаунт?",
                replyMarkup: registrationKeyboard,
                cancellationToken: cancellationToken);
        }

        public async Task<UserSessionState> CreateAccount(ITelegramBotClient botClient, long chatId, string? userName, string? locationName, CancellationToken cancellationToken)
        {
            var id = chatId;

            if (string.IsNullOrWhiteSpace(userName))
            {
                await botClient.SendMessage(chatId, "Будь ласка, введіть ім'я користувача.", cancellationToken: cancellationToken);
                return UserSessionState.WaitingForUserName;
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                await botClient.SendMessage(chatId, "Будь ласка, введіть вашу локацію.", cancellationToken: cancellationToken);
                return UserSessionState.WaitingForLocation;
            }

            var user = new CreateUserRequest
            {
                Id = id,
                Username = userName,
                LocationName = locationName
            };

            var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/User", user, cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Added user with Id:{id} | UserName: {null} | Location: {null}");
                await botClient.SendMessage(chatId, "Користувача успішно додано!", cancellationToken: cancellationToken);
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
                _logger.LogError($"User add error: {response.StatusCode} - {errorDetails}");
                await botClient.SendMessage(chatId, "Помилка при додаванні користувача.", cancellationToken: cancellationToken);
            }
            return UserSessionState.Idle;
        }


    }
}
