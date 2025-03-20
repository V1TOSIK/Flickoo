using Flickoo.Telegram;
using Flickoo.Telegram.Interfaces;
using Flickoo.Telegram.Keyboards;
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
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IMediaService, MediaService>();
        services.AddTransient<MainKeyboard>();
        services.AddTransient<AddProductCategoryInlineKeyboard>();
        services.AddTransient<MyProductKeyboard>();
        services.AddTransient<AddProductMediaKeyboard>();
        services.AddTransient<ProductInlineKeyboard>();
        services.AddTransient<CategoriesInlineKeyboard>();
        services.AddTransient<LikeInlineKeyboard>();
    })
    .Build();

host.Run();