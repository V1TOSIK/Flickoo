using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;

namespace Flickoo.Telegram.Keyboards
{
    public class MyProductKeyboard
    {
        public async Task SendMyProductKeyboard(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("Додати продукт"),
                new KeyboardButton("Назад")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, text, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }
    }
}
