using System.Net.Http.Json;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Flickoo.Telegram
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly MainKeyboard _mainKeyboard;
        private readonly MyProductKeyboard _myProductKeyboard;
        private readonly Dictionary<long, UserSession> _userSessions = new();
        private readonly Dictionary<long, ProductSession> _productSessions = new();

        public TelegramBotService(
            ITelegramBotClient botClient,
            HttpClient httpClient,
            ILogger<TelegramBotService> logger,
            IUserService userService,
            IProductService productService,
            IMediaService mediaService,
            MainKeyboard mainKeyboard,
            MyProductKeyboard myProductKeyboard)
        {
            _botClient = botClient;
            _httpClient = httpClient;
            _logger = logger;
            _userService = userService;
            _productService = productService;
            _mediaService = mediaService;
            _mainKeyboard = mainKeyboard;
            _myProductKeyboard = myProductKeyboard;
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
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(botClient, callbackQuery, cancellationToken),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            });
        }

        private async Task OnMessage(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken)
        {
            var chatId = msg.Chat.Id;
            var userName = msg.From?.Username ?? "Unknown";
            if (string.IsNullOrEmpty(msg.Text) && msg.MediaGroupId == null)
            {
                _logger.LogWarning("Повідомлення не може бути пустим.");
                await botClient.SendMessage(chatId, "Повідомлення не може бути пустим.", cancellationToken: cancellationToken);
                return;
            }
            _logger.LogInformation($"Отримано повiдомлення: {msg.Text} | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");

            if(await HandleBaseCommand(botClient, chatId, msg, cancellationToken))
                return;

            if (await UserSessionCheck(botClient, chatId, msg, cancellationToken))
                return;

            if(await ProductSessionCheck(botClient, chatId, msg, cancellationToken))
                return;

            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Вибери потрібну команду на панелі");


        }

        private async Task<bool> HandleBaseCommand(ITelegramBotClient botClient,
            long chatId,
            Message command,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                await botClient.SendMessage(chatId, "Пуста команда.", cancellationToken: cancellationToken);
                return false;
            }

            switch (command.Text.ToLower())
            {
                case "/start":
                    await botClient.SendMessage(chatId, "Привіт! Я Telegram-бот на C#.", cancellationToken: cancellationToken);
                    await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Вибери потрібну команду на панелі");
                    break;

                case "вихід":
                    await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Дію скасовано");
                    _userSessions.Remove(chatId);
                    _productSessions.Remove(chatId);
                    break;

                default:
                    return false;
            }
            return true;
        }
        private async Task<bool> UserSessionCheck(ITelegramBotClient botClient, long chatId, Message msg, CancellationToken cancellationToken)
        {
            if (!_userSessions.ContainsKey(chatId))
                _userSessions[chatId] = new UserSession();

            if (_userSessions[chatId].State != UserSessionState.Idle)
            {
                switch (_userSessions[chatId].State)
                {
                    case UserSessionState.CreateWaitingForUserName:
                        if (msg.Text == "вихід")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            return true;
                        }
                        _userSessions[chatId].UserName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.CreateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        break;

                    case UserSessionState.CreateWaitingForLocation:
                        if (msg.Text == "вихід")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Реєстрацію скасовано");
                            return true;
                        }
                        _userSessions[chatId].LocationName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.CreateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        break;

                    case UserSessionState.UpdateWaitingForUserName:
                        if (msg.Text == "вихід")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення скасовано");
                            return true;
                        }
                        _userSessions[chatId].UserName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.UpdateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        break;
                    case UserSessionState.UpdateWaitingForLocation:
                        if (msg.Text == "вихід")
                        {
                            _userSessions[chatId].State = UserSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення скасовано");
                            return true;
                        }
                        _userSessions[chatId].LocationName = msg.Text ?? "";
                        _userSessions[chatId].State = await _userService.UpdateAccount(botClient, chatId, _userSessions[chatId].UserName, _userSessions[chatId].LocationName, cancellationToken);
                        break;
                }
            }
            else
            {
                if(await HandleUserCommand(botClient, msg, chatId, _userSessions[chatId], cancellationToken))
                    return true;
            }
            return false;
        }

        private async Task<bool> HandleUserCommand(ITelegramBotClient botClient,
            Message command,
            long chatId,
            UserSession userSession,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                await botClient.SendMessage(chatId, "Пуста команда.", cancellationToken: cancellationToken);
                return false;
            }

            switch (command.Text.ToLower())
            {
                case "мій профіль":
                    await _userService.MyProfile(botClient, chatId, cancellationToken);
                    break;

                case "створити акаунт":
                    _userSessions[chatId].State = await _userService.CreateAccount(botClient,
                        chatId,
                        userSession.UserName ?? "",
                        userSession.LocationName,
                        cancellationToken);
                    break;
    
                case "оновити дані":
                    _userSessions[chatId].State = await _userService.UpdateAccount(botClient,
                        chatId,
                        userSession.UserName,
                        userSession.LocationName,
                        cancellationToken);
                    if (_userSessions[chatId].State == UserSessionState.Idle)
                        _userSessions.Remove(chatId);
                    break;

                default:
                    return false;
            }
            return true;

        }

        private async Task<bool> ProductSessionCheck(ITelegramBotClient botClient, long chatId, Message msg, CancellationToken cancellationToken)
        {
            if (!_productSessions.ContainsKey(chatId))
                _productSessions[chatId] = new ProductSession();

            if (_productSessions[chatId].State != ProductSessionState.Idle)
            {
                switch (_productSessions[chatId].State)
                {
                    case ProductSessionState.WaitingForCategory:
                        if (msg.Text == "вихід")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        break;

                    case ProductSessionState.WaitingForMedia:
                        if (msg.Text == "вихід")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        if (msg.Photo != null && msg.Photo.Length > 0 && msg.Photo.Length < 6)
                        {
                            _productSessions[chatId].MediaUrls = await SavePhoto(botClient, msg, chatId, cancellationToken);

                            _productSessions[chatId].State = await _productService.AddProduct(botClient,
                                chatId,
                                _productSessions[chatId].CategoryId,
                                _productSessions[chatId].MediaUrls,
                                _productSessions[chatId].ProductName,
                                _productSessions[chatId].Price,
                                _productSessions[chatId].ProductDescription,
                                cancellationToken);
                        }

                        break;

                    case ProductSessionState.WaitingForProductName:
                        if (msg.Text == "вихід")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            cancellationToken);
                        break;

                    case ProductSessionState.WaitingForPrice:
                        if (msg.Text == "вихід")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        _productSessions[chatId].ProductName = msg.Text ?? "";
                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            cancellationToken);
                        break;

                    case ProductSessionState.WaitingForDescription:
                        if (msg.Text == "вихід")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        if (!decimal.TryParse(msg.Text, out var price))
                        {
                            await botClient.SendMessage(chatId, "Ціна повинна бути числом", cancellationToken: cancellationToken);
                            return true;
                        }
                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            cancellationToken);
                        break;
                }
            }
            else
            {
                if (await HandleProductCommand(botClient, msg, chatId, _productSessions[chatId], cancellationToken))
                    return true;
            }

            return false;

        }

        private async Task<List<string?>> SavePhoto(ITelegramBotClient botClient, Message msg, long chatId, CancellationToken cancellationToken)
        {

            if (msg.Photo == null || msg.Photo.Length == 0)
            {
                _logger.LogWarning("Повідомлення не містить фото.");
                return [];
            }

            if (msg.Photo.Length > 5)
            {
                _logger.LogWarning("Не можна зберігати більше 4 фото.");
                return [];
            }

            var mediaUrls = new List<string?>();

            foreach (var photoSize in msg.Photo)
            {
                try
                {
                    var file = await botClient.GetFile(photoSize.FileId, cancellationToken);
                    var filePath = file.FilePath;

                    if (string.IsNullOrEmpty(filePath))
                    {
                        _logger.LogWarning("Не вдалося отримати шлях до файлу {FileId}", photoSize.FileId);
                        continue;
                    }

                    await using var fileStream = new MemoryStream();
                    await botClient.DownloadFile(filePath, fileStream, cancellationToken);
                    fileStream.Seek(0, SeekOrigin.Begin);

                    var isSaved = await _mediaService.SaveProductMediaFile(fileStream, $"{photoSize.FileId}.jpg", chatId);
                    if (!isSaved)
                    {
                        _logger.LogError("Не вдалося зберегти файл {FileId}", photoSize.FileId);
                    }
                    else
                    {
                        mediaUrls.Add(_mediaService.GetProductMediaFilePath(chatId, $"{photoSize.FileId}.jpg"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка при збереженні фото {FileId}", photoSize.FileId);
                }
            }
            return mediaUrls;
        }

        private async Task<bool> HandleProductCommand(ITelegramBotClient botClient,
            Message command,
            long chatId,
            ProductSession productSession,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                await botClient.SendMessage(chatId, "Пуста команда.", cancellationToken: cancellationToken);
                return false;
            }

            switch (command.Text.ToLower())
            {
                case "мої оголошення":
                    await _productService.GetProducts(botClient, chatId, cancellationToken);
                    await _myProductKeyboard.SendMyProductKeyboard(botClient, chatId, cancellationToken);
                    break;
                case "додати продукт":
                    _productSessions[chatId].State = await _productService.AddProduct(botClient,
                        chatId,
                        _productSessions[chatId].CategoryId,
                        _productSessions[chatId].MediaUrls,
                        _productSessions[chatId].ProductName,
                        _productSessions[chatId].Price,
                        _productSessions[chatId].ProductDescription,
                        cancellationToken);
                    break;

                default:
                    return false;
            }
            return true;
        }

        private async Task OnCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Message == null)
            {
                _logger.LogWarning("CallbackQuery.Message не може бути пустим.");
                return;
            }

            var chatId = callbackQuery.Message.Chat.Id;
            var userName = callbackQuery.From.Username ?? "Unknown";
            _logger.LogInformation($"Отримано callbackQuery: {callbackQuery.Data} | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");
            if (string.IsNullOrEmpty(callbackQuery.Data))
            {
                _logger.LogWarning("CallbackQuery не може бути пустим.");
                await botClient.SendMessage(chatId, "CallbackQuery не може бути пустим.", cancellationToken: cancellationToken);
                return;
            }
            else
            {
                switch (_productSessions[chatId].State)
                {
                    case ProductSessionState.WaitingForCategory:
                        _productSessions[chatId].CategoryId = int.Parse(callbackQuery.Data);
                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            cancellationToken);
                        break;

                    default:
                        break;
                }
                
            }
            return;
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