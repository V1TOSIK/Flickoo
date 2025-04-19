namespace Flickoo.Telegram.DTOs.Product
{
    public class GetProductResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public string PriceCurrency { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
