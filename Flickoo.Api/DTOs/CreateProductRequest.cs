namespace Flickoo.Api.DTOs
{
    public class CreateProductRequest
    {
        public List<string?> MediaUrls { get; set; } = new List<string?>();
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public long UserId { get; set; }
        public long CategoryId { get; set; }
    }
}
