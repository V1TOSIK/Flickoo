using Flickoo.Api.DTOs.Product.Get;

namespace Flickoo.Api.Interfaces.Services
{
    public interface IFavouriteService
    {
        Task<IEnumerable<GetProductResponse>> GetFavouriteProductsAsync(long userId);
        Task<IEnumerable<long>> GetFavouriteProductIdsAsync(long userId);
        Task<bool> AddFavouritesAsync(long userId, long productId);
        Task<bool> RemoveFavouritesAsync(long userId, long productId);
        Task<bool> IsFavouriteAsync(long userId, long productId);
        Task<bool> ClearFavouritesAsync(long userId);
    }
}
