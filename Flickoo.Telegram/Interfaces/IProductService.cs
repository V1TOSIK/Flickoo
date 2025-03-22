using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.enums;
using Telegram.Bot;

namespace Flickoo.Telegram.Interfaces
{
    public interface IProductService
    {
        Task GetProducts(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken);

        Task<Queue<GetProductResponse>> GetProductsForSwaping(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            CancellationToken cancellationToken);

        Task<ProductSessionState> AddProduct(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            bool addMoreMedia,
            CancellationToken cancellationToken);

        Task<ProductSessionState> UpdateProduct(ITelegramBotClient botClient,
            long productId,
            long chatId,
            string? productName,
            decimal? productPrice,
            string? productDescription,
            List<string?> mediaUrl,
            bool addMoreMedia,
            CancellationToken cancellationToken);
     
        Task DeleteProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken);

        Task LikeProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken);
        
        Task DislikeProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken);

        Task<Queue<GetProductResponse>> GetLikedProducts(ITelegramBotClient botClient,
            long chatId,
            string filter,
            CancellationToken cancellationToken);
    }
}
