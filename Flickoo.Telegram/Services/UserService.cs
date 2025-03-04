using FlickooBot;
using Telegram.Bot;

namespace Flickoo.Telegram.Services
{
    class UserService
    {
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBotService> _logger;
        public UserService(ITelegramBotClient telegramBotClient,
            HttpClient httpClient,
            ILogger<TelegramBotService> logger)
        {
            _telegramBotClient = telegramBotClient;
            _httpClient = httpClient;
            _logger = logger;
        }


    }
}
