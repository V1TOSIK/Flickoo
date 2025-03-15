using Flickoo.Telegram.enums;

namespace Flickoo.Telegram.SessionModels
{
    class ProductSession
    {
        public ProductSessionState State{ get; set; } = ProductSessionState.Idle;
        public List<string?> MediaUrl { get; set; } = new List<string?>();
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0m;
        public string ProductDescription { get; set; } = string.Empty;
        public long UserId { get; set; }
        public long CategoryId { get; set; }
    }
}
