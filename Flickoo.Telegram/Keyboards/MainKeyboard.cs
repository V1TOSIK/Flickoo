using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flickoo.Telegram.Keyboards;

public class MainKeyboard(ILogger<TelegramBotService> logger)
{
    public async Task SendMainKeyboard(ITelegramBotClient botClient, long chatId, string text)
    {
        if (chatId == 0)
        {
            logger.LogError("Не вдалося отримати chatId");
            return;
        }

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("мій профіль"),
            new KeyboardButton("мої оголошення"),
            new KeyboardButton("мої лайки"),
            new KeyboardButton("почати")

        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
        await botClient.SendMessage(chatId, text, replyMarkup: keyboard);
    }
}