using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
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
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly MainKeyboard _mainKeyboard;
        private readonly MyProductKeyboard _myProductKeyboard;
        private readonly CategoriesInlineKeyboard _categoriesInlineKeyboard;
        private readonly LikeInlineKeyboard _likeInlineKeyboard;
        private readonly Dictionary<long, UserSession> _userSessions = new();
        private readonly Dictionary<long, ProductSession> _productSessions = new();
        private Queue<GetProductResponse> _productsForSwaping = [];

        public TelegramBotService(
            ITelegramBotClient botClient,
            HttpClient httpClient,
            ILogger<TelegramBotService> logger,
            IUserService userService,
            IProductService productService,
            IMediaService mediaService,
            MainKeyboard mainKeyboard,
            MyProductKeyboard myProductKeyboard,
            CategoriesInlineKeyboard categoriesInlineKeyboard,
            LikeInlineKeyboard likeInlineKeyboard)
        {
            _botClient = botClient;
            _httpClient = httpClient;
            _logger = logger;
            _userService = userService;
            _productService = productService;
            _mediaService = mediaService;
            _mainKeyboard = mainKeyboard;
            _myProductKeyboard = myProductKeyboard;
            _categoriesInlineKeyboard = categoriesInlineKeyboard;
            _likeInlineKeyboard = likeInlineKeyboard;
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
            if (msg.Photo != null)
            {
                _logger.LogInformation($"Отримано фото | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");
            }
            else if (string.IsNullOrEmpty(msg.Text) && (msg.Type != MessageType.Photo || msg.Type != MessageType.Video))
            {
                _logger.LogWarning("Повідомлення не може бути пустим.");
                await botClient.SendMessage(chatId, "Повідомлення не може бути пустим.", cancellationToken: cancellationToken);
                return;
            }
            _logger.LogInformation($"Отримано повiдомлення: {msg.Text} | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");

            if (await HandleBaseCommand(botClient, chatId, msg, cancellationToken))
                return;

            if (await UserSessionCheck(botClient, chatId, msg, cancellationToken))
                return;
            _userSessions.Remove(chatId);

            if (await ProductSessionCheck(botClient, chatId, msg, cancellationToken))
                return;
            _productSessions.Remove(chatId);

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
                return false;
            }
            switch (command.Text.ToLower())
            {
                case "вподобане":
                    if (!_productSessions.ContainsKey(chatId))
                        _productSessions[chatId] = new ProductSession();
                    _productSessions[chatId].State = ProductSessionState.SwapingLikedProducts;
                    var likeKeyboard = _likeInlineKeyboard.SendLikeInlineButtonsAsync(botClient, chatId, cancellationToken: cancellationToken);
                    await botClient.SendMessage(chatId, "Оберіть спосіб сортування", cancellationToken: cancellationToken, replyMarkup: likeKeyboard);
                    return true;


                case "оголошення":
                    if (!_productSessions.ContainsKey(chatId))
                        _productSessions[chatId] = new ProductSession();
                    _productSessions[chatId].State = ProductSessionState.AwaitCategoryForSwaping;
                    var keyboard = await _categoriesInlineKeyboard.SendInlineButtonsAsync(_httpClient, botClient, chatId, cancellationToken);
                    await botClient.SendMessage(chatId, "Оберіть категорію", cancellationToken: cancellationToken, replyMarkup: keyboard);
                    return true;

                case "назад":
                    await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Дію скасовано");
                    _userSessions.Remove(chatId);
                    _productSessions.Remove(chatId);
                    return true;

                default:
                    return false;
            }
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

        private async Task<bool> HandleUserCommand(ITelegramBotClient botClient,
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

        private async Task<bool> ProductSessionCheck(ITelegramBotClient botClient, long chatId, Message msg, CancellationToken cancellationToken)
        {
            if (!_productSessions.ContainsKey(chatId))
                _productSessions[chatId] = new ProductSession();

            if (_productSessions[chatId].State != ProductSessionState.Idle)
            {
                switch (_productSessions[chatId].State)
                {
                    case ProductSessionState.WaitingForCategory:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        return true;

                    case ProductSessionState.WaitingForProductName:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        _productSessions[chatId].ProductName = msg.Text ?? "";
                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForPrice:
                        if (msg.Text == "назад")
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
                        _productSessions[chatId].Price = price;
                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForDescription:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        _productSessions[chatId].ProductDescription = msg.Text ?? "";
                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForMedia:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }

                        if (msg.Text == "надіслати фото заново")
                        {
                            _logger.LogInformation("Повторне надсилання фото/відео");
                            _productSessions[chatId].MediaUrls.Clear();
                            _productSessions[chatId].State = await _productService.AddProduct(botClient,
                                chatId,
                                _productSessions[chatId].CategoryId,
                                _productSessions[chatId].ProductName,
                                _productSessions[chatId].Price,
                                _productSessions[chatId].ProductDescription,
                                _productSessions[chatId].MediaUrls,
                                _productSessions[chatId].AddMoreMedia,
                                cancellationToken);
                            return true;
                        }

                        if (msg.Text == "готово")
                        {
                            _productSessions[chatId].AddMoreMedia = false;
                            _productSessions[chatId].State = await _productService.AddProduct(botClient,
                                chatId,
                                _productSessions[chatId].CategoryId,
                                _productSessions[chatId].ProductName,
                                _productSessions[chatId].Price,
                                _productSessions[chatId].ProductDescription,
                                _productSessions[chatId].MediaUrls,
                                _productSessions[chatId].AddMoreMedia,
                                cancellationToken);
                            _productSessions.Remove(chatId);
                            return true;
                        }

                        if (msg.Type != MessageType.Photo && msg.Type != MessageType.Video && string.IsNullOrEmpty(msg.Text))
                        {
                            _logger.LogWarning("Ви скинули не фото/відео");
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "ви скинули не фото/відео");
                            return true;
                        }

                        if (msg.Photo == null || _productSessions[chatId].MediaUrls.Count() >= 5)
                        {
                            _productSessions[chatId].MediaUrls.RemoveRange(5, _productSessions[chatId].MediaUrls.Count() - 5);
                            _productSessions[chatId].AddMoreMedia = false;
                        }

                        _productSessions[chatId].MediaUrls.Add(await SavePhoto(botClient, msg, chatId, cancellationToken));

                        _productSessions[chatId].State = await _productService.AddProduct(botClient,
                                chatId,
                                _productSessions[chatId].CategoryId,
                                _productSessions[chatId].ProductName,
                                _productSessions[chatId].Price,
                                _productSessions[chatId].ProductDescription,
                                _productSessions[chatId].MediaUrls,
                                _productSessions[chatId].AddMoreMedia,
                                cancellationToken);

                        return true;
                    /*Update*/

                    case ProductSessionState.WaitingForProductNameUpdate:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення продукту скасовано");
                            return true;
                        }
                        _productSessions[chatId].ProductName = msg.Text ?? "";
                        _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                            chatId,
                            _productSessions[chatId].ProductId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForPriceUpdate:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення продукту скасовано");
                            return true;
                        }
                        if (!decimal.TryParse(msg.Text, out var updatePrice))
                        {
                            await botClient.SendMessage(chatId, "Ціна повинна бути числом", cancellationToken: cancellationToken);
                            return true;
                        }
                        _productSessions[chatId].Price = updatePrice;
                        _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                            chatId,
                            _productSessions[chatId].ProductId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForDescriptionUpdate:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення продукту скасовано");
                            return true;
                        }
                        _productSessions[chatId].ProductDescription = msg.Text ?? "";
                        _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                            chatId,
                            _productSessions[chatId].ProductId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForMediaUpdate:
                        if (msg.Text == "назад")
                        {
                            _productSessions[chatId].State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Оновлення продукту скасовано");
                            return true;
                        }

                        if (msg.Text == "надіслати фото заново")
                        {
                            _logger.LogInformation("Повторне надсилання фото/відео");
                            _productSessions[chatId].MediaUrls.Clear();
                            _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                                chatId,
                                _productSessions[chatId].ProductId,
                                _productSessions[chatId].ProductName,
                                _productSessions[chatId].Price,
                                _productSessions[chatId].ProductDescription,
                                _productSessions[chatId].MediaUrls,
                                _productSessions[chatId].AddMoreMedia,
                                cancellationToken);
                            return true;
                        }

                        if (msg.Text == "готово")
                        {
                            _productSessions[chatId].AddMoreMedia = false;
                            _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                                chatId,
                                _productSessions[chatId].ProductId,
                                _productSessions[chatId].ProductName,
                                _productSessions[chatId].Price,
                                _productSessions[chatId].ProductDescription,
                                _productSessions[chatId].MediaUrls,
                                _productSessions[chatId].AddMoreMedia,
                                cancellationToken);
                            _productSessions.Remove(chatId);
                            return true;
                        }

                        if (msg.Type != MessageType.Photo && msg.Type != MessageType.Video && string.IsNullOrEmpty(msg.Text))
                        {
                            _logger.LogWarning("Ви скинули не фото/відео");
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "ви скинули не фото/відео");
                            return true;
                        }

                        if (msg.Photo == null || _productSessions[chatId].MediaUrls.Count() >= 5)
                        {
                            _productSessions[chatId].MediaUrls.RemoveRange(5, _productSessions[chatId].MediaUrls.Count() - 5);
                            _productSessions[chatId].AddMoreMedia = false;
                        }

                        _productSessions[chatId].MediaUrls.Add(await SavePhoto(botClient, msg, chatId, cancellationToken));

                        _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                                chatId,
                                _productSessions[chatId].ProductId,
                                _productSessions[chatId].ProductName,
                                _productSessions[chatId].Price,
                                _productSessions[chatId].ProductDescription,
                                _productSessions[chatId].MediaUrls,
                                _productSessions[chatId].AddMoreMedia,
                                cancellationToken);

                        return true;
                }
            }
            else
            {
                if (await HandleProductCommand(botClient, msg, chatId, _productSessions[chatId], cancellationToken))
                    return true;
            }
            return false;

        }

        private async Task<string> SavePhoto(ITelegramBotClient botClient, Message msg, long chatId, CancellationToken cancellationToken)
        {
            if (msg == null)
            {
                _logger.LogWarning("Пусте повідомлення.");
                return "";
            }

            if (msg.Photo == null && msg.Video == null)
            {
                _logger.LogWarning("Повідомлення не містить фото/відео.");
                await botClient.SendMessage(chatId, "Повідомлення не містить фото/відео.", cancellationToken: cancellationToken);
                return "";
            }

            if (msg.Photo == null)
                _logger.LogWarning("Повідомлення не містить фото.");

            else
                return msg.Photo.Last().FileId;
                
            if(msg.Video == null)
                _logger.LogWarning("Повідомлення не містить відео.");

            else
                return msg.Video.FileId;

            return "";
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
                        _productSessions[chatId].ProductName,
                        _productSessions[chatId].Price,
                        _productSessions[chatId].ProductDescription,
                        _productSessions[chatId].MediaUrls,
                        _productSessions[chatId].AddMoreMedia,
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

            var splitData = callbackQuery.Data.Split("_");

            if (splitData[0] == "like")
            {
                await _productService.LikeProduct(botClient,
                    chatId,
                    int.Parse(splitData[1]),
                    cancellationToken);
                await SendNextProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "first")
            {
                if(splitData[1] == "new")
                {
                    _productSessions[chatId].ProductsQueue = await _productService.GetLikedProducts(botClient, chatId, cancellationToken);
                    await SendNextLikedProduct(botClient, chatId, cancellationToken);
                }
                else if (splitData[1] == "old")
                {
                    _productSessions[chatId].ProductsQueue = await _productService.GetLikedProducts(botClient, chatId, cancellationToken);
                    await SendNextLikedProduct(botClient, chatId, cancellationToken);
                }
            }
            else if (splitData[0] == "write")
            {
                await botClient.SendMessage(chatId, "Вибачте, ця функція ще не реалізована", cancellationToken: cancellationToken);
            }
            else if (splitData[0] == "dislike")
            {
                await _productService.DislikeProduct(botClient,
                    chatId,
                    int.Parse(splitData[1]),
                    cancellationToken);
                await SendNextProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "update")
            {
                if (!_productSessions.ContainsKey(chatId))
                {
                    _productSessions[chatId] = new ProductSession();
                }
                _productSessions[chatId].ProductId = int.Parse(splitData[1]);
                _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                    chatId,
                    _productSessions[chatId].ProductId,
                    _productSessions[chatId].ProductName,
                    _productSessions[chatId].Price,
                    _productSessions[chatId].ProductDescription,
                    _productSessions[chatId].MediaUrls,
                    _productSessions[chatId].AddMoreMedia,
                    cancellationToken);
            }
            else if (splitData[0] == "delete")
            {
                await _productService.DeleteProduct(botClient, chatId, int.Parse(splitData[1]), cancellationToken);
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
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);
                        break;

                    case ProductSessionState.AwaitCategoryForSwaping:

                        if (!_productSessions.ContainsKey(chatId))
                            _productSessions[chatId] = new ProductSession();

                        _productSessions[chatId].ProductsQueue = await _productService.GetProductsForSwaping(botClient, chatId, int.Parse(callbackQuery.Data), cancellationToken);

                        await SendNextProduct(botClient, chatId, cancellationToken);
                        return;

                    case ProductSessionState.SwapingLikedProducts:
                        _productSessions[chatId].ProductsQueue = await _productService.GetLikedProducts(botClient, chatId, cancellationToken);
                        await SendNextLikedProduct(botClient, chatId, cancellationToken);
                        return;

                    default:
                        _productSessions[chatId].State = ProductSessionState.Idle;
                        return;
                }

            }
            return;
        }

        private async Task SendNextLikedProduct(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (!_productSessions.ContainsKey(chatId) || _productSessions[chatId].ProductsQueue.Count == 0)
            {
                await botClient.SendMessage(chatId, "Ви не вподобали жодного товару.", cancellationToken: cancellationToken);
                return;
            }
            var product = _productSessions[chatId].ProductsQueue.Dequeue();
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("напиcати продавцю", $"Write_{product}"),
                InlineKeyboardButton.WithCallbackData("👎 Дизлайк", $"dislike_{product.Id}")
            });
            var mediaList = new List<IAlbumInputMedia>();
            foreach (var media in product.MediaUrls)
            {
                if (media != null)
                    mediaList.Add(new InputMediaPhoto(media));
            }
            if (mediaList.Count > 0)
            {
                await botClient.SendMediaGroup(
                    chatId: chatId,
                    mediaList,
                    cancellationToken: cancellationToken
                );
                await botClient.SendMessage(chatId,
                $"Назва: {product.Name}\n" +
                $"Ціна: {product.Price}\n\n" +
                $"Опис: {product.Description}",
                cancellationToken: cancellationToken,
                replyMarkup: inlineKeyboard);
                return;
            }
            else
            {
                await botClient.SendMessage(chatId,
                $"Назва: {product.Name}\n" +
                $"Ціна: {product.Price}\n\n" +
                $"Опис: {product.Description}",
                cancellationToken: cancellationToken,
                replyMarkup: inlineKeyboard);
                return;
            }
        }

        private async Task SendNextProduct(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (!_productSessions.ContainsKey(chatId) || _productSessions[chatId].ProductsQueue.Count == 0)
            {
                await botClient.SendMessage(chatId, "Більше товарів немає.", cancellationToken: cancellationToken);
                return;
            }

            var product = _productSessions[chatId].ProductsQueue.Dequeue();

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("👍 Лайк", $"like_{product.Id}"),
                InlineKeyboardButton.WithCallbackData("👎 Дизлайк", $"dislike_{product.Id}")
            });

            var mediaList = new List<IAlbumInputMedia>();
            foreach (var media in product.MediaUrls)
            {
                if (media != null)
                    mediaList.Add(new InputMediaPhoto(media));
            }

            if (mediaList.Count > 0)
            {
                await botClient.SendMediaGroup(
                    chatId: chatId,
                    mediaList,
                    cancellationToken: cancellationToken
                );

                await botClient.SendMessage(chatId,
                $"Назва: {product.Name}\n" +
                $"Ціна: {product.Price}\n\n" +
                $"Опис: {product.Description}",
                cancellationToken: cancellationToken,
                replyMarkup: inlineKeyboard);
                return;
            }
            else
            {
                await botClient.SendMessage(chatId,
                $"Назва: {product.Name}\n" +
                $"Ціна: {product.Price}\n\n" +
                $"Опис: {product.Description}",
                cancellationToken: cancellationToken,
                replyMarkup: inlineKeyboard);
                return;

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