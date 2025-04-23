using Flickoo.Telegram.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Flickoo.Telegram
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IUserService _userService;
        private readonly IUserSessionService _userSessionService;
        private readonly IProductSessionService _productSessionService;
        private readonly IKeyboards _keyboards;

        public TelegramBotService(
            ITelegramBotClient botClient,
            ILogger<TelegramBotService> logger,
            IUserService userService,
            IUserSessionService userSessionService,
            IProductSessionService productSessionService,
            IKeyboards keyboards)
        {
            _botClient = botClient;
            _logger = logger;
            _userService = userService;
            _userSessionService = userSessionService;
            _productSessionService = productSessionService;
            _keyboards = keyboards;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var me = await _botClient.GetMe(cancellationToken: cancellationToken);
            _logger.LogInformation($"Бот {me.FirstName} запущено!");

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                cancellationToken: cts.Token
            );

            await Task.Delay(Timeout.Infinite, cancellationToken);
            await cts.CancelAsync();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            await (update switch
            {
                { Message: { } message } => OnMessage(botClient, message, cancellationToken),
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(botClient, callbackQuery, cancellationToken),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            });
        }

        private async Task OnMessage(ITelegramBotClient botClient,
            Message msg,
            CancellationToken cancellationToken)
        {
            var chatId = msg.Chat.Id;
            var userName = msg.From?.Username ?? "Unknown";

            if (msg.Photo != null)
                _logger.LogInformation($"Отримано фото | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");

            else if (msg.Type ==  MessageType.Text)
                _logger.LogInformation($"Отримано повiдомлення: {msg.Text} | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");

            else if (string.IsNullOrEmpty(msg.Text) && (msg.Type != MessageType.Photo || msg.Type != MessageType.Video))
            {
                _logger.LogWarning("Повідомлення не може бути пустим.");
                await botClient.SendMessage(chatId, "Повідомлення не може бути пустим.", cancellationToken: cancellationToken);
                return;
            }

            if (await HandleBaseCommand(botClient, chatId, userName, msg, cancellationToken))
                return;

            if (await _userSessionService.UserSessionCheck(botClient, chatId, userName, msg, cancellationToken))
                return;
            
            if (await _productSessionService.ProductSessionCheck(botClient, chatId, msg, cancellationToken))
                return;

            _userSessionService.ResetSession(chatId);
            _productSessionService.ResetSession(chatId);
            await _keyboards.SendMainKeyboard(botClient, chatId, "Вибери потрібну команду на панелі", cancellationToken);


        }

        private async Task<bool> HandleBaseCommand(ITelegramBotClient botClient,
            long chatId,
            string userName,
            Message command,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Text))
            {
                _logger.LogWarning("Пуста команда.");
                return false;
            }

            if (command.From == null)
            {
                _logger.LogWarning("Не вдалося отримати дані користувача.");
                return false;
            }

            switch (command.Text.ToLower())
            {
                case "/start":
                    await _keyboards.SendMainKeyboard(botClient, chatId, "Привіт 👋😊\n" +
                        "Якщо потрібна допомога, то в меню є команда - > /help\n" +
                        "Приємного користування 👋", cancellationToken);
                    await _userService.AddUnRegisteredUser(botClient, chatId, userName, cancellationToken);
                    return true;

                case "/help":

                    await _keyboards.SendMainKeyboard(botClient, chatId, "Нажаль допомога поки не працює\n" +
                        "Думаю у вас не буде проблем з ботом", cancellationToken);
                    return true;

                case "/language":
                    await _keyboards.SendMainKeyboard(botClient, chatId, "Додаток підтримує лише українську мову", cancellationToken);
                    return true;

                case "назад":
                    _userSessionService.ResetSession(chatId);
                    _productSessionService.ResetSession(chatId);
                    await _keyboards.SendMainKeyboard(botClient, chatId, "Дію скасовано", cancellationToken);
                    return true;

                default:
                    return false;
            }
        }
        
        private async Task OnCallbackQuery(ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            CancellationToken cancellationToken)
        {
            if (callbackQuery.Message == null)
            {
                _logger.LogWarning("CallbackQuery.Message не може бути пустим.");
                return;
            }

            var chatId = callbackQuery.Message.Chat.Id;
            var userName = callbackQuery.From.Username ?? "Unknown";
            _logger.LogInformation($"Отримано callbackQuery: {callbackQuery.Data} | ChatId: {chatId} | UserName: {userName} | Time: {DateTime.UtcNow}");


            if (await _productSessionService.ProductCallback(botClient, callbackQuery, chatId, cancellationToken))
                return;

            _userSessionService.ResetSession(chatId);
            _productSessionService.ResetSession(chatId);
            await _keyboards.SendMainKeyboard(botClient, chatId, "Вибери потрібну команду на панелі", cancellationToken);
        }

        private async Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            _logger.LogInformation($"Unknown update type: {update.Type}");
            if (update.Message != null)
                await botClient.SendMessage(update.Message.Chat.Id, "Unknown update type");
        }

        private async Task HandleErrorAsync(ITelegramBotClient botClient,
            Exception exception,
            HandleErrorSource source,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("HandleError: {Exception}", exception);
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}