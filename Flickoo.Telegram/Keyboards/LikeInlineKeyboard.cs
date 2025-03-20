using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;

namespace Flickoo.Telegram.Keyboards
{
    public class LikeInlineKeyboard
    {
        public InlineKeyboardMarkup SendLikeInlineButtonsAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var keyboard = new InlineKeyboardMarkup()
            {
                InlineKeyboard =
               [
                   [
                        new InlineKeyboardButton
                        {
                            Text = "Спочатку нові",
                            CallbackData = "first_new"
                        },
                        new InlineKeyboardButton
                        {
                            Text = "Спочатку старі",
                            CallbackData = "first_old"
                        }
                    ]
               ]
            };

            return keyboard;
        }
    }
}
