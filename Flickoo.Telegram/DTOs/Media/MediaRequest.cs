namespace Flickoo.Telegram.DTOs.Media
{
    public class MediaRequest
    {
        public long ProductId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = Stream.Null;
    }
}
