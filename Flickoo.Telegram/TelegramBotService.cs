using System.Net.Http.Json;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IUserService _userService;
        private readonly Dictionary<long, UserSession> _userSessions = new();

        public TelegramBotService(
            ITelegramBotClient botClient,
            HttpClient httpClient,
            ILogger<TelegramBotService> logger,
            IUserService userService)
        {
            _userService = userService;
            _botClient = botClient;
            _httpClient = httpClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var me = await _botClient.GetMe(cancellationToken: cancellationToken);
            _logger.LogInformation($"Бот {me.FirstName} запущено!");

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                cancellationToken: cts.Token
            );

            await Task.Delay(Timeout.Infinite, cancellationToken);
            await cts.CancelAsync();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (update switch
            {
                { Message: { } message } => OnMessage(botClient, message, cancellationToken),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            });
        }

        private async Task OnMessage(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken)
        {
            var chatId = msg.Chat.Id;
            var userName = msg.From?.Username ?? "Unknown";
            if (string.IsNullOrEmpty(msg.Text))
            {
                _logger.LogWarning("Повідомлення не може бути пустим.");
                await botClient.SendMessage(chatId, "Повідомлення не може бути пустим.", cancellationToken: cancellationToken);
                return;
            }
            _logger.LogInformation($"Отримано повiдомлення: {msg.Text} | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");

            if (!_userSessions.ContainsKey(chatId))
                _userSessions[chatId] = new UserSession();


            if (_userSessions[chatId].State != UserSessionState.Idle)
            {
                switch (_userSessions[chatId].State)
                {
                    case UserSessionState.WaitingForUserName:
                        if(msg.Text == "/exit")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            return;
                        }
                        _userSessions[chatId].UserName = msg.Text;
                        _userSessions[chatId].State = await _userService.CreateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        break;
                    case UserSessionState.WaitingForLocation:
                        if (msg.Text == "/exit")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            return;
                        }
                        _userSessions[chatId].LocationName = msg.Text;
                        _userSessions[chatId].State = await _userService.CreateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        break;
                }
            }
            else
                await HandleCommand(botClient, msg, chatId, _userSessions[chatId], cancellationToken);

        }
        

        private async Task HandleCommand(ITelegramBotClient botClient, Message command, long chatId, UserSession session, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                await botClient.SendMessage(chatId, "Пуста команда.", cancellationToken: cancellationToken);
                return;
            }

            switch (command.Text.ToLower())
            {
                case "/start":
                    await botClient.SendMessage(chatId, "Привіт! Я Telegram-бот на C#.", cancellationToken: cancellationToken);
                    await SendMainKeyboard(botClient, chatId, "Вибери потрібну команду на панелі");
                    break;

                case "/myprofile":
                    await _userService.MyProfile(botClient, chatId, cancellationToken);
                    break;

                case "/createaccount":
                    _userSessions[chatId].State = await _userService.CreateAccount(botClient,
                        chatId,
                        session.UserName,
                        session.LocationName,
                        cancellationToken);
                    break;

                case "/exit":
                    await SendMainKeyboard(botClient, chatId, "Дію скасовано");
                    _userSessions[chatId].State = UserSessionState.Idle;
                    _userSessions[chatId].UserName = string.Empty;
                    _userSessions[chatId].LocationName = string.Empty;
                    break;

                default:
                    await SendMainKeyboard(botClient, chatId, "невідома команда");
                    break;
            }

        }

        private async Task SendMainKeyboard(ITelegramBotClient botClient, long chatId, string text)
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
            await botClient.SendMessage(chatId, text, replyMarkup: keyboard);
        }

        
        private async Task AddProduct(ITelegramBotClient botClient, Message msg)
        {
            if (msg.Text?.ToLower() == "/add product")
            {
                var category = 1; // замініть на реальний ID категорії
                                  // Відправка POST-запиту для додавання продукту
                var product = new
                {
                    Name = "Motorcycle",
                    Price = 15000.0m,
                    Description = "A great motorcycle",
                    UserId = msg.Chat.Id,
                    CategoryId = category
                };

                var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/Product", product);

                if (response.IsSuccessStatusCode)
                {
                    await botClient.SendMessage(msg.Chat.Id, "Продукт успішно додано!");
                    _logger.LogInformation("Продукт успішно додано!");
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError("{ErrorDetails}", errorDetails);
                    await botClient.SendMessage(msg.Chat.Id, "Помилка при додаванні продукту.");
                }
            }
        }

        private async Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            _logger.LogInformation($"Unknown update type: {update.Type}");
            if (update.Message != null)
                await botClient.SendMessage(update.Message.Chat.Id, "Unknown update type");
        }

        private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            _logger.LogInformation("HandleError: {Exception}", exception);
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}