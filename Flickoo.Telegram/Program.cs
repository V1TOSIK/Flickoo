using Flickoo.Telegram;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Services;
using Microsoft.Extensions.Options;
using Telegram.Bot;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<TelegramOptions>(context.Configuration.GetSection(nameof(Telegram)));
        services.Configure<ApiOptions>(context.Configuration.GetSection(ApiOptions.Api));

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
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IMediaService, MediaService>();
        services.AddSingleton<IFavouriteService, FavouriteService>();
        services.AddSingleton<IUserSessionService, UserSessionService>();
        services.AddSingleton<IProductSessionService, ProductSessionService>();
        services.AddSingleton<IKeyboards, MyKeyboards>();
    })
    .Build();

host.Run();