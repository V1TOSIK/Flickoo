using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.Interfaces;
using System.Net.Http.Json;
using Flickoo.Telegram.enums;
using Telegram.Bot;
using Flickoo.Telegram.SessionModels;

namespace Flickoo.Telegram.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IKeyboards _keyboards;
        public UserService(
            HttpClient httpClient,
            ILogger<TelegramBotService> logger,
            IKeyboards keyboards)
        {
            _httpClient = httpClient;
            _logger = logger;
            _keyboards = keyboards;
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
                        await _keyboards.SendMyProfileKeyboard(botClient,
                            chatId,
                            user,
                            $"Username: {user.Username}\n"
                            + $"Location: {user.LocationName}",
                            cancellationToken);
                    }
                }
                else
                {
                    _logger.LogWarning("Користувач не зареєстрований!");
                    await _keyboards.SendMyProfileRegKeyboard(botClient,
                        chatId,
                        "Ви ще не зареєстровані." +
                        "\nБажаєте створити акаунт?",
                        cancellationToken);
                }

            }
            else
            {
                _logger.LogError("Помилка при перевірці наявності користувача.");
                await botClient.SendMessage(chatId, "Помилка при перевірці наявності користувача.",
                    cancellationToken: cancellationToken);
            }

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
            CreateOrUpdateUserRequest createUserRequest;
            if (string.IsNullOrWhiteSpace(name))
            {
                createUserRequest = new CreateOrUpdateUserRequest
                {
                    Id = chatId,
                    LocationName = "",
                    Registered = false
                };
            }
            else
            {
                createUserRequest = new CreateOrUpdateUserRequest
                {
                    Id = chatId,
                    Username = name,
                    LocationName = "",
                    Registered = false
                };
            }

            var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/User", createUserRequest, cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                // не працює
                if (response.Content.ReadAsStringAsync(cancellationToken).Result == "userExist")
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
            UserSession session,
            CancellationToken cancellationToken)
        {
            if (!await UserCheck(botClient, chatId, session, "", cancellationToken))
            {
                return session.State;
            }

            var user = new CreateOrUpdateUserRequest
            {
                Id = chatId,
                Username = session.UserName,
                LocationName = session.LocationName,
                Registered = true
            };

            var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/User/register", user, cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Added user with Id:{chatId} | UserName: {null} | Location: {null}");
                await _keyboards.SendMainKeyboard(botClient, chatId, "Користувача успішно додано!", cancellationToken);
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
                _logger.LogError($"User add error: {response.StatusCode} - {errorDetails}");
                await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при додаванні користувача.", cancellationToken);
            }
            return UserSessionState.Idle;
        }

        public async Task<UserSessionState> UpdateAccount(ITelegramBotClient botClient,
            long chatId,
            UserSession session,
            CancellationToken cancellationToken)
        {
            if (!await UserCheck(botClient, chatId, session, "(new)", cancellationToken))
            {
                return session.State;
            }

            var newUser = new CreateOrUpdateUserRequest()
            {
                Id = chatId,
                Username = session.UserName,
                LocationName = session.LocationName
            };

            var response = await _httpClient.PutAsJsonAsync($"https://localhost:8443/api/User/{chatId}", newUser, cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Updated user with Id:{chatId} | UserName: {session.UserName} | Location: {session.LocationName}");
                await _keyboards.SendMainKeyboard(botClient, chatId, "Користувача успішно оновлено!", cancellationToken);
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
                _logger.LogError($"User update error: {response.StatusCode} - {errorDetails}");
                await _keyboards.SendMainKeyboard(botClient, chatId, "Не вдалося оновити користувача", cancellationToken);
            }

            return UserSessionState.Idle;
        }

        private async Task<bool> UserCheck(ITelegramBotClient botClient,
            long chatId,
            UserSession session,
            string checkTypeText,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(session.UserName))
            {
                await _keyboards.SendCancelKeyboard(botClient, chatId, $"Будь ласка, введіть ім'я користувача. {checkTypeText}", cancellationToken: cancellationToken);
                session.State = UserSessionState.WaitingForUserName;
                return false;
            }

            if (string.IsNullOrWhiteSpace(session.LocationName))
            {
                await _keyboards.SendCancelKeyboard(botClient, chatId, $"Будь ласка, введіть вашу локацію. {checkTypeText}", cancellationToken: cancellationToken);
                session.State = UserSessionState.WaitingForLocation;
                return false;
            }

            return true;
        }
    }
}
