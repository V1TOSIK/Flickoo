using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class FlickooBot
{
    private static string _token = "8183469904:AAEu35s0wV4gDPl8kYNf6byCCKVbq-TRINI";
    private static ITelegramBotClient? _botClient;
    private static HttpClient _httpClient = new HttpClient();

    static async Task Main()
    {
        _botClient = new TelegramBotClient(_token);
        var me = await _botClient.GetMe();
        Console.WriteLine($"Бот {me.FirstName} запущено!");

        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            cancellationToken: cts.Token
        );

        Console.ReadLine();
        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Text is not null)
        {
            var message = update.Message;

            var userId = message.From?.Id ?? 0;
            Console.WriteLine($"Отримано повiдомлення: {message.Text} | ChatId: {message.Chat.Id} | UserName: {message.Chat.Username} | UserId: {userId} | Time: {DateTime.UtcNow}");

            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Привіт! Я Telegram-бот на C#."
                );
            }

            if (message.Text.ToLower() == "/adduser")
            {
                var user = new
                {
                    id = message.From.Id,
                    Username = message.Chat.Username,
                    PhoneNumber = "+380663081621"
                };
                Console.WriteLine($"{user.id}, {user.Username}, {user.PhoneNumber}");
                if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await botClient.SendMessage(message.Chat.Id, "Необхідно вказати ім'я користувача та номер телефону.");
                    return;
                }
                var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/User", user);

                if (response.IsSuccessStatusCode)
                {
                    await botClient.SendMessage(message.Chat.Id, "Користувача успішно додано!");
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {errorDetails}");
                    await botClient.SendMessage(message.Chat.Id, "Помилка при додаванні користувача.");
                }
            }

            if (message.Text.ToLower() == "/addproduct")
            {
                var category = 1; // замініть на реальний ID категорії
                // Відправка POST-запиту для додавання продукту
                var product = new
                {
                    Name = "Motorcycle",
                    Price = 15000.0m,
                    Description = "A great motorcycle",
                    UserId = message.From.Id,
                    CategoryId = category // замініть на реальний ID категорії
                };

                var response = await _httpClient.PostAsJsonAsync("https://localhost:8443/api/Product", product);

                if (response.IsSuccessStatusCode)
                {
                    await botClient.SendMessage(message.Chat.Id, "Продукт успішно додано!");
                    Console.WriteLine("Продукт успішно додано!");
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {errorDetails}");
                    await botClient.SendMessage(message.Chat.Id, "Помилка при додаванні продукту.");
                    Console.WriteLine("Помилка при додаванні продукту.");
                }
            }
        }

    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Помилка: {exception.Message}");
        return Task.CompletedTask;
    }
}
