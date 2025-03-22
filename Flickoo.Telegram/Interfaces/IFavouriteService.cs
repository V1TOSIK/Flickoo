using Flickoo.Telegram.DTOs;
using Telegram.Bot;

namespace Flickoo.Telegram.Interfaces
{
    public interface IFavouriteService
    {
        Task AddToFavouriteProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken);

        Task DislikeProduct(ITelegramBotClient botClient,
            long chatId,
            long productId,
            CancellationToken cancellationToken);

        Task<Queue<GetProductResponse>> GetFavouriteProducts(ITelegramBotClient botClient,
            long chatId,
            string filter,
            CancellationToken cancellationToken);
    }
}
