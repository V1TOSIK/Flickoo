using Flickoo.Telegram;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Services;
using Microsoft.Extensions.Options;
using Telegram.Bot;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<TelegramOptions>(context.Configuration.GetSection(nameof(Telegram)));

        services.AddHttpClient("TelegramBotClient").RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramOptions? telegramOptions = sp.GetService<IOptions<TelegramOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(telegramOptions);
                TelegramBotClientOptions options = new(telegramOptions.Token);
                return new TelegramBotClient(options, httpClient);
            });
        services.AddHostedService<TelegramBotService>();
        services.AddSingleton<IUserService, UserService>();
    })
    .Build();

host.Run();
