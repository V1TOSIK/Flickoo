using Flickoo.Telegram.enums;
using Telegram.Bot;

namespace Flickoo.Telegram.Interfaces
{
    public interface IProductService
    {
        Task GetProducts(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken);

        Task<ProductSessionState> AddProduct(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            CancellationToken cancellationToken);

        Task UpdateProduct(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            CancellationToken cancellationToken);
     
        Task DeleteProduct(ITelegramBotClient botClient,
            long chatId,
            string productName,
            CancellationToken cancellationToken);
    }
}
