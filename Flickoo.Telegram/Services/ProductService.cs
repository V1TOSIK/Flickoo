using Telegram.Bot;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.enums;
using System.Net.Http.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Flickoo.Telegram.SessionModels;
using Flickoo.Telegram.DTOs.Product;
using Flickoo.Telegram.DTOs.Media;
using Flickoo.Telegram.DTOs.User;
using Microsoft.Extensions.Options;

namespace Flickoo.Telegram.Services
{
    public class ProductService : IProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;
        private readonly IMediaService _mediaService;
        private readonly IKeyboards _keyboards;
        private readonly string _apiUrl;

        public ProductService(
            IMediaService mediaService,
            HttpClient httpClient,
            ILogger<ProductService> logger,
            IKeyboards keyboards,
            IOptions<ApiOptions> apiOptions)
        {
            _mediaService = mediaService;
            _httpClient = httpClient;
            _logger = logger;
            _keyboards = keyboards;
            _apiUrl = apiOptions.Value.Url;
        }

        public async Task GetUserProducts(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                throw new ArgumentNullException(nameof(chatId));
            }

            var productResponse = await _httpClient.GetAsync($"{_apiUrl}/api/Product/myproducts/{chatId}", cancellationToken);

            if (productResponse.IsSuccessStatusCode)
            {
                var products = await productResponse.Content.ReadFromJsonAsync<List<GetProductResponse>>(cancellationToken: cancellationToken);

                if (products == null || products.Count == 0)
                {
                    await _keyboards.SendAddProductKeyboard(botClient, chatId, "Ви ще не додали жодного продукту", cancellationToken);
                    _logger.LogWarning("Користувач ще не додав жодного продукту");

                }
                else
                {
                    foreach (var product in products)
                    {
                        if (products != null)
                        {
                            var productMedias = await _mediaService.GetMediaFromUrlsByProductIdAsync(botClient, product.Id, cancellationToken);
                            if (productMedias == null || productMedias.Count == 0)
                            {
                                await _keyboards.SendReductProductButtons(botClient,
                                    chatId,
                                    product.Id,
                                    $"📢 {product.Name}\n" +
                                    $"💰 {product.PriceAmount}  {product.PriceCurrency}\n" +
                                    $"📍 {product.LocationName}\n" +
                                    $"──────────────────────────\n" +
                                    $"📜 Опис: {product.Description}",
                                    cancellationToken);
                            }
                            else
                            {
                                await botClient.SendMediaGroup(chatId, productMedias, cancellationToken: cancellationToken);

                                await _keyboards.SendReductProductButtons(botClient,
                                    chatId,
                                    product.Id,
                                    $"📢 {product.Name}\n" +
                                    $"💰 {product.PriceAmount}  {product.PriceCurrency}\n" +
                                    $"📍 {product.LocationName}\n" +
                                    $"──────────────────────────\n" +
                                    $"📜 Опис: {product.Description}",
                                    cancellationToken);
                            }
                        }
                    }
                    await _keyboards.SendAddProductKeyboard(botClient, chatId, "Ось ваші товари", cancellationToken);
                    _logger.LogInformation("Продукти успішно відправлені");
                }
            }
            else if (productResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Ви ще не зареєстровані", cancellationToken);
                _logger.LogWarning("Користувач не зареєстрований!");

            }
            else
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при отриманні продуктів", cancellationToken);
                _logger.LogError("Помилка при отриманні продуктів");
            }

        }

        public async Task<Queue<GetProductResponse>> GetProductsForSwaping(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            CancellationToken cancellationToken)
        {

            var response = await _httpClient.GetAsync($"{_apiUrl}/api/Product/category/{categoryId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<Queue<GetProductResponse>>(cancellationToken: cancellationToken);
                if (products == null || products.Count == 0)
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

        public async Task<ProductSessionState> AddProductAsync(ITelegramBotClient botClient,
            long chatId,
            ProductSession session,
            CancellationToken cancellationToken)
        {
            if (!await ProductCheck(botClient, chatId, session, "Add", cancellationToken))
                return session.State;

            var product = new CreateProductRequest
            {
                Name = session.Name,
                PriceAmount = session.PriceAmount,
                PriceCurrency = session.PriceCurrency,
                Description = session.ProductDescription,
                UserId = chatId,
                CategoryId = session.CategoryId
            };

            var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/api/Product", product, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Продукт успішно додано", cancellationToken);
                _logger.LogInformation("Продукт успішно додано");
            }
            else
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при додаванні продукту", cancellationToken);
                _logger.LogError("Помилка при додаванні продукту");
            }
            var productId = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!long.TryParse(productId, out long productIdParsed))
            {
                _logger.LogError("Не вдалося отримати ID продукту");
                await _keyboards.SendMainKeyboard(botClient, chatId, "Не вдалося отримати ID продукту", cancellationToken);
                return ProductSessionState.Idle;
            }
            if (session.MediaFiles != null && session.MediaFiles.Count > 0)
            {
                var mediaRequests = new List<MediaRequest>();
                for (int i = 0; i < session.MediaFiles.Count; i++)
                {
                    var mediaFile = session.MediaFiles[i];
                    if (mediaFile.CanSeek)
                        mediaFile.Position = 0;

                    var mediaRequest = new MediaRequest
                    {
                        ProductId = productIdParsed,
                        FileName = $"{chatId}_{productIdParsed}_{i + 1}.{session.MediaTypes[i]}",
                        ContentType = session.MediaTypes[i],
                        FileStream = mediaFile
                    };
                    mediaRequests.Add(mediaRequest);
                }

                foreach (var mediaRequest in mediaRequests)
                {
                    var res = await _mediaService.UploadMediaAsync(botClient, mediaRequest, productIdParsed, cancellationToken);

                    if (res)
                    {
                        _logger.LogInformation("Медіа успішно додано");
                    }
                    else
                    {
                        _logger.LogError("Помилка при додаванні медіа");
                        await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при додаванні медіа", cancellationToken);
                    }
                }
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
                await _keyboards.SendMainKeyboard(botClient, chatId, "Не вдалося вибрати категорію", cancellationToken);
                return ProductSessionState.Idle;
            }

            if (!await ProductCheck(botClient, chatId, session, "Update", cancellationToken))
                return session.State;

            var product = new UpdateProductRequest
            {
                Name = session.Name,
                PriceAmount = session.PriceAmount,
                PriceCurrency = session.PriceCurrency,
                Description = session.ProductDescription,
                UserId = chatId
            };

            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/api/Product/{session.ProductId}", product, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Продукт успішно оновлено", cancellationToken);
                _logger.LogInformation("Продукт успішно оновлено");
                var mediaResponse = await _httpClient.DeleteAsync($"{_apiUrl}/api/Media/{session.ProductId}", cancellationToken);
                if (mediaResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Медіа успішно видалено");
                }
                else
                {
                    _logger.LogError("Помилка при видалені медіа для оновлення");
                    await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при оновлені медіа", cancellationToken);
                }
            }
            else
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при оновлені продукту", cancellationToken);
                _logger.LogError("Помилка при оновлені продукту");
            }

            if (session.MediaFiles != null && session.MediaFiles.Count > 0)
            {
                var mediaRequests = new List<MediaRequest>();
                for (int i = 0; i < session.MediaFiles.Count; i++)
                {
                    var mediaFile = session.MediaFiles[i];
                    if (mediaFile.CanSeek)
                        mediaFile.Position = 0;

                    var mediaRequest = new MediaRequest
                    {
                        ProductId = session.ProductId,
                        FileName = $"{chatId}_{session.ProductId}_{i + 1}.{session.MediaTypes[i]}",
                        ContentType = session.MediaTypes[i],
                        FileStream = mediaFile
                    };
                    mediaRequests.Add(mediaRequest);
                }

                foreach (var mediaRequest in mediaRequests)
                {
                    var res = await _mediaService.UploadMediaAsync(botClient, mediaRequest, session.ProductId, cancellationToken);

                    if (res)
                    {
                        _logger.LogInformation("Медіа успішно оновлено");
                    }
                    else
                    {
                        _logger.LogError("Помилка при оновлені медіа");
                        await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при оновлені медіа", cancellationToken);
                    }
                }
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
            var response = await _httpClient.DeleteAsync($"{_apiUrl}/api/Product/{productId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Продукт успішно видалено", cancellationToken);
                _logger.LogInformation("Продукт успішно видалено");
            }
            else
            {
                await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при видаленні продукту", cancellationToken);
                _logger.LogError("Помилка при видаленні продукту");
            }

        }

        public async Task WriteToSeller(ITelegramBotClient botClient,
            long chatId,
            long productId,
            string userName,
            CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetFromJsonAsync<GetSellerResponse>($"{_apiUrl}/api/Product/{productId}/seller", cancellationToken);

            if (response != null)
            {
                    if (response.Id == chatId)
                    {
                        await botClient.SendMessage(chatId,
                            "Не можна писати самому собі",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (string.IsNullOrEmpty(response.Username))
                    {
                        await botClient.SendMessage(chatId,
                            "Продавець не має публічного username, тому неможливо відкрити прямий чат.",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    string directChatLink = $"https://t.me/{response.Username}";

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithUrl("Написати продавцю", directChatLink)
                    });

                    await botClient.SendMessage(chatId,
                        "Натисніть кнопку нижче, щоб перейти до профілю продавця і написати йому в особисті:",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: cancellationToken);
            }

            return;
        }

        private async Task<bool> ProductCheck(ITelegramBotClient botClient,
            long chatId,
            ProductSession session,
            string checkType,
            CancellationToken cancellationToken)
        {
            string checkTypeText = string.Empty;

            if (checkType == "Add")
            {
                if (session.CategoryId == 0)
                {
                    _logger.LogWarning("Виберіть категорію");
                    await _keyboards.SendCategoriesInlineButtons(botClient, chatId, "Виберіть категорію", false, cancellationToken);
                    session.State = ProductSessionState.WaitingForCategory;
                    return false;
                }
            }
            else if (checkType == "Update")
            {
                if (session.ProductId == 0)
                {
                    _logger.LogWarning("Помилка при редагуванні");
                    await _keyboards.SendMainKeyboard(botClient, chatId, "Помилка при редагуванні", cancellationToken: cancellationToken);
                    return false;
                }
                checkTypeText = "(new)";
            }

            if (string.IsNullOrEmpty(session.Name))
            {
                _logger.LogWarning($"Введіть назву продукту {checkTypeText}");
                await _keyboards.SendCancelKeyboard(botClient, chatId, $"Введіть назву продукту {checkTypeText}", cancellationToken: cancellationToken);
                session.State = ProductSessionState.WaitingForProductName;
                return false;
            }

            if(session.PriceCurrency != "₴" && session.PriceCurrency != "$" && session.PriceCurrency != "€")
            {
                _logger.LogWarning($"Виберіть валюту {checkTypeText}");
                await _keyboards.SendCurrencyKeyboard(botClient, chatId, $"Виберіть валюту\n₴ (грн)\n$ (долар)\n€ (євро) {checkTypeText}", cancellationToken: cancellationToken);
                session.State = ProductSessionState.WaitingForPriceCurrency;
                return false;
            }

            if (session.PriceAmount < 1)
            {
                _logger.LogWarning($"Введіть ціну {checkTypeText}");
                await _keyboards.SendCancelKeyboard(botClient, chatId, $"Введіть ціну {checkTypeText}", cancellationToken: cancellationToken);
                session.State = ProductSessionState.WaitingForPriceAmount;
                return false;
            }

            if (string.IsNullOrEmpty(session.ProductDescription))
            {
                _logger.LogWarning($"Введіть опис продукту {checkTypeText}");
                await _keyboards.SendCancelKeyboard(botClient, chatId, $"Введіть новий опис продукту", cancellationToken: cancellationToken);
                session.State = ProductSessionState.WaitingForDescription;
                return false;
            }

            if (session.MediaFiles == null || session.MediaFiles is [])
            {
                _logger.LogWarning($"Виберіть фото продукту {checkTypeText}");
                await _keyboards.SendMediaKeyboard(botClient, chatId, $"Виберіть фото продукту {checkTypeText}\nПОПЕРЕДЖЕННЯ!\nOбрати можна лише 5 фото/відео", cancellationToken: cancellationToken);
                session.State = ProductSessionState.WaitingForMedia;
                return false;
            }
            if (session.MediaFiles.Count > 5)
            {
                _logger.LogWarning("Можна додати лише 5 фото/відео");
                await _keyboards.SendMediaKeyboard(botClient, chatId, "Можна додати лише 5 фото/відео!!!!", cancellationToken);
                session.State = ProductSessionState.WaitingForMedia;
                return false;
            }

            if (session.AddMoreMedia)
            {
                _logger.LogInformation($"Додано {session.MediaFiles.Count}/5 фото");
                await _keyboards.SendMediaKeyboard(botClient, chatId, $"Додано {session.MediaFiles.Count}/5 фото", cancellationToken);
                session.State = ProductSessionState.WaitingForMedia;
                return false;
            }
            return true;
        }
    }
}
