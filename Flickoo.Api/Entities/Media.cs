using Flickoo.Api.enums;

namespace Flickoo.Api.Entities
{
    public class Media
    {
        public long Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public MediaType TypeOfMedia { get; set; }

        public long ProductId { get; set; }
        public required Product Product { get; set; }
    }
}
