using Flickoo.Telegram.Interfaces;
using System.Net.Http.Json;
using Flickoo.Telegram.enums;
using Telegram.Bot;
using Flickoo.Telegram.SessionModels;
using Flickoo.Telegram.DTOs.User;
using Microsoft.Extensions.Options;

namespace Flickoo.Telegram.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IKeyboards _keyboards;
        private readonly string _apiUrl;
        public UserService(
            HttpClient httpClient,
            ILogger<TelegramBotService> logger,
            IKeyboards keyboards,
            IOptions<ApiOptions> apiOptions)
        {
            _httpClient = httpClient;
            _logger = logger;
            _keyboards = keyboards;
            _apiUrl = apiOptions.Value.Url;
        }

        public async Task MyProfile(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return;
            }

            var response = await _httpClient.GetFromJsonAsync<GetUserResponse>($"{_apiUrl}/api/User/{chatId}", cancellationToken);

            if (response != null)
            {
                if (response.Registered)
                {
                _logger.LogInformation("Користувача знайдено!");
                await _keyboards.SendMyProfileKeyboard(botClient,
                    chatId,
                    response,
                    $"NickName: {response.Nickname}\n"
                    + $"Location: {response.LocationName}",
                    cancellationToken);
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
                _logger.LogWarning("Користувача не знайдено!");
                await _keyboards.SendMainKeyboard(botClient,
                    chatId,
                    "Помилка при пошуку акаунта.\n",
                    cancellationToken);
            }
        }


        public async Task<bool> AddUnRegisteredUser(ITelegramBotClient botClient,
            long chatId,
            string username,
            CancellationToken cancellationToken)
        {

            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return false;
            }
            CreateUserRequest createUserRequest;
            if (string.IsNullOrWhiteSpace(username))
            {
                createUserRequest = new CreateUserRequest
                {
                    Id = chatId,
                    Username = "",
                    NickName = "",
                    LocationName = "",
                    Registered = false
                };
            }
            else
            {
                createUserRequest = new CreateUserRequest
                {
                    Id = chatId,
                    Username = username,
                    LocationName = "",
                    Registered = false
                };
            }

            var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/api/User", createUserRequest, cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Added unregistered user with Id:{chatId} | UserName: {username} | Location: {null}");
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

            var user = new CreateUserRequest
            {
                Id = chatId,
                Username = session.UserName,
                NickName = session.NickName,
                LocationName = session.LocationName,
                Registered = true
            };

            var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/api/User/registration", user, cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Added user with Id:{chatId} | UserName: {session.UserName} | Location: {session.LocationName}");
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

            var newUser = new UpdateUserRequest()
            {
                Id = chatId,
                NickName = session.NickName,
                LocationName = session.LocationName
            };

            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/api/User/{chatId}", newUser, cancellationToken: cancellationToken);

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
            if (string.IsNullOrWhiteSpace(session.NickName))
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
