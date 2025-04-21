public class TelegramOptions
{
    public const string Telegram = nameof(Telegram);

    public string Token { get; init; } = default!;

    public string apiUrl { get; set; } = string.Empty;
}