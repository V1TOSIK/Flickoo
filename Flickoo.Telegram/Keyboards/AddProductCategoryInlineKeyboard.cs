using Flickoo.Telegram.DTOs;
using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Keyboards
{
    public class AddProductCategoryInlineKeyboard
    {
        public async Task<InlineKeyboardMarkup> SendCategoriesInlineButtonsAsync(HttpClient _httpClient, ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            List<Category> categories = new();
            var response = await _httpClient.GetAsync("https://localhost:8443/api/Product/category");
            if (response.IsSuccessStatusCode)
            {
                categories = await response.Content.ReadFromJsonAsync<List<Category>>(cancellationToken: cancellationToken) ?? [];
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

            return new InlineKeyboardMarkup(keyboardButtons);
        }
    }
}
