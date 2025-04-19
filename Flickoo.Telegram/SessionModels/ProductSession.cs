using Flickoo.Telegram.DTOs.Product;
using Flickoo.Telegram.enums;

namespace Flickoo.Telegram.SessionModels
{
    public class ProductSession
    {
        public ProductSessionState State{ get; set; } = ProductSessionState.Idle;
        public long ProductId { get; set; }
        public List<string?> MediaUrls { get; set; } = [];
        public List<Stream> MediaFiles { get; set; } = [];
        public List<string> MediaTypes { get; set; } = [];
        public string Name { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; } = 0m;
        public string PriceCurrency { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public bool AddMoreMedia { get; set; } = true;
        public string? Action {  get; set; } = null;
        public Queue<GetProductResponse> ProductsQueue { get; set; } = [];
    }
}
