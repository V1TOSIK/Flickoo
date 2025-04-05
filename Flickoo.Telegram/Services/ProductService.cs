using Telegram.Bot;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.DTOs;
using System.Net.Http.Json;
using Flickoo.Telegram.Keyboards;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Flickoo.Telegram.SessionModels;

namespace Flickoo.Telegram.Services
{
    public class ProductService : IProductService
    {
        private readonly IMediaService _mediaService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;
        private readonly MainKeyboard _mainKeyboard;
        private readonly MyProductKeyboard _myProductKeyboard;
        private readonly AddProductCategoryInlineKeyboard _addProductCategoryInlineKeyboard;
        private readonly AddProductMediaKeyboard _addProductMediaKeyboard;
        private readonly ProductInlineKeyboard _productInlineKeyboard;

        public ProductService(
            IMediaService mediaService,
            HttpClient httpClient,
            ILogger<ProductService> logger,
            MainKeyboard mainKeyboard,
            MyProductKeyboard myProductKeyboard,
            AddProductCategoryInlineKeyboard addProductCategoryInlineKeyboard,
            AddProductMediaKeyboard addProductMediaKeyboard,
            ProductInlineKeyboard productInlineKeyboard)
        {
            _mediaService = mediaService;
            _httpClient = httpClient;
            _logger = logger;
            _mainKeyboard = mainKeyboard;
            _myProductKeyboard = myProductKeyboard;
            _addProductCategoryInlineKeyboard = addProductCategoryInlineKeyboard;
            _addProductMediaKeyboard = addProductMediaKeyboard;
            _productInlineKeyboard = productInlineKeyboard;
        }

        public async Task GetUserProducts(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                throw new ArgumentNullException(nameof(chatId));
            }

            var productResponse = await _httpClient.GetAsync($"https://localhost:8443/api/Product/{chatId}", cancellationToken);

            if (productResponse.IsSuccessStatusCode)
            {
                var products = await productResponse.Content.ReadFromJsonAsync<List<GetProductResponse>>(cancellationToken: cancellationToken);

                if (products == null || products.Count() == 0)
                {
                    await _myProductKeyboard.SendMyProductKeyboard(botClient, chatId, "Ви ще не додали жодного продукту", cancellationToken);
                    _logger.LogWarning("Користувач ще не додав жодного продукту");

                }
                else
                {
                    foreach (var product in products)
                    {
                        if (products != null)
                        {

                            if (product.MediaUrls == null || product.MediaUrls.Count == 0)
                            {
                                var keyboard = _productInlineKeyboard.SendProductButtons(product.Id, cancellationToken);
                                await botClient.SendMessage(chatId, $"{product.Name}\n{product.Price} грн\n{product.Description}", cancellationToken: cancellationToken, replyMarkup: keyboard);
                            }
                            else
                            {
                                var mediaGroup = await _mediaService.GetMediaGroup(botClient, product.MediaUrls, cancellationToken);

                                if (mediaGroup.Count > 0)
                                    await botClient.SendMediaGroup(chatId, mediaGroup, cancellationToken: cancellationToken);

                                var keyboard = _productInlineKeyboard.SendProductButtons(product.Id, cancellationToken);
                                await botClient.SendMessage(chatId, $"{product.Name}\n{product.Price} грн\n\n{product.Description}", cancellationToken: cancellationToken, replyMarkup: keyboard);
                            }
                        }
                    }
                    await _myProductKeyboard.SendMyProductKeyboard(botClient, chatId, "Ось ваші товари", cancellationToken);
                    _logger.LogInformation("Продукти успішно відправлені");
                }
            }
            else if (productResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Ви ще не зареєстровані");
                _logger.LogWarning("Користувач не зареєстрований!");

            }
            else
            {
                await botClient.SendMessage(chatId, "Помилка при отриманні продуктів", cancellationToken: cancellationToken);
                _logger.LogError("Помилка при отриманні продуктів");
            }

        }

        public async Task<Queue<GetProductResponse>> GetProductsForSwaping(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            CancellationToken cancellationToken)
        {

            var response = await _httpClient.GetAsync($"https://localhost:8443/api/Product/bycategory/{categoryId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<Queue<GetProductResponse>>(cancellationToken: cancellationToken);
                if (products == null || products.Count() == 0)
                {
                    await botClient.SendMessage(chatId, "Вибачте, але в цій категорії немає продуктів", cancellationToken: cancellationToken);
                    _logger.LogWarning("Вибачте, але в цій категорії немає продуктів");
                }
                else
                {
                    _logger.LogInformation("Продукти успішно відправлені");
                    return products;
                }
            }
            else
            {
                await botClient.SendMessage(chatId, "Помилка при отриманні продуктів", cancellationToken: cancellationToken);
                _logger.LogError("Помилка при отриманні продуктів");
            }
            return [];
        }

