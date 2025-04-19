namespace Flickoo.Api.DTOs.Product.Create
{
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public string PriceCurrency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long UserId { get; set; }
        public long CategoryId { get; set; }
    }
}
