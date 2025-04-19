namespace Flickoo.Api.DTOs.Media.Update
{
    public class UpdateMediaRequest
    {
        public long ProductId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = Stream.Null;
    }
}
