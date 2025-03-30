using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.Interfaces;
using System.Net.Http.Json;
using Telegram.Bot;

namespace Flickoo.Telegram.Services
{
    class FavouriteService : IFavouriteService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FavouriteService> _logger;
        public FavouriteService(HttpClient httpClient,
            ILogger<FavouriteService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task AddToFavouriteProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken)
        {
            if (productId == 0)
            {
                _logger.LogWarning($"User with chatId {chatId} use product with Id == 0, When user like product(AddToFavouriteProduct");
                await botClient.SendMessage(chatId, "Виберіть продукт для лайку", cancellationToken: cancellationToken);
                return;
            }
            var response = await _httpClient.PostAsync($"https://localhost:8443/api/Product/like/{productId}/user/{chatId}", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await botClient.SendMessage(chatId, "❤️", cancellationToken: cancellationToken);
                _logger.LogInformation($"Product with id: {productId}, was liked by userId: {chatId}");
            }
            else
            {
                await botClient.SendMessage(chatId, "Помилка при лайку продукту", cancellationToken: cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Error while liking product: {responseContent}");
            }
        }

        public async Task DislikeProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken)
        {
            if (chatId == 0)
                return;

            if (productId == 0)
                return;

            var response = await _httpClient.DeleteAsync($"https://localhost:8443/api/Product/{productId}/user/{chatId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await botClient.SendMessage(chatId, "👎", cancellationToken: cancellationToken);
                _logger.LogInformation($"product is disliked");
            }
            else
                _logger.LogWarning("product not disliked");
        }

        public async Task<Queue<GetProductResponse>> GetFavouriteProducts(ITelegramBotClient botClient,
            long chatId,
            string filter,
            CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"https://localhost:8443/api/Product/liked/{chatId}/{filter}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<Queue<GetProductResponse>>(cancellationToken: cancellationToken);
                if (products == null || products.Count() == 0)
                {
                    await botClient.SendMessage(chatId, "Кінець списку", cancellationToken: cancellationToken);
                    _logger.LogWarning("Лайкнуті продукти закінчилися");
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
