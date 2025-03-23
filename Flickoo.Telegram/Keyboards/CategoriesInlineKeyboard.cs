using Flickoo.Telegram.DTOs;
using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Keyboards
{
    public class CategoriesInlineKeyboard
    {
        private readonly HttpClient _httpClient;
        public CategoriesInlineKeyboard(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<InlineKeyboardMarkup> SendInlineButtonsAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            List<CategoryDto> categories = new();
            var response = await _httpClient.GetAsync("https://localhost:8443/api/Product/category");
            if (response.IsSuccessStatusCode)
            {
                categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(cancellationToken: cancellationToken) ?? [];
            }
            else
            {
                await botClient.SendMessage(chatId, "не вдалося отримати категорії", cancellationToken: cancellationToken);
                throw new Exception("Failed to get categories");
            }

            List<InlineKeyboardButton[]> keyboardButtons = categories
                .Select(category => new[]
                {
                    InlineKeyboardButton.WithCallbackData(category.Name, category.Id.ToString())
                })
                .ToList();

            keyboardButtons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Всі оголошення", "0")
            });

            return new InlineKeyboardMarkup(keyboardButtons);
        }
    }
}
