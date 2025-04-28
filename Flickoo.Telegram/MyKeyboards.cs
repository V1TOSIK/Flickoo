using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Flickoo.Telegram.Interfaces;
using System.Net.Http.Json;
using Flickoo.Telegram.DTOs.User;
using Flickoo.Telegram.DTOs.Category;

namespace Flickoo.Telegram
{
    class MyKeyboards : IKeyboards
    {
        private readonly ILogger<MyKeyboards> _logger;
        private readonly HttpClient _httpClient;
        public MyKeyboards(ILogger<MyKeyboards> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }
        public async Task SendMainKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (chatId == 0)
            {
                _logger.LogError("Не вдалося отримати chatId");
                return;
            }

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
            new KeyboardButton("👤"),
            new KeyboardButton("📢"),
            new KeyboardButton("⭐"),
            new KeyboardButton("🚀")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        public async Task SendAddProductKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
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
            await botClient.SendMessage(chatId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        public async Task SendReductProductButtons(ITelegramBotClient botClient, long chatId, long productId, string messageText, CancellationToken cancellationToken)
        {
            var keyboard = new InlineKeyboardMarkup()
            {
                InlineKeyboard =
                [
                    [
                        new InlineKeyboardButton
                        {
                            Text = "✏️ Редагувати",
                            CallbackData = $"update_{productId}"
                        },
                        new InlineKeyboardButton
                        {
                            Text = "🗑️ Видалити",
                            CallbackData = $"delete_{productId}"
                        }
                    ]
                ]
            };
            await botClient.SendMessage(chatId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        public async Task SendLikeFilterButtons(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
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

            await botClient.SendMessage(chatId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        public async Task SendMediaKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
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
            await botClient.SendMessage(chatId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        public async Task SendCategoriesInlineButtons(ITelegramBotClient botClient, long chatId, string messageText, bool withAllCategoryButton, CancellationToken cancellationToken)
        {
            List<CategoryDto> categories = [];
            var response = await _httpClient.GetAsync("https://localhost:8443/api/Category", cancellationToken);
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
            if (withAllCategoryButton)
            {
                keyboardButtons.Add(
                [
                    InlineKeyboardButton.WithCallbackData("Всі категорії", "0")
                ]);
            }
            await botClient.SendMessage(chatId, messageText, replyMarkup: new InlineKeyboardMarkup(keyboardButtons), cancellationToken: cancellationToken);
        }

        public async Task SendMyProfileKeyboard(ITelegramBotClient botClient, long chatId, GetUserResponse user, string messageText, CancellationToken cancellationToken)
        {
            var profileKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("оновити дані"),
                new KeyboardButton("назад")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, messageText, replyMarkup: profileKeyboard, cancellationToken: cancellationToken);
        }

        public async Task SendMyProfileRegKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            var registrationKeyboard = new ReplyKeyboardMarkup(new[]
            {
                    new KeyboardButton("створити акаунт"),
                    new KeyboardButton("назад")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, messageText, replyMarkup: registrationKeyboard, cancellationToken: cancellationToken);
        }

        public async Task SendCancelKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            var cancelKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("назад")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, messageText, replyMarkup: cancelKeyboard, cancellationToken: cancellationToken);
        }

        public async Task SendCurrencyKeyboard(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            var currencyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("₴"),
                new KeyboardButton("$"),
                new KeyboardButton("€"),
                new KeyboardButton("назад")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            await botClient.SendMessage(chatId, messageText, replyMarkup: currencyKeyboard, cancellationToken: cancellationToken);
        }
    }
}

