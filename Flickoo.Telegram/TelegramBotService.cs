using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net.Http.Json;
using Telegram.Bot.Types.ReplyMarkups;
using Flickoo.Telegram.DTOs;

namespace FlickooBot
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;

        public TelegramBotService(
            ITelegramBotClient botClient,
            HttpClient httpClient,
            ILogger<TelegramBotService> logger)
        {
            _botClient = botClient;
            _httpClient = httpClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var me = await _botClient.GetMe();
            _logger.LogInformation($"Бот {me.FirstName} запущено!");

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                cancellationToken: cts.Token
            );

            Console.ReadLine();
            cts.Cancel();

            await Task.Delay(1000, cancellationToken);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (update switch
            {
                { Message: { } message } => OnMessage(botClient, message, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update)
            });
        }

        private async Task OnMessage(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken)
        {
            var chatId = msg.Chat.Id;
            var userId = msg.From?.Id ?? 0;
            var userName = msg.From?.Username ?? "Unknown";
            string phoneNumber = string.Empty;
            _logger.LogInformation($"Отримано повiдомлення: {msg.Text} | ChatId: {chatId} | UserName: {userName} | UserId: {userId} | Time: {DateTime.UtcNow}");

            if (msg.Contact != null)
            {
                phoneNumber = msg.Contact.PhoneNumber;
                _logger.LogInformation($"Отримано номер телефону: {phoneNumber}");/*
                await botClient.SendMessage(chatId, $"Ваш номер телефону: {phoneNumber} успішно отримано.");*/
                return;

            }
                if (msg.Text == null)
            {
                _logger.LogWarning("Повідомлення не може бути пустим.");
                await botClient.SendMessage(chatId, "Повідомлення не може бути пустим.");
                return;
            }

            switch (msg.Text.ToLower())
            {
                case "/start":
                    await botClient.SendMessage(chatId,"Привiт! Я Telegram-бот на C#.");
                    await SendMainKeyboard(_botClient, chatId);
                    break;

                case"/myprofile":
                    await MyProfile(botClient, chatId, cancellationToken);
                    break;

                case "/createaccount":
                    await CreateAccount(msg, chatId, phoneNumber, cancellationToken);
                    break;

                case "/cancel":
                    await SendMainKeyboard(botClient, chatId);
                    break;

                default:
                    await botClient.SendMessage(chatId, "Не вдалося розпiзнати команду.");
                    await SendMainKeyboard(botClient, chatId);
                    break;
            }
        }

        private async Task MyProfile(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if(chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return;
            }

            var response = await _httpClient.GetAsync($"https://localhost:8443/api/User/check/{chatId}");

            if (response.IsSuccessStatusCode)
            {
                var userExists = await response.Content.ReadFromJsonAsync<bool>();

                if (userExists)
                {
                    var userResponse = await _httpClient.GetAsync($"https://localhost:8443/api/User/{chatId}");
                    var user = await userResponse.Content.ReadFromJsonAsync<GetUserResponse>();
                    if (user != null)
                    {
                        await botClient.SendMessage(chatId,
                            "Користувача знайдено!\n" +
                            $"Username: {user.Username}\n" +
                            $"PhoneNumber: {user.PhoneNumber}");
                        _logger.LogInformation("Користувача знайдено!");
                    }
                }
                else
                {
                    _logger.LogWarning("Користувач не зареєстрований!");
                    await SendRegKeyboard(botClient, chatId, cancellationToken);
                }

            }
            else
            {
                _logger.LogError("Помилка при перевiрцi наявностi користувача.");
                await botClient.SendMessage(chatId, "Помилка при перевірці наявності користувача.");
            }

        }

        private async Task SendRegKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return;
            }
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                    new KeyboardButton("/createAccount"),
                    new KeyboardButton("/cancel")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await _botClient.SendMessage(chatId, "Ви ще не зареєстровані.\nБажаєте створити акаунт?", replyMarkup: keyboard);
        }
        private async Task SendMainKeyboard(ITelegramBotClient botClient, long chatId)
        {
            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return;
            }

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                    new KeyboardButton("/myprofile"),
                    new KeyboardButton("/myproducts"),
                    new KeyboardButton("/mylikes"),
                    new KeyboardButton("/categories")

            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await _botClient.SendMessage(chatId, "Виберiть дiю:", replyMarkup: keyboard);
        }

        private async Task CreateAccount(Message msg, long chatId, string phoneNumber, CancellationToken cancellationToken)
        {
            var id = chatId;
            var userName = msg.Chat.Username;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Необхідно вказати iм'я користувача.");
                await _botClient.SendMessage(msg.Chat.Id, "Необхiдно вказати iм'я користувача");
                return;
            }
            if (string.IsNullOrEmpty(phoneNumber))
            {
                _logger.LogWarning("Необхідно вказати iм'я номер телефону");
                await _botClient.SendMessage(msg.Chat.Id, "Необхiдно вказати номер телефону");
                return;
            }

            var user = new CreateUserRequest
            {
                Id = id,
                Username = userName,
                PhoneNumber = phoneNumber
            };

            var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/User", user);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Added user with Id:{id} | UserName: {userName} | PhoneNumber: {phoneNumber}");
                await _botClient.SendMessage(msg.Chat.Id, "Користувача успiшно додано!");
                await SendMainKeyboard(_botClient, msg.Chat.Id);
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                _logger.LogError($"User add error: {response.StatusCode} - {errorDetails}");
                await _botClient.SendMessage(msg.Chat.Id, "Помилка при додаваннi користувача.");
            }
        }

        private async Task AddProduct(Message msg)
        {
            if (msg.Text.ToLower() == "/addproduct")
            {
                var category = 1; // замініть на реальний ID категорії
                                  // Відправка POST-запиту для додавання продукту
                var product = new
                {
                    Name = "Motorcycle",
                    Price = 15000.0m,
                    Description = "A great motorcycle",
                    UserId = msg.From.Id,
                    CategoryId = category // замініть на реальний ID категорії
                };

                var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/Product", product);

                if (response.IsSuccessStatusCode)
                {
                    await _botClient.SendMessage(msg.Chat.Id, "Продукт успiшно додано!");
                    _logger.LogInformation("Продукт успiшно додано!");
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Помилка при додаваннi продукту.", errorDetails);
                    await _botClient.SendMessage(msg.Chat.Id, "Помилка при додаваннi продукту.");
                }
            }
        }

        private async Task UnknownUpdateHandlerAsync(Update update)
        {
            _logger.LogInformation($"Unknown update type: {update.Type}");
            await _botClient.SendMessage(update.Message.Chat.Id, "Unknown update type");
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Помилка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}