namespace Flickoo.Telegram.DTOs
{
    public class GetProductResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public List<string?> MediaUrls { get; set; } = [];
    }
}
