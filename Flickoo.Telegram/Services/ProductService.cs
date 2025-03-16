using Telegram.Bot;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.DTOs;
using System.Net.Http.Json;
using Flickoo.Telegram.Keyboards;
using Telegram.Bot.Types;

namespace Flickoo.Telegram.Services
{
    public class ProductService : IProductService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;
        private readonly MainKeyboard _mainKeyboard;
        private readonly AddProductCategoryInlineKeyboard _addProductCategoryInlineKeyboard;

        public ProductService(ITelegramBotClient botClient,
            HttpClient httpClient,
            ILogger<ProductService> logger,
            MainKeyboard mainKeyboard,
            AddProductCategoryInlineKeyboard addProductCategoryInlineKeyboard)
        {
            _botClient = botClient;
            _httpClient = httpClient;
            _logger = logger;
            _mainKeyboard = mainKeyboard;
            _addProductCategoryInlineKeyboard = addProductCategoryInlineKeyboard;
        }

        public async Task GetProducts(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
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
                            if (product.MediaUrl == null || product.MediaUrl.Count() == 0)
                            {
                                await botClient.SendMessage(chatId, $"{product.Name}\n{product.Price}\n{product.Description}", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                
                                foreach (var media in product.MediaUrl)
                                {
                                    if (media != null)
                                    {
                                        if (media.EndsWith(".mp4"))
                                            mediaList.Add(new InputMediaPhoto(media));

                                        else if (media.EndsWith(".jpg") || media.EndsWith(".png"))
                                            mediaList.Add(new InputMediaPhoto(media));

                                        else
                                        {
                                            await botClient.SendMessage(chatId, "Невідомий формат файлу", cancellationToken: cancellationToken);
                                            _logger.LogWarning("Невідомий формат файлу");
                                        }
                                    }
                                    await botClient.SendMediaGroup(chatId, mediaList, cancellationToken: cancellationToken);
                                    await botClient.SendMessage(chatId, $"{product.Name}\n{product.Price}\n{product.Description}", cancellationToken: cancellationToken);
                                }

                            }
                        }
                        _logger.LogInformation("Продукти успішно відправлені");
                    }
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

        public async Task<ProductSessionState> AddProduct(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            CancellationToken cancellationToken)
        {
            if(categoryId  == 0)
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
                _logger.LogWarning("Введіть ціну");
                await botClient.SendMessage(chatId, "Введіть ціну", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForPrice;
            }

            if (string.IsNullOrEmpty(productDescription))
            {
                _logger.LogWarning("Введіть опис продукту");
                await botClient.SendMessage(chatId, "Введіть опис продукту", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForDescription;
            }

            if (mediaUrl == null || mediaUrl.Count() > 5)
            {
                _logger.LogWarning("Виберіть фото продукту");
                await botClient.SendMessage(chatId, "Виберіть фото продукту\nПОПЕРЕДЖЕННЯ!\nOбрати можна лише 5 фото/відео", cancellationToken: cancellationToken);
                return ProductSessionState.WaitingForMedia;
            }
            var product = new CreateOrUpdateProductRequest
            {
                MediaUrl = mediaUrl,
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

        public async Task UpdateProduct(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
     
        public async Task DeleteProduct(ITelegramBotClient botClient, long chatId, string productName, CancellationToken cancellationToken)
        {

        }
    }
}
