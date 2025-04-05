using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Services
{
    class ProductSessionService : IProductSessionService
    {
        private readonly ILogger<ProductSessionService> _logger;
        private readonly MainKeyboard _mainKeyboard;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly Dictionary<long, ProductSession> _productSessions = [];
        private readonly LikeInlineKeyboard _likeInlineKeyboard;
        private readonly CategoriesInlineKeyboard _categoriesInlineKeyboard;
        private readonly IFavouriteService _favouriteService;

        public ProductSessionService(ILogger<ProductSessionService> logger,
            MainKeyboard mainKeyboard,
            IProductService productService,
            IMediaService mediaService,
            LikeInlineKeyboard likeInlineKeyboard,
            CategoriesInlineKeyboard categoriesInlineKeyboard,
            IFavouriteService favouriteService)
        {
            _logger = logger;
            _mainKeyboard = mainKeyboard;
            _productService = productService;
            _mediaService = mediaService;   
            _likeInlineKeyboard = likeInlineKeyboard;
            _categoriesInlineKeyboard = categoriesInlineKeyboard;
            _favouriteService = favouriteService;
        }

        public async Task<bool> ProductSessionCheck(ITelegramBotClient botClient,
            long chatId,
            Message msg,
            CancellationToken cancellationToken)
        {
            var session = GetProductSession(chatId);

            if (string.IsNullOrEmpty(session.Action))
                return await HandleProductCommand(botClient, msg, chatId, cancellationToken);

            else if (session.Action == "add")
                return await AddProduct(botClient, msg, chatId, cancellationToken);

            else if (session.Action == "update")
                return await UpdateProduct(botClient, msg, chatId, cancellationToken);

            else
                return false;
        }


        public async Task<bool> HandleProductCommand(ITelegramBotClient botClient,
            Message command,
            long chatId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                return false;
            }

            var session = GetProductSession(chatId);

            switch (command.Text.ToLower())
            {
                case "вподобане":
                    ResetSession(chatId);
                    session.State = ProductSessionState.SwapingLikedProducts;
                    var likeKeyboard = _likeInlineKeyboard.SendLikeInlineButtonsAsync(botClient, chatId, cancellationToken: cancellationToken);
                    await botClient.SendMessage(chatId, "Оберіть спосіб сортування", cancellationToken: cancellationToken, replyMarkup: likeKeyboard);
                    return true;

                case "🚀":
                    ResetSession(chatId);
                    session.State = ProductSessionState.AwaitCategoryForSwaping;
                    var keyboard = await _categoriesInlineKeyboard.SendInlineButtonsAsync(botClient, chatId, cancellationToken);
                    await botClient.SendMessage(chatId, "Оберіть категорію", cancellationToken: cancellationToken, replyMarkup: keyboard);
                    return true;

                case "мої оголошення":
                    ResetSession(chatId);
                    await _productService.GetUserProducts(botClient, chatId, cancellationToken);
                    return true;

                case "додати продукт":
                    session.Action = "add";
                    session.State = await _productService.AddProduct(botClient, chatId, session, cancellationToken);

                    if (session.State == ProductSessionState.Idle)
                        ResetSession(chatId);
                    return true;

                case "назад":
                    await CancelAction(botClient, chatId, "Додавання продукту скасовано", cancellationToken);
                    return true;

                default:
                    return false;
            }
        }

        public async Task<bool> AddProduct(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken)
        {
            var session = GetProductSession(chatId);
            switch (session.State)
            {
                case ProductSessionState.WaitingForCategory:
                    return true;

                case ProductSessionState.WaitingForProductName:
                    session.Name = msg.Text ?? "";
                    session.State = await _productService.AddProduct(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForPrice:
                    if (!decimal.TryParse(msg.Text, out var price))
                    {
                        await botClient.SendMessage(chatId, "Ціна повинна бути числом", cancellationToken: cancellationToken);
                        return true;
                    }
                    session.Price = price;
                    session.State = await _productService.AddProduct(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForDescription:
                    session.ProductDescription = msg.Text ?? "";
                    session.State = await _productService.AddProduct(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForMedia:

                    if (msg.Type != MessageType.Photo && msg.Type != MessageType.Video && string.IsNullOrEmpty(msg.Text))
                    {
                        _logger.LogWarning("Ви скинули не фото/відео");
                        await _mainKeyboard.SendMainKeyboard(botClient, chatId, "ви скинули не фото/відео");
                        return true;
                    }

                    if (msg.Photo == null || session.MediaUrls.Count() > 5)
                    {
                        session.MediaUrls.RemoveRange(5, session.MediaUrls.Count() - 5);
                    }

                    if (msg.Text == "надіслати фото заново")
                    {
                        _logger.LogInformation("Повторне надсилання фото/відео");
                        session.MediaUrls.Clear();
                        return true;
                    }

                    if (msg.Text == "готово")
                    {
                        session.AddMoreMedia = false;
                        session.State = await _productService.AddProduct(botClient, chatId, session, cancellationToken);
                        ResetSession(chatId);
                        return true;
                    }

                    session.MediaUrls.Add(await _mediaService.GetMediaIdFromMsg(botClient, msg, chatId, cancellationToken));
                    session.State = await _productService.AddProduct(botClient, chatId, session, cancellationToken);

                    return true;

                default:
                    ResetSession(chatId);
                    return false;

            }
        }

        public async Task<bool> UpdateProduct(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken)
        {
            var session = GetProductSession(chatId);
            switch (session.State)
            {
                case ProductSessionState.Idle:
                    session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                    return true;
                case ProductSessionState.WaitingForProductName:
                    session.Name = msg.Text ?? "";
                    session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForPrice:
                    if (!decimal.TryParse(msg.Text, out var updatePrice))
                    {
                        await botClient.SendMessage(chatId, "Ціна повинна бути числом", cancellationToken: cancellationToken);
                        return true;
                    }
                    session.Price = updatePrice;
                    session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForDescription:
                    session.ProductDescription = msg.Text ?? "";

                    session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForMedia:

                    if (msg.Type != MessageType.Photo && msg.Type != MessageType.Video && string.IsNullOrEmpty(msg.Text))
                    {
                        _logger.LogWarning("Ви скинули не фото/відео");
                        await _mainKeyboard.SendMainKeyboard(botClient, chatId, "ви скинули не фото/відео");
                        return true;
                    }

                    if (session.MediaUrls.Count() >= 5)
                    {
                        session.MediaUrls.RemoveRange(5, session.MediaUrls.Count() - 5);
                        
                    }

                    if (msg.Text == "надіслати фото заново")
                    {
                        _logger.LogInformation("Повторне надсилання фото/відео");
                        session.MediaUrls.Clear();
                        session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                        return true;
                    }

                    if (msg.Text == "готово")
                    {
                        session.AddMoreMedia = false;
                        session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                        ResetSession(chatId);
                        return true;
                    }

                    session.MediaUrls.Add(await _mediaService.GetMediaIdFromMsg(botClient, msg, chatId, cancellationToken));
                    session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);

                    return true;

                default:
                    ResetSession(chatId);
                    return false;
            }
        }

        public async Task SendNextLikedProduct(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var session = GetProductSession(chatId);

            if (session.ProductsQueue.Count == 0)
            {
                await botClient.SendMessage(chatId, "Вподобаних товарів немає", cancellationToken: cancellationToken);
                return;
            }
            var product = session.ProductsQueue.Dequeue();
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("➡️", $"next"),
                InlineKeyboardButton.WithCallbackData("💬", $"write_{product.Id}"),
                InlineKeyboardButton.WithCallbackData("👎", $"dislike_{product.Id}")
            });
            var mediaList = await _mediaService.GetMediaGroup(botClient, product.MediaUrls, cancellationToken);



            string productText = $"📢 {product.Name}\n" +
                         $"💰 {product.Price} грн\n" +
                         /*$"📍 Локація: {product.Location}\n" +*/
                         $"──────────────────────────\n" +
                         $"📜 Опис: {product.Description}";

            if (mediaList != null && mediaList.Count > 0)
            {
                await botClient.SendMediaGroup(
                    chatId: chatId,
                    mediaList,
                    cancellationToken: cancellationToken
                );
                

                await botClient.SendMessage(chatId,
                    productText,
                    cancellationToken: cancellationToken,
                    replyMarkup: inlineKeyboard);
                return;
            }
            else
            {
                await botClient.SendMessage(chatId,
                    productText,
                    cancellationToken: cancellationToken,
                    replyMarkup: inlineKeyboard);
                return;
            }
        }

        public async Task SendNextProduct(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var session = GetProductSession(chatId);
            if ( session.ProductsQueue.Count == 0)
            {
                await botClient.SendMessage(chatId, "Більше товарів немає.", cancellationToken: cancellationToken);
                ResetSession(chatId);
                return;
            }

            var product = session.ProductsQueue.Dequeue();

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("❤️", $"like_{product.Id}"),
                InlineKeyboardButton.WithCallbackData("👎", $"dislike_{product.Id}")
            });

            var mediaList = await _mediaService.GetMediaGroup(botClient, product.MediaUrls, cancellationToken);

            string productText = $"📢 {product.Name}\n" +
                         $"💰 {product.Price} грн\n" +
                         /*$"📍 Локація: {product.Location}\n" +*/
                         $"──────────────────────────\n" +
                         $"📜 Опис: {product.Description}";

            if (mediaList.Count() > 0)
            {
                await botClient.SendMediaGroup(
                    chatId: chatId,
                    mediaList,
                    cancellationToken: cancellationToken
                );

                await botClient.SendMessage(chatId,
                    productText,
                    cancellationToken: cancellationToken,
                    replyMarkup: inlineKeyboard);
                return;
            }
            else
            {
                await botClient.SendMessage(chatId,
                    productText,
                    cancellationToken: cancellationToken,
                    replyMarkup: inlineKeyboard);
                return;

            }
        }

        public ProductSession GetProductSession(long chatId)
        {
            if (!_productSessions.TryGetValue(chatId, out var session))
            {
                session = new ProductSession();
                _productSessions[chatId] = session;
            }

            return session;
        }
        
        public void ResetSession(long chatId)
        {
            if(_productSessions.ContainsKey(chatId))
            {
                var session = _productSessions[chatId];
                session.State = ProductSessionState.Idle;
                session.ProductId = 0;
                session.MediaUrls.Clear();
                session.Name = string.Empty;
                session.Price = 0m;
                session.ProductDescription = string.Empty;
                session.CategoryId = 0;
                session.AddMoreMedia = true;
                session.Action = string.Empty;

                _productSessions.Remove(chatId);
            }
        }

        private async Task CancelAction(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            await _mainKeyboard.SendMainKeyboard(botClient, chatId, messageText);
            ResetSession(chatId);
        }

        public async Task<bool> ProductCallback(ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            long chatId,
            CancellationToken cancellationToken)
        {

            if (string.IsNullOrEmpty(callbackQuery.Data))
            {
                _logger.LogWarning("CallbackQuery не може бути пустим.");
                await botClient.SendMessage(chatId, "CallbackQuery не може бути пустим.", cancellationToken: cancellationToken);
                return false;
            }

            var splitData = callbackQuery.Data.Split("_");
            var session = GetProductSession(chatId);

            switch (splitData[0])
            {
                case "like":
                    await _favouriteService.AddToFavouriteProduct(botClient,
                        chatId,
                        int.Parse(splitData[1]),
                        cancellationToken);
                    await SendNextProduct(botClient, chatId, cancellationToken);
                    return true;

                case "dislike":
                    await _favouriteService.DislikeProduct(botClient,
                    chatId,
                    long.Parse(splitData[1]),
                    cancellationToken);
                    await SendNextProduct(botClient, chatId, cancellationToken);
                    return true;

                case "next":
                    await SendNextProduct(botClient, chatId, cancellationToken);
                    return true;

                case "write":
                    await _productService.WriteToSeller(botClient,
                        chatId,
                        long.Parse(splitData[1]),
                        callbackQuery?.From?.Username ?? "Unknown",
                        cancellationToken);
                    return true;

                case "update":
                    session.ProductId = long.Parse(splitData[1]);
                    session.Action = "update";
                    await ProductSessionCheck(botClient,
                        chatId,
                        callbackQuery.Message,
                        cancellationToken);
                    return true;

                case "delete":
                    await _productService.DeleteProduct(botClient,
                    chatId,
                    int.Parse(splitData[1]),
                    cancellationToken);
                    return true;

                case "first":
                    if (splitData[1] == "old")
                    {
                        session.ProductsQueue = await _favouriteService.GetFavouriteProducts(botClient, chatId, "FirstOld", cancellationToken);
                        await SendNextLikedProduct(botClient, chatId, cancellationToken);
                    }
                    else if (splitData[1] == "new")
                    {
                        session.ProductsQueue = await _favouriteService.GetFavouriteProducts(botClient, chatId, "FirstNew", cancellationToken);
                        await SendNextLikedProduct(botClient, chatId, cancellationToken);
                    }
                    return true;

                default:
                    switch (session.State)
                    {
                        case ProductSessionState.WaitingForCategory:
                            session.CategoryId = int.Parse(callbackQuery.Data);
                            session.State = await _productService.AddProduct(botClient,
                                chatId,
                                session,
                                cancellationToken);
                            return true;

                        case ProductSessionState.AwaitCategoryForSwaping:
                            session.ProductsQueue = await _productService.GetProductsForSwaping(botClient, chatId, int.Parse(callbackQuery.Data), cancellationToken);

                            await SendNextProduct(botClient, chatId, cancellationToken);
                            return true;

                            case ProductSessionState.SwapingLikedProducts:
                            await SendNextLikedProduct(botClient, chatId, cancellationToken);
                            return true;
                    }
                    return false;

            }
        }
    }
}
