using Telegram.Bot;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.DTOs;
using System.Net.Http.Json;
using Flickoo.Telegram.Keyboards;
using Telegram.Bot.Types;
using Flickoo.Telegram.SessionModels;

namespace Flickoo.Telegram.Services
{
    public class ProductService : IProductService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;
        private readonly MainKeyboard _mainKeyboard;
        private readonly AddProductCategoryInlineKeyboard _addProductCategoryInlineKeyboard;
        private readonly AddProductMediaKeyboard _addProductMediaKeyboard;
        private readonly ProductInlineKeyboard productInlineKeyboard;

        public ProductService(ITelegramBotClient botClient,
            HttpClient httpClient,
            ILogger<ProductService> logger,
            MainKeyboard mainKeyboard,
            AddProductCategoryInlineKeyboard addProductCategoryInlineKeyboard,
            AddProductMediaKeyboard addProductMediaKeyboard,
            ProductInlineKeyboard productInlineKeyboard)
        {
            _botClient = botClient;
            _httpClient = httpClient;
            _logger = logger;
            _mainKeyboard = mainKeyboard;
            _addProductCategoryInlineKeyboard = addProductCategoryInlineKeyboard;
            _addProductMediaKeyboard = addProductMediaKeyboard;
            this.productInlineKeyboard = productInlineKeyboard;
        }

