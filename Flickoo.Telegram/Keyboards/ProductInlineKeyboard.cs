using Flickoo.Telegram.DTOs;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Keyboards
{
    public class ProductInlineKeyboard
    {
        public InlineKeyboardMarkup SendProductButtons(long id, CancellationToken cancellationToken)
        {
            var keyboard = new InlineKeyboardMarkup()
            {
                InlineKeyboard =
                [
                    [
                        new InlineKeyboardButton
                        {
                            Text = "Оновити товар",
                            CallbackData = $"update_{id}"
                        },
                        new InlineKeyboardButton
                        {
                            Text = "Видалити товар",
                            CallbackData = $"delete_{id}"
                        }
                    ]
                ]
            };
            
            return keyboard;
        }
    }
}