        public async Task<ProductSessionState> AddProduct(ITelegramBotClient botClient,
            long chatId,
            ProductSession session,
            CancellationToken cancellationToken)
        {
            if (session.CategoryId == 0)
            {
                _logger.LogWarning("Виберіть категорію");
                var categoryKeyboard = await _addProductCategoryInlineKeyboard.SendCategoriesInlineButtonsAsync(_httpClient, botClient, chatId, cancellationToken);
                await botClient.SendMessage(chatId, "Виберіть категорію", replyMarkup: categoryKeyboard, cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForCategory;
            }


            if (string.IsNullOrEmpty(session.Name))
            {
                _logger.LogWarning("Введіть назву продукту");
                await botClient.SendMessage(chatId, "Введіть назву продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForProductName;
            }

            if (session.Price < 1)
            {
                _logger.LogWarning("Введіть ціну(в грн)");
                await botClient.SendMessage(chatId, "Введіть ціну(в грн)", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForPrice;
            }

            if (string.IsNullOrEmpty(session.ProductDescription))
            {
                _logger.LogWarning("Введіть опис продукту");
                await botClient.SendMessage(chatId, "Введіть опис продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForDescription;
            }

            if (session.MediaUrls == null || session.MediaUrls is [])
            {
                _logger.LogWarning("Виберіть фото продукту");
                await botClient.SendMessage(chatId, "Виберіть фото продукту\nПОПЕРЕДЖЕННЯ!\nOбрати можна лише 5 фото/відео", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }
            if (session.MediaUrls.Count() > 5)
            {
                _logger.LogWarning("Можна додати лише 5 фото/відео");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, "Можна додати лише 5 фото/відео!!!!", cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }

            if (session.AddMoreMedia)
            {
                _logger.LogInformation($"Додано {session.MediaUrls.Count()}/5 фото");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, $"Додано {session.MediaUrls.Count()}/5 фото", cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }

            var product = new CreateOrUpdateProductRequest
            {
                MediaUrls = session.MediaUrls,
                Name = session.Name,
                Price = session.Price,
                Description = session.ProductDescription,
                UserId = chatId,
                CategoryId = session.CategoryId
            };

            var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/Product", product, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Продукт успішно додано");
                _logger.LogInformation("Продукт успішно додано");
            }
            else
            {
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Помилка при додаванні продукту");
                _logger.LogError("Помилка при додаванні продукту");
            }

            return ProductSessionState.Idle;
        }

        public async Task<ProductSessionState> UpdateProduct(ITelegramBotClient botClient,
            long chatId,
            ProductSession session,
            CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                _logger.LogWarning("Не вдалося отримати chatId");
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Не вдалося отримати chatId");
                return ProductSessionState.Idle;
            }

            if (string.IsNullOrEmpty(session.Name))
            {
                _logger.LogWarning("Введіть нову назву продукту");
                await botClient.SendMessage(chatId, "Введіть нову назву продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForProductName;
            }

            if (session.Price < 1)
            {
                _logger.LogWarning("Введіть нову ціну(в грн)");
                await botClient.SendMessage(chatId, "Введіть нову ціну (в грн)", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForPrice;
            }

            if (string.IsNullOrEmpty(session.ProductDescription))
            {
                _logger.LogWarning("Введіть новий опис продукту");
                await botClient.SendMessage(chatId, "Введіть новий опис продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForDescription;
            }

            if (session.MediaUrls == null || session.MediaUrls is [])
            {
                _logger.LogWarning("Виберіть нові фото продукту");
                await botClient.SendMessage(chatId, "Виберіть нові фото продукту\nПОПЕРЕДЖЕННЯ!\nOбрати можна лише 5 фото/відео", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }
            if (session.MediaUrls.Count() > 5)
            {
                _logger.LogWarning("Можна додати лише 5 фото/відео");
                await botClient.SendMessage(chatId, "Можна додати лише 5 фото/відео!!!!");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, "Можна додати лише 5 фото/відео!!!!", cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }

            if (session.AddMoreMedia)
            {
                _logger.LogInformation($"Додано {session.MediaUrls.Count()}/5 фото");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, $"Додано {session.MediaUrls.Count()}/5 фото", cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }

            var product = new CreateOrUpdateProductRequest
            {
                Name = session.Name,
                Price = session.Price,
                Description = session.ProductDescription,
                UserId = chatId,
                MediaUrls = session.MediaUrls
            };

            var response = await _httpClient.PutAsJsonAsync($"https://localhost:8443/api/Product/{session.ProductId}", product);
            if (response.IsSuccessStatusCode)
            {
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Продукт успішно оновлено");
                _logger.LogInformation("Продукт успішно оновлено");
            }
            else
            {
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Помилка при оновлені продукту");
                _logger.LogError("Помилка при оновлені продукту");
            }

            return ProductSessionState.Idle;
        }

        public async Task DeleteProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken)
        {
            if (productId == 0)
            {
                _logger.LogWarning("Виберіть продукт для видалення");
                await botClient.SendMessage(chatId, "Виберіть продукт для видалення", cancellationToken: cancellationToken);
                return;
            }
            var response = await _httpClient.DeleteAsync($"https://localhost:8443/api/Product/{productId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Продукт успішно видалено");
                _logger.LogInformation("Продукт успішно видалено");
            }
            else
            {
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Помилка при видаленні продукту");
                _logger.LogError("Помилка при видаленні продукту");
            }
        }

        public async Task WriteToSeller(ITelegramBotClient botClient, long chatId, long productId, string userName, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"https://localhost:8443/api/Product/userId/{productId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseSellerId = await response.Content.ReadAsStringAsync();
                if (long.TryParse(responseSellerId, out long sellerId))
                {
                    if(sellerId == chatId)
                    {
                        await botClient.SendMessage(chatId,
                            "Не можна писати самому собі",
                            cancellationToken: cancellationToken);
                        return;
                    }    
                    Chat sellerChat;
                    try
                    {
                        sellerChat = await botClient.GetChat(sellerId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Не вдалося отримати дані про продавця із id {sellerId}: {ex.Message}");
                        await botClient.SendMessage(chatId,
                            "Не вдалося отримати дані про продавця.",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (string.IsNullOrEmpty(sellerChat.Username))
                    {
                        await botClient.SendMessage(chatId,
                            "Продавець не має публічного username, тому неможливо відкрити прямий чат.",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    string directChatLink = $"https://t.me/{sellerChat.Username}";

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithUrl("Написати продавцю", directChatLink)
                    });

                    await botClient.SendMessage(chatId,
                        "Натисніть кнопку нижче, щоб перейти до профілю продавця і написати йому в особисті:",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                _logger.LogInformation("не вдалося розпарсити ід продавця");
                await botClient.SendMessage(chatId, "помилка", cancellationToken: cancellationToken);
            }

            return;
        }
        private async Task<ProductSessionState> ProductNameCheck(ITelegramBotClient botClient,
            long chatId,
            string? productName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(productName))
            {
                _logger.LogWarning("Введіть назву продукту");
                await botClient.SendMessage(chatId, "Введіть назву продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForProductName;
            }
            return ProductSessionState.WaitingForPrice;
        }

        private async Task<ProductSessionState> ProductPriceCheck(ITelegramBotClient botClient,
            long chatId,
            decimal? productPrice,
            CancellationToken cancellationToken)
        {
            if (productPrice == null || productPrice < 1)
            {
                _logger.LogWarning("Введіть ціну(в грн)");
                await botClient.SendMessage(chatId, "Введіть ціну", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForPrice;
            }
            return ProductSessionState.WaitingForDescription;
        }

        private async Task<ProductSessionState> ProductDescriptionCheck(ITelegramBotClient botClient,
            long chatId,
            string? productDescription,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(productDescription))
            {
                _logger.LogWarning("Введіть опис продукту");
                await botClient.SendMessage(chatId, "Введіть опис продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForDescription;
            }
            return ProductSessionState.WaitingForMedia;
        }

        private async Task<ProductSessionState> ProductMediaCheck(ITelegramBotClient botClient,
            long chatId,
            List<string?> mediaUrl,
            bool addMoreMedia,
            CancellationToken cancellationToken)
        {
            if (mediaUrl == null || mediaUrl is [])
            {
                _logger.LogWarning("Виберіть нові фото для продукту");
                await botClient.SendMessage(chatId, "Виберіть нові фото для продукту\nПОПЕРЕДЖЕННЯ!\nOбрати можна лише 5 фото/відео", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }
            if (mediaUrl.Count() > 5)
            {
                _logger.LogWarning("Можна додати лише 5 фото/відео");
                await botClient.SendMessage(chatId, "Можна додати лише 5 фото/відео!!!!");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, "Можна додати лише 5 фото/відео!!!!", cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }

            if (addMoreMedia)
            {
                _logger.LogInformation($"Додано {mediaUrl.Count()}/5 фото");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, $"Додано {mediaUrl.Count()}/5 фото", cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }
            return ProductSessionState.Idle;
        }
    }
}
