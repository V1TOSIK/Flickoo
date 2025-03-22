using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace Flickoo.Telegram.Services
{
    class ProductSessionService : IProductSessionService
    {
        private readonly ILogger<ProductSessionService> _logger;
        private readonly MainKeyboard _mainKeyboard;
        private readonly MyProductKeyboard _myProductKeyboard;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly Dictionary<long, ProductSession> _productSessions = new();
        public ProductSessionService(ILogger<ProductSessionService> logger,
            MainKeyboard mainKeyboard,
            MyProductKeyboard myProductKeyboard,
            IProductService productService,
            IMediaService mediaService,
            Dictionary<long,
            ProductSession> productSessions)
        {
            _logger = logger;
            _mainKeyboard = mainKeyboard;
            _myProductKeyboard = myProductKeyboard;
            _productService = productService;
            _mediaService = mediaService;
            _productSessions = productSessions;
        }

        public async Task<bool> ProductSessionCheck(ITelegramBotClient botClient, long chatId, Message msg, CancellationToken cancellationToken)
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

                        _productSessions[chatId].MediaUrls.Add(await _mediaService.GetMediaIdFromMsg(botClient, msg, chatId, cancellationToken));

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

                    case ProductSessionState.AddProduct:
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

        public async Task<bool> UpdateProduct(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            long productId,
            ProductSessionState productSessionState,
            CancellationToken cancellationToken)
        {
            if (!_productSessions.ContainsKey(chatId))
                _productSessions[chatId] = new ProductSession();
            switch (productSessionState)
            {
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

                _productSessions[chatId].MediaUrls.Add(await _mediaService.GetMediaIdFromMsg(botClient, msg, chatId, cancellationToken));

                return true;
            }
            return false;
        }

        public async Task<bool> HandleProductCommand(ITelegramBotClient botClient,
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
                    await _productService.GetUserProducts(botClient, chatId, cancellationToken);
                    await _myProductKeyboard.SendMyProductKeyboard(botClient, chatId, cancellationToken);
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
            return true;
        }
    }
}