        public async Task GetProducts(ITelegramBotClient botClient,
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
                    await botClient.SendMessage(chatId, "Ви ще не додали жодного продукту", cancellationToken: cancellationToken);
                    _logger.LogWarning("Користувач ще не додав жодного продукту");
                }
                else
                {
                    foreach (var product in products)
                    {
                        var mediaList = new List<IAlbumInputMedia>();
                        if (products != null)
                        {
                            if (product.MediaUrls == null || product.MediaUrls.Count == 0)
                            {
                                var keyboard = productInlineKeyboard.SendProductButtons(product.Id, cancellationToken);
                                await botClient.SendMessage(chatId, $"{product.Name}\n{product.Price} грн\n{product.Description}", cancellationToken: cancellationToken, replyMarkup: keyboard);
                            }
                            else
                            {

                                foreach (var media in product.MediaUrls)
                                {
                                    if (media != null)
                                    {
                                        mediaList.Add(new InputMediaPhoto(media));
                                    }
                                }
                                if (mediaList.Count > 0)
                                {
                                    await botClient.SendMediaGroup(chatId, mediaList, cancellationToken: cancellationToken);
                                }

                                var keyboard = productInlineKeyboard.SendProductButtons(product.Id, cancellationToken);
                                await botClient.SendMessage(chatId, $"{product.Name}\n{product.Price} грн\n\n{product.Description}", cancellationToken: cancellationToken, replyMarkup: keyboard);
                            }
                        }
                    }
                    _logger.LogInformation("Продукти успішно відправлені");
                }
            }
            else if (productResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await botClient.SendMessage(chatId, "Ви ще не зареєстровані!", cancellationToken: cancellationToken);
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
            long categoryId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            bool addMoreMedia,
            CancellationToken cancellationToken)
        {
            if (categoryId == 0)
            {
                _logger.LogWarning("Виберіть категорію");
                var categoryKeyboard = await _addProductCategoryInlineKeyboard.SendCategoriesInlineButtonsAsync(_httpClient, botClient, chatId, cancellationToken);
                await botClient.SendMessage(chatId, "Виберіть категорію", replyMarkup: categoryKeyboard, cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForCategory;
            }


            if (string.IsNullOrEmpty(productName))
            {
                _logger.LogWarning("Введіть назву продукту");
                await botClient.SendMessage(chatId, "Введіть назву продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForProductName;
            }

            if (productPrice == null || productPrice < 1)
            {
                _logger.LogWarning("Введіть ціну(в грн)");
                await botClient.SendMessage(chatId, "Введіть ціну", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForPrice;
            }

            if (string.IsNullOrEmpty(productDescription))
            {
                _logger.LogWarning("Введіть опис продукту");
                await botClient.SendMessage(chatId, "Введіть опис продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForDescription;
            }

            if (mediaUrl == null || mediaUrl is [])
            {
                _logger.LogWarning("Виберіть фото продукту");
                await botClient.SendMessage(chatId, "Виберіть фото продукту\nПОПЕРЕДЖЕННЯ!\nOбрати можна лише 5 фото/відео", cancellationToken: cancellationToken);
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

            var product = new CreateOrUpdateProductRequest
            {
                MediaUrls = mediaUrl,
                Name = productName,
                Price = productPrice.Value,
                Description = productDescription,
                UserId = chatId,
                CategoryId = categoryId
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
            long productId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            bool addMoreMedia,
            CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                _logger.LogWarning("Не вдалося отримати chatId");
                await _mainKeyboard.SendMainKeyboard(botClient, chatId, "Не вдалося отримати chatId");
                return ProductSessionState.Idle;
            }


            if (string.IsNullOrEmpty(productName))
            {
                _logger.LogWarning("Введіть нову назву продукту");
                await botClient.SendMessage(chatId, "Введіть нову назву продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForProductNameUpdate;
            }

            if (productPrice == null || productPrice < 1)
            {
                _logger.LogWarning("Введіть нову ціну");
                await botClient.SendMessage(chatId, "Введіть нову ціну(в грн)", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForPriceUpdate;
            }

            if (string.IsNullOrEmpty(productDescription))
            {
                _logger.LogWarning("Введіть новий опис продукту");
                await botClient.SendMessage(chatId, "Введіть новий опис продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForDescriptionUpdate;
            }

            if (mediaUrl == null || mediaUrl is [])
            {
                _logger.LogWarning("Виберіть нові фото для продукту");
                await botClient.SendMessage(chatId, "Виберіть нові фото для продукту\nПОПЕРЕДЖЕННЯ!\nOбрати можна лише 5 фото/відео", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForMediaUpdate;
            }
            if (mediaUrl.Count() > 5)
            {
                _logger.LogWarning("Можна додати лише 5 фото/відео");
                await botClient.SendMessage(chatId, "Можна додати лише 5 фото/відео!!!!");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, "Можна додати лише 5 фото/відео!!!!", cancellationToken);
                return ProductSessionState.WaitingForMediaUpdate;
            }

            if (addMoreMedia)
            {
                _logger.LogInformation($"Додано {mediaUrl.Count()}/5 фото");
                await _addProductMediaKeyboard.SendAddProductMediaKeyboard(botClient, chatId, $"Додано {mediaUrl.Count()}/5 фото", cancellationToken);
                return ProductSessionState.WaitingForMediaUpdate;
            }

            var product = new CreateOrUpdateProductRequest
            {
                Name = productName,
                Price = productPrice.Value,
                Description = productDescription,
                UserId = chatId,
                MediaUrls = mediaUrl
            };

            var response = await _httpClient.PutAsJsonAsync($"https://localhost:8443/api/Product/{productId}", product, cancellationToken);
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

        public async Task DeleteProduct(ITelegramBotClient botClient, long chatId, long productId, CancellationToken cancellationToken)
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

        public async Task LikeProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken)
        {
            if (productId == 0)
            {
                _logger.LogWarning("Виберіть продукт для лайку");
                await botClient.SendMessage(chatId, "Виберіть продукт для лайку", cancellationToken: cancellationToken);
                return;
            }
            var response = await _httpClient.PostAsync($"https://localhost:8443/api/Product/like/{productId}/user/{chatId}", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await botClient.SendMessage(chatId, "Ви лайкнули продукт", cancellationToken: cancellationToken);
                _logger.LogInformation("Продукт лайкнуто");
            }
            else
            {
                await botClient.SendMessage(chatId, "Помилка при лайку продукту", cancellationToken: cancellationToken);
                _logger.LogError("Помилка при лайку продукту");
            }
        }

        public async Task DislikeProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public async Task<Queue<GetProductResponse>> GetLikedProducts(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"https://localhost:8443/api/Product/liked/{chatId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<Queue<GetProductResponse>>(cancellationToken: cancellationToken);
                if (products == null || products.Count() == 0)
                {
                    await botClient.SendMessage(chatId, "Ви ще не лайкнули жодного продукту", cancellationToken: cancellationToken);
                    _logger.LogWarning("Користувач ще не лайкнув жодного продукту");
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
    }
}
