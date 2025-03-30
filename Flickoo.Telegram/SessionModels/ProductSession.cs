using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.enums;

namespace Flickoo.Telegram.SessionModels
{
    public class ProductSession
    {
        public ProductSessionState State{ get; set; } = ProductSessionState.Idle;
        public long ProductId { get; set; }
        public List<string?> MediaUrls { get; set; } = new List<string?>();
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0m;
        public string ProductDescription { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public bool AddMoreMedia { get; set; } = true;
        public string? Action {  get; set; } = null;
        public Queue<GetProductResponse> ProductsQueue { get; set; } = [];
    }
}
