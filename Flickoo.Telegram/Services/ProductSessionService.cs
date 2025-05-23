﻿using Flickoo.Telegram.enums;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

using TParseMode = Telegram.Bot.Types.Enums.ParseMode;
using System;

namespace Flickoo.Telegram.Services
{
    class ProductSessionService : IProductSessionService
    {
        private readonly ILogger<ProductSessionService> _logger;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly IFavouriteService _favouriteService;
        private readonly IKeyboards _keyboards;
        private readonly Dictionary<long, ProductSession> _productSessions = [];

        public ProductSessionService(ILogger<ProductSessionService> logger,
            IProductService productService,
            IMediaService mediaService,
            IFavouriteService favouriteService,
            IKeyboards keyboards)
        {
            _logger = logger;
            _productService = productService;
            _mediaService = mediaService;
            _favouriteService = favouriteService;
            _keyboards = keyboards;
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
                case "⭐":
                    ResetSession(chatId);
                    session.State = ProductSessionState.SwapingLikedProducts;
                    await _keyboards.SendLikeFilterButtons(botClient, chatId, "Оберіть спосіб сортування", cancellationToken: cancellationToken);
                    return true;

                case "🚀":
                    ResetSession(chatId);
                    session.State = ProductSessionState.AwaitCategoryForSwaping;
                    await _keyboards.SendCategoriesInlineButtons(botClient, chatId, "Оберіть категорію", true, cancellationToken);
                    return true;

                case "📢":
                    ResetSession(chatId);
                    await _productService.GetUserProducts(botClient, chatId, cancellationToken);
                    return true;

                case "додати продукт":
                    
                    session.Action = "add";
                    session.State = await _productService.AddProductAsync(botClient, chatId, session, cancellationToken);

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
                    session.State = await _productService.AddProductAsync(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForPriceCurrency:
                    session.PriceCurrency = msg.Text ?? "";
                    session.State = await _productService.AddProductAsync(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForPriceAmount:
                    if (!decimal.TryParse(msg.Text, out var priceAmount))
                    {
                        await botClient.SendMessage(chatId, "Ціна повинна бути числом", cancellationToken: cancellationToken);
                        return true;
                    }
                    session.PriceAmount = priceAmount;
                    session.State = await _productService.AddProductAsync(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForDescription:
                    session.ProductDescription = msg.Text ?? "";
                    session.State = await _productService.AddProductAsync(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForMedia:

                    if (msg.Type != MessageType.Photo && msg.Type != MessageType.Video && string.IsNullOrEmpty(msg.Text))
                    {
                        _logger.LogWarning("Ви скинули не фото/відео");
                        await _keyboards.SendMainKeyboard(botClient, chatId, "ви скинули не фото/відео", cancellationToken);
                        return true;
                    }

                    if (msg.Text == "надіслати фото заново")
                    {
                        await _keyboards.SendMediaKeyboard(botClient, chatId, "Повторне надсилання фото/відео", cancellationToken: cancellationToken);
                        _logger.LogInformation("Повторне надсилання фото/відео");
                        session.MediaFiles.Clear();
                        session.MediaTypes.Clear();
                        return true;
                    }

                    if (msg.Text == "готово")
                    {
                        if (session.MediaFiles.Count == 0)
                        {
                            await _keyboards.SendMediaKeyboard(botClient, chatId, "Ви не надіслали жодного фото/відео", cancellationToken: cancellationToken);
                            return true;
                        }
                        session.AddMoreMedia = false;
                        session.State = await _productService.AddProductAsync(botClient, chatId, session, cancellationToken);
                        ResetSession(chatId);
                        return true;
                    }

                    session.MediaFiles.Add(await _mediaService.GetMediaFileFromMsgAsync(botClient, msg, chatId, cancellationToken));
                    session.MediaTypes.Add(_mediaService.GetMediaTypeFromMsgAsync(msg, cancellationToken));

                    if (session.MediaFiles.Count > 5)
                    {
                        session.MediaFiles.RemoveRange(5, session.MediaFiles.Count - 5);
                        session.MediaTypes.RemoveRange(5, session.MediaTypes.Count - 5);
                    }


                    session.State = await _productService.AddProductAsync(botClient, chatId, session, cancellationToken);

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

                case ProductSessionState.WaitingForPriceCurrency:
                    session.PriceCurrency = msg.Text ?? "";
                    session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                    return true;

                case ProductSessionState.WaitingForPriceAmount:
                    if (!decimal.TryParse(msg.Text, out var updatePrice))
                    {
                        await botClient.SendMessage(chatId, "Ціна повинна бути числом", cancellationToken: cancellationToken);
                        return true;
                    }
                    session.PriceAmount = updatePrice;
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
                        await _keyboards.SendMainKeyboard(botClient, chatId, "ви скинули не фото/відео", cancellationToken);
                        return true;
                    }

                    if (msg.Text == "надіслати фото заново")
                    {
                        await _keyboards.SendMediaKeyboard(botClient, chatId, "Повторне надсилання фото/відео", cancellationToken: cancellationToken);
                        _logger.LogInformation("Повторне надсилання фото/відео");
                        session.MediaFiles.Clear();
                        session.MediaTypes.Clear();
                        return true;
                    }

                    if (msg.Text == "готово")
                    {
                        if (session.MediaFiles.Count == 0)
                        {
                            await _keyboards.SendMediaKeyboard(botClient, chatId, "Ви не надіслали жодного фото/відео", cancellationToken: cancellationToken);
                            return true;
                        }

                        session.AddMoreMedia = false;
                        session.State = await _productService.UpdateProduct(botClient, chatId, session, cancellationToken);
                        ResetSession(chatId);
                        return true;
                    }

                    session.MediaFiles.Add(await _mediaService.GetMediaFileFromMsgAsync(botClient, msg, chatId, cancellationToken));
                    session.MediaTypes.Add(_mediaService.GetMediaTypeFromMsgAsync(msg, cancellationToken));

                    if (session.MediaFiles.Count >= 5)
                    {
                        session.MediaFiles.RemoveRange(5, session.MediaFiles.Count - 5);
                        session.MediaTypes.RemoveRange(5, session.MediaTypes.Count - 5);
                    }

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
                InlineKeyboardButton.WithCallbackData("👎", $"delliked_{product.Id}")
            });
            var mediaList = await _mediaService.GetMediaFromUrlsByProductIdAsync(botClient, product.Id, cancellationToken);

            var caption =
                $"📢 {product.Name}\n" +
                $"💰 {product.PriceAmount} {product.PriceCurrency}\n" +
                $"📍 {product.LocationName}\n" +
                $"📜 Опис: {product.Description}";
            if (mediaList == null || mediaList.Count == 0)
            {
                await botClient.SendMessage(chatId, caption, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
            }
            else
            {

                if (mediaList.Count > 0)
                {
                    if (mediaList[0] is InputMediaPhoto inputMediaPhoto)
                    {
                        inputMediaPhoto.Caption = caption;
                        inputMediaPhoto.ParseMode = TParseMode.Markdown;
                    }
                    else if (mediaList[0] is InputMediaVideo inputMediaVideo)
                    {
                        inputMediaVideo.Caption = caption;
                        inputMediaVideo.ParseMode = TParseMode.Markdown;
                    }
                    await botClient.SendMediaGroup(chatId, mediaList, cancellationToken: cancellationToken);
                    await botClient.SendMessage(
                    chatId: chatId,
                    text: "Що хочете зробити з оголошенням?  📦",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken
                );
                }

            }
        }

        public async Task SendNextProduct(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var session = GetProductSession(chatId);
            if ( session.ProductsQueue.Count == 0)
            {
                await botClient.SendMessage(chatId, "Більше товарів немає.", cancellationToken: cancellationToken);
                session.State = ProductSessionState.AwaitCategoryForSwaping;
                return;
            }

            var product = session.ProductsQueue.Dequeue();

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("❤️", $"like_{product.Id}"),
                InlineKeyboardButton.WithCallbackData("👎", $"dislike_{product.Id}")
            });

            var mediaList = await _mediaService.GetMediaFromUrlsByProductIdAsync(botClient, product.Id, cancellationToken);

            var caption =
                $"📢 {product.Name}\n" +
                $"💰 {product.PriceAmount} {product.PriceCurrency}\n" +
                $"📍 {product.LocationName}\n" +
                $"📜 Опис: {product.Description}";
            if (mediaList == null || mediaList.Count == 0)
            {
                await botClient.SendMessage(chatId, caption, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
            }
            else
            {

                if (mediaList.Count > 0)
                {
                    if (mediaList[0] is InputMediaPhoto inputMediaPhoto)
                    {
                        inputMediaPhoto.Caption = caption;
                        inputMediaPhoto.ParseMode = TParseMode.Markdown;
                    }
                    else if (mediaList[0] is InputMediaVideo inputMediaVideo)
                    {
                        inputMediaVideo.Caption = caption;
                        inputMediaVideo.ParseMode = TParseMode.Markdown;
                    }
                    await botClient.SendMediaGroup(chatId, mediaList, cancellationToken: cancellationToken);
                    await botClient.SendMessage(
                    chatId: chatId,
                    text: "Що хочете зробити з оголошенням?  📦",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken
                );
                }

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
                session.MediaFiles.Clear();
                session.MediaTypes.Clear();
                session.MediaUrls.Clear();
                session.Name = string.Empty;
                session.PriceAmount = 0m;
                session.PriceCurrency = string.Empty;
                session.ProductDescription = string.Empty;
                session.CategoryId = 0;
                session.AddMoreMedia = true;
                session.Action = string.Empty;
            }
        }

        private async Task CancelAction(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            await _keyboards.SendMainKeyboard(botClient, chatId, messageText, cancellationToken);
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

                case "delliked":
                    await _favouriteService.DislikeProduct(botClient,
                        chatId,
                        long.Parse(splitData[1]),
                        cancellationToken);
                    await SendNextLikedProduct(botClient, chatId, cancellationToken);
                    return true;

                case "next":
                    await SendNextLikedProduct(botClient, chatId, cancellationToken);
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
                    if (callbackQuery.Message is null)
                    {
                        _logger.LogWarning("CallbackQuery.Message не може бути пустим.");
                        return false;
                    }

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
                            session.State = await _productService.AddProductAsync(botClient,
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
