using Flickoo.Api.enums;

namespace Flickoo.Api.DTOs
{
    public class MediaDto
    {
        public string Url { get; set; } = string.Empty;
        public MediaType TypeOfMedia { get; set; } = MediaType.unknown;
        public long ProductId { get; set; }
    }
}
