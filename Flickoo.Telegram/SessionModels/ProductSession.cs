using Flickoo.Telegram.enums;

namespace Flickoo.Telegram.SessionModels
{
    class ProductSession
    {
        public ProductSessionState  State{ get; set; } = ProductSessionState.Idle;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0m;
        public string ProductDescription { get; set; } = string.Empty;
    }
}
