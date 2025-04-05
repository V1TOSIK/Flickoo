using Flickoo.Telegram.DTOs;
using Flickoo.Telegram.enums;
using Flickoo.Telegram.SessionModels;
using Telegram.Bot;

namespace Flickoo.Telegram.Interfaces
{
    public interface IProductService
    {
        Task GetUserProducts(ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken);

        Task<Queue<GetProductResponse>> GetProductsForSwaping(ITelegramBotClient botClient,
            long chatId,
            long categoryId,
            CancellationToken cancellationToken);

        Task<ProductSessionState> AddProduct(ITelegramBotClient botClient,
            long chatId,
            ProductSession session,
            CancellationToken cancellationToken);

        Task<ProductSessionState> UpdateProduct(ITelegramBotClient botClient,
            long chatId,
            ProductSession session,
            CancellationToken cancellationToken);
     
        Task DeleteProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken);

        Task WriteToSeller(ITelegramBotClient botClient,
            long chatId,
            long productId,
            string userName,
            CancellationToken cancellationToken);


    }
}
