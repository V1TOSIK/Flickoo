using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Flickoo.Telegram
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IFavouriteService _favouriteService;
        private readonly IUserSessionService _userSessionService;
        private readonly IProductSessionService _productSessionService;
        private readonly MainKeyboard _mainKeyboard;

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
            _logger = logger;
            _userService = userService;
            _productService = productService;
            _favouriteService = favouriteService;
            _userSessionService = userSessionService;
            _productSessionService = productSessionService;
            _mainKeyboard = mainKeyboard;
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
                case "назад":
                    await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Дію скасовано");
                    _userSessionService.ResetSession(chatId);
                    _productSessionService.ResetSession(chatId);
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
                await _productSessionService.SendNextLikedProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "like")
            {
                await _favouriteService.AddToFavouriteProduct(botClient,
                    chatId,
                    int.Parse(splitData[1]),
                    cancellationToken);
                await _productSessionService.SendNextProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "first")
            {
                var session = _productSessionService.GetProductSession(chatId);
                if (splitData[1] == "new")
                {
                    session.ProductsQueue = await _favouriteService.GetFavouriteProducts(botClient, chatId, "FirstNew", cancellationToken);
                    await _productSessionService.SendNextLikedProduct(botClient, chatId, cancellationToken);
                }
                else if (splitData[1] == "old")
                {
                    session.ProductsQueue = await _favouriteService.GetFavouriteProducts(botClient, chatId, "FirstOld", cancellationToken);
                    await _productSessionService.SendNextLikedProduct(botClient, chatId, cancellationToken);
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
                await _productSessionService.SendNextProduct(botClient, chatId, cancellationToken);
            }
            else if (splitData[0] == "update")
            {
                await _productSessionService.UpdateProduct(botClient, callbackQuery.Message, chatId, int.Parse(splitData[1]), cancellationToken);
            }
            else if (splitData[0] == "delete")
            {
                await _productService.DeleteProduct(botClient, chatId, int.Parse(splitData[1]), cancellationToken);
            }
            else
            {
                var session = _productSessionService.GetProductSession(chatId);
                switch (session.State)
                {
                    case ProductSessionState.WaitingForCategory:
                        session.CategoryId = int.Parse(callbackQuery.Data);
                        session.State = await _productService.AddProduct(botClient,
                            chatId,
                            session.CategoryId,
                            session.ProductName,
                            session.Price,
                            session.ProductDescription,
                            session.MediaUrls,
                            session.AddMoreMedia,
                            cancellationToken);
                        break;

                    case ProductSessionState.AwaitCategoryForSwaping:

                        session.ProductsQueue = await _productService.GetProductsForSwaping(botClient, chatId, int.Parse(callbackQuery.Data), cancellationToken);

                        await _productSessionService.SendNextProduct(botClient, chatId, cancellationToken);
                        return;

                    case ProductSessionState.SwapingLikedProducts:
                        await _productSessionService.SendNextLikedProduct(botClient, chatId, cancellationToken);
                        return;

                    default:
                        session.State = ProductSessionState.Idle;
                        return;
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