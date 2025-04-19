using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces.Repositories
{
    public interface IFavouriteRepository
    {
        Task<IEnumerable<Product>> GetFavouriteProductsAsync(long userId);
        Task<IEnumerable<long>> GetFavouriteProductIdsAsync(long userId);
        Task<bool> AddToFavouritesAsync(long userId, long productId);
        Task<bool> RemoveFromFavouritesAsync(long userId, long productId);
        Task<bool> IsProductInFavouritesAsync(long userId, long productId);
        Task<bool> ClearFavouritesAsync(long userId);
    }
}
