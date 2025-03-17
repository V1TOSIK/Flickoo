using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Keyboards
{
    public class AddProductMediaKeyboard
    {
        public async Task SendAddProductMediaKeyboard(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("надіслати фото заново"),
                new KeyboardButton("готово"),
                new KeyboardButton("назад")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, text, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }   
    }
}
