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
        private readonly IFavouriteService _favouriteService;
        private readonly IUserSessionService _userSessionService;
        private readonly IProductSessionService _productSessionService;
        private readonly MainKeyboard _mainKeyboard;
        private readonly MyProductKeyboard _myProductKeyboard;
        private readonly CategoriesInlineKeyboard _categoriesInlineKeyboard;
        private readonly LikeInlineKeyboard _likeInlineKeyboard;
        private Queue<GetProductResponse> _productsForSwaping = [];

        public TelegramBotService(
            ITelegramBotClient botClient,
            HttpClient httpClient,
            ILogger<TelegramBotService> logger,
            IUserService userService,
            IProductService productService,
            IMediaService mediaService,
            IFavouriteService favouriteService,
            IUserSessionService userSessionService,
            IProductSessionService productSessionService,
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
            _favouriteService = favouriteService;
            _userSessionService = userSessionService;
            _productSessionService = productSessionService;
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
                _logger.LogInformation($"Отримано фото | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");

            else if (msg.Type ==  MessageType.Text)
                _logger.LogInformation($"Отримано повiдомлення: {msg.Text} | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");

            else if (string.IsNullOrEmpty(msg.Text) && (msg.Type != MessageType.Photo || msg.Type != MessageType.Video))
            {
                _logger.LogWarning("Повідомлення не може бути пустим.");
                await botClient.SendMessage(chatId, "Повідомлення не може бути пустим.", cancellationToken: cancellationToken);
                return;
            }
            await _userService.AddUnRegisteredUser(botClient, chatId, userName, cancellationToken);

            if (await HandleBaseCommand(botClient, chatId, msg, cancellationToken))
                return;

            if (await _userSessionService.UserSessionCheck(botClient, chatId, msg, cancellationToken))
                return;
            

            if (await _productSessionService.ProductSessionCheck(botClient, chatId, msg, cancellationToken))
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


                case "🚀":
                    if (!_productSessions.ContainsKey(chatId))
                        _productSessions[chatId] = new ProductSession();
                    _productSessions[chatId].State = ProductSessionState.AwaitCategoryForSwaping;
                    var keyboard = await _categoriesInlineKeyboard.SendInlineButtonsAsync(_httpClient, botClient, chatId, cancellationToken);
                    await botClient.SendMessage(chatId, "Оберіть категорію", cancellationToken: cancellationToken, replyMarkup: keyboard);
                    return true;

                case "назад":
                    await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Дію скасовано");
                    return true;

                default:
                    return false;
            }
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

            if (splitData[0] == "next")
            {
                await SendNextLikedProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "like")
            {
                await _favouriteService.AddToFavouriteProduct(botClient,
                    chatId,
                    int.Parse(splitData[1]),
                    cancellationToken);
                await SendNextProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "first")
            {
                if(splitData[1] == "new")
                {
                    _productSessions[chatId].ProductsQueue = await _favouriteService.GetFavouriteProducts(botClient, chatId, "FirstNew", cancellationToken);
                    await SendNextLikedProduct(botClient, chatId, cancellationToken);
                }
                else if (splitData[1] == "old")
                {
                    _productSessions[chatId].ProductsQueue = await _favouriteService.GetFavouriteProducts(botClient, chatId, "FirstOld", cancellationToken);
                    await SendNextLikedProduct(botClient, chatId, cancellationToken);
                }
            }
            else if (splitData[0] == "write")
            {
                await botClient.SendMessage(chatId, "Вибачте, ця функція ще не реалізована", cancellationToken: cancellationToken);
                return;
            }
            else if (splitData[0] == "dislike")
            {
                await _favouriteService.DislikeProduct(botClient,
                    chatId,
                    int.Parse(splitData[1]),
                    cancellationToken);
                await SendNextProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "update")
            {
                await _productSessionService.UpdateProduct(botClient, callbackQuery.Message, chatId, int.Parse(splitData[1]), ProductSessionState, cancellationToken);
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
                await botClient.SendMessage(chatId, "Вподобаних товарів немає", cancellationToken: cancellationToken);
                return;
            }
            var product = _productSessions[chatId].ProductsQueue.Dequeue();
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("наступний продукт", $"next"),
                InlineKeyboardButton.WithCallbackData("напиcати продавцю", $"write_{product}"),
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

            var mediaList = await _mediaService.GetMediaGroup(botClient, product.MediaUrls, cancellationToken);

            if (mediaList.Count() > 0)
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