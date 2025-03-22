using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.Interfaces;
using System.Net.Http.Json;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly MainKeyboard _mainKeyboard;
        public UserService(
            HttpClient httpClient,
            ILogger<TelegramBotService> logger,
            MainKeyboard mainKeyboard)
        {
            _httpClient = httpClient;
            _logger = logger;
            _mainKeyboard = mainKeyboard;
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
                new KeyboardButton("оновити дані"),
                new KeyboardButton("назад")
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
        private async Task SendMyProfileRegKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var registrationKeyboard = new ReplyKeyboardMarkup(new[]
            {
                    new KeyboardButton("створити акаунт"),
                    new KeyboardButton("назад")
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


        public async Task<bool> AddUnRegisteredUser(ITelegramBotClient botClient,
            long chatId,
            string name,
            CancellationToken cancellationToken)
        {

            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return false;
            }
            var user = new CreateOrUpdateUserRequest();
            if (string.IsNullOrWhiteSpace(name))
            {
                user = new CreateOrUpdateUserRequest
                {
                    Id = chatId,
                    LocationName = "",
                    Registered = false
                };
            }
            else
            {
                user = new CreateOrUpdateUserRequest
                {
                    Id = chatId,
                    Username = name,
                    LocationName = "",
                    Registered = false
                };
            }

            var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/User", user, cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                // не працює
                if (response.Content.ReadAsStringAsync().Result == "userExist")
                {
                    _logger.LogInformation($"User with Id:{chatId} already exists");
                    return true;
                }
                _logger.LogInformation($"Added unregistered user with Id:{chatId} | UserName: {name} | Location: {null}");
                return true;
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
                _logger.LogError($"User add error: {response.StatusCode} - {errorDetails}");
                return false;
            }
        }

        public async Task<UserSessionState> CreateAccount(ITelegramBotClient botClient,
            long chatId,
            string? userName,
            string? locationName,
            CancellationToken cancellationToken)
        {
            var id = chatId;

            if (string.IsNullOrWhiteSpace(userName))
            {
                await botClient.SendMessage(chatId, "Будь ласка, введіть ім'я користувача.", cancellationToken: cancellationToken);
                return UserSessionState.CreateWaitingForUserName;
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                await botClient.SendMessage(chatId, "Будь ласка, введіть вашу локацію.", cancellationToken: cancellationToken);
                return UserSessionState.CreateWaitingForLocation;
            }

            var user = new CreateOrUpdateUserRequest
            {
                Id = id,
                Username = userName,
                LocationName = locationName,
                Registered = true
            };

            var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/User", user, cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Added user with Id:{id} | UserName: {null} | Location: {null}");
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Користувача успішно додано!");
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
                _logger.LogError($"User add error: {response.StatusCode} - {errorDetails}");
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Помилка при додаванні користувача.");
            }
            return UserSessionState.Idle;
        }



        public async Task<UserSessionState> UpdateAccount(ITelegramBotClient botClient,
            long chatId,
            string? userName,
            string? locationName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                await botClient.SendMessage(chatId, "Введіть нове ім'я, або ж залиште попереднє", cancellationToken: cancellationToken);
                return UserSessionState.UpdateWaitingForUserName;
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                await botClient.SendMessage(chatId, "Введіть нове місце розташування, або ж залиште попереднє", cancellationToken: cancellationToken);
                return UserSessionState.UpdateWaitingForLocation;
            }
            
            var newUser = new CreateOrUpdateUserRequest()
            {
                Id = chatId,
                Username = userName,
                LocationName = locationName
            };

            var response = await _httpClient.PutAsJsonAsync($"https://localhost:8443/api/User/{chatId}", newUser, cancellationToken: cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Updated user with Id:{chatId} | UserName: {userName} | Location: {locationName}");
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Користувача успішно оновлено!");
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
                _logger.LogError($"User update error: {response.StatusCode} - {errorDetails}");
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Не вдалося оновити користувача");
            }
            
            return UserSessionState.Idle;
        }

    }
}
