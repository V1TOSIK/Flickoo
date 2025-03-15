namespace Flickoo.Telegram.DTOs
{
    class GetProductResponse
    {
        public List<string?> MediaUrl { get; set; } = new List<string?>();
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
