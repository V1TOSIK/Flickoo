using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Flickoo.Telegram.DTOs;

namespace Flickoo.Telegram.Services
{
    class ProductSessionService : IProductSessionService
    {
        private readonly ILogger<ProductSessionService> _logger;
        private readonly MainKeyboard _mainKeyboard;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly Dictionary<long, ProductSession> _productSessions = new();
        private readonly LikeInlineKeyboard _likeInlineKeyboard;
        private readonly CategoriesInlineKeyboard _categoriesInlineKeyboard;

        public ProductSessionService(ILogger<ProductSessionService> logger,
            MainKeyboard mainKeyboard,
            IProductService productService,
            IMediaService mediaService,
            LikeInlineKeyboard likeInlineKeyboard,
            CategoriesInlineKeyboard categoriesInlineKeyboard)
        {
            _logger = logger;
            _mainKeyboard = mainKeyboard;
            _productService = productService;
            _mediaService = mediaService;   
            _likeInlineKeyboard = likeInlineKeyboard;
            _categoriesInlineKeyboard = categoriesInlineKeyboard;
        }

        public async Task<bool> ProductSessionCheck(ITelegramBotClient botClient,
            long chatId,
            Message msg,
            CancellationToken cancellationToken)
        {
            var session = GetProductSession(chatId);

            if (session.State != ProductSessionState.Idle)
            {
                switch (session.State)
                {
                    case ProductSessionState.WaitingForCategory:
                        if (msg.Text == "назад")
                        {
                            session.State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        return true;

                    case ProductSessionState.WaitingForProductName:
                        if (msg.Text == "назад")
                        {
                            session.State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        session.ProductName = msg.Text ?? "";
                        session.State = await _productService.AddProduct(botClient,
                            chatId,
                            session.CategoryId,
                            session.ProductName,
                            session.Price,
                            session.ProductDescription,
                            session.MediaUrls,
                            session.AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForPrice:
                        if (msg.Text == "назад")
                        {
                            session.State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        if (!decimal.TryParse(msg.Text, out var price))
                        {
                            await botClient.SendMessage(chatId, "Ціна повинна бути числом", cancellationToken: cancellationToken);
                            return true;
                        }
                        session.Price = price;
                        session.State = await _productService.AddProduct(botClient,
                            chatId,
                            session.CategoryId,
                            session.ProductName,
                            session.Price,
                            session.ProductDescription,
                            session.MediaUrls,
                            session.AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForDescription:
                        if (msg.Text == "назад")
                        {
                            session.State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }
                        session.ProductDescription = msg.Text ?? "";
                        session.State = await _productService.AddProduct(botClient,
                            chatId,
                            session.CategoryId,
                            session.ProductName,
                            session.Price,
                            session.ProductDescription,
                            session.MediaUrls,
                            session.AddMoreMedia,
                            cancellationToken);
                        return true;

                    case ProductSessionState.WaitingForMedia:
                        if (msg.Text == "назад")
                        {
                            session.State = ProductSessionState.Idle;
                            await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                            return true;
                        }


                        if (msg.Text == "надіслати фото заново")
                        {
                            _logger.LogInformation("Повторне надсилання фото/відео");
                            session.MediaUrls.Clear();
                            session.State = await _productService.AddProduct(botClient,
                                chatId,
                                session.CategoryId,
                                session.ProductName,
                                session.Price,
                                session.ProductDescription,
                                session.MediaUrls,
                                session.AddMoreMedia,
                                cancellationToken);
                            return true;
                        }

                        if (msg.Text == "готово")
                        {
                            session.AddMoreMedia = false;
                            session.State = await _productService.AddProduct(botClient,
                                chatId,
                                session.CategoryId,
                                session.ProductName,
                                session.Price,
                                session.ProductDescription,
                                session.MediaUrls,
                                session.AddMoreMedia,
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

                        if (msg.Photo == null || session.MediaUrls.Count() >= 5)
                        {
                            session.MediaUrls.RemoveRange(5, session.MediaUrls.Count() - 5);
                            session.AddMoreMedia = false;
                        }

                        session.MediaUrls.Add(await _mediaService.GetMediaIdFromMsg(botClient, msg, chatId, cancellationToken));

                        session.State = await _productService.AddProduct(botClient,
                                chatId,
                                session.CategoryId,
                                session.ProductName,
                                session.Price,
                                session.ProductDescription,
                                session.MediaUrls,
                                session.AddMoreMedia,
                                cancellationToken);

                        return true;

                    case ProductSessionState.AddProduct:
                        session.State = await _productService.AddProduct(botClient,
                            chatId,
                            session.CategoryId,
                            session.ProductName,
                            session.Price,
                            session.ProductDescription,
                            session.MediaUrls,
                            session.AddMoreMedia,
                            cancellationToken);
                        if (session.State == ProductSessionState.Idle)
                            ResetSession(chatId);
                        return true;

                    default:
                        ResetSession(chatId);
                        return false;

                }
            }
            else
                return await HandleProductCommand(botClient, msg, chatId, cancellationToken);

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

            if (!_productSessions.ContainsKey(chatId))
                _productSessions[chatId] = new ProductSession();

            switch (command.Text.ToLower())
            {
                case "вподобане":
                    _productSessions[chatId].State = ProductSessionState.SwapingLikedProducts;
                    var likeKeyboard = _likeInlineKeyboard.SendLikeInlineButtonsAsync(botClient, chatId, cancellationToken: cancellationToken);
                    await botClient.SendMessage(chatId, "Оберіть спосіб сортування", cancellationToken: cancellationToken, replyMarkup: likeKeyboard);
                    return true;

                case "🚀":
                    _productSessions[chatId].State = ProductSessionState.AwaitCategoryForSwaping;
                    var keyboard = await _categoriesInlineKeyboard.SendInlineButtonsAsync(botClient, chatId, cancellationToken);
                    await botClient.SendMessage(chatId, "Оберіть категорію", cancellationToken: cancellationToken, replyMarkup: keyboard);
                    return true;

                case "мої оголошення":
                    await _productService.GetUserProducts(botClient, chatId, cancellationToken);
                    
                    return true;
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

                    if (_productSessions[chatId].State == ProductSessionState.Idle)
                        _productSessions.Remove(chatId);

                    return true;

                default:
                    return false;
            }
        }

        public async Task<bool> UpdateProduct(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            long productId,
            CancellationToken cancellationToken)
        {
            switch (GetProductSession(chatId).State)
            {
                case ProductSessionState.WaitingForProductNameUpdate:
                    if (msg.Text == "назад")
                    {
                        _productSessions[chatId].State = ProductSessionState.Idle;
                        await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                        return true;
                    }
                    _productSessions[chatId].ProductName = msg.Text ?? "";
                    _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                        chatId,
                        _productSessions[chatId].CategoryId,
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
                        await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
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
                        _productSessions[chatId].CategoryId,
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
                        await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                        return true;
                    }
                    _productSessions[chatId].ProductDescription = msg.Text ?? "";

                    _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
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
                        await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Додавання продукту скасовано");
                        return true;
                    }


                    if (msg.Text == "надіслати фото заново")
                    {
                        _logger.LogInformation("Повторне надсилання фото/відео");
                        _productSessions[chatId].MediaUrls.Clear();
                        _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
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
                        _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
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

                    _productSessions[chatId].MediaUrls.Add(await _mediaService.GetMediaIdFromMsg(botClient, msg, chatId, cancellationToken));

                    _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                            chatId,
                            _productSessions[chatId].CategoryId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                    _productSessions[chatId].AddMoreMedia,
                            cancellationToken);

                    return true;

                case ProductSessionState.UpdateProduct:
                    _productSessions[chatId].State = await _productService.UpdateProduct(botClient,
                            chatId,
                            _productSessions[chatId].ProductId,
                            _productSessions[chatId].ProductName,
                            _productSessions[chatId].Price,
                            _productSessions[chatId].ProductDescription,
                            _productSessions[chatId].MediaUrls,
                            _productSessions[chatId].AddMoreMedia,
                            cancellationToken);

                    if (_productSessions[chatId].State == ProductSessionState.Idle)
                        _productSessions.Remove(chatId);
                    return true;
            }
            return false;
        }

        public async Task SendNextLikedProduct(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
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

        public async Task SendNextProduct(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            if (!_productSessions.ContainsKey(chatId) || _productSessions[chatId].ProductsQueue.Count == 0)
            {
                await botClient.SendMessage(chatId, "Більше товарів немає.", cancellationToken: cancellationToken);
                ResetSession(chatId);
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

        public void SetProductsQueue(long chatId,
            IEnumerable<GetProductResponse> products)
        {
            var session = GetProductSession(chatId);
            session.ProductsQueue = new Queue<GetProductResponse>(products);
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
                session.ProductName = string.Empty;
                session.Price = 0m;
                session.ProductDescription = string.Empty;
                session.CategoryId = 0;
                session.AddMoreMedia = true;
            }
        }
    }
}
