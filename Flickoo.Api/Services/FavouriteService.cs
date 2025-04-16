using Flickoo.Api.DTOs.Product.Get;
using Flickoo.Api.Interfaces.Repositories;
using Flickoo.Api.Interfaces.Services;

namespace Flickoo.Api.Services
{
    public class FavouriteService : IFavouriteService
    {
        private readonly IFavouriteRepository _favouriteRepository;
        private readonly ILogger<FavouriteService> _logger;
        public FavouriteService(IFavouriteRepository favouriteRepository,
            ILogger<FavouriteService> logger)
        {
            _favouriteRepository = favouriteRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<long>> GetFavouriteProductIdsAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetFavouriteProductIdsAsync: Invalid user ID provided.");
                return Enumerable.Empty<long>();
            }
            
            var productIds = await _favouriteRepository.GetFavouriteProductIdsAsync(userId);
            
            if (productIds == null || !productIds.Any())
            {
                _logger.LogWarning($"GetFavouriteProductIdsAsync: No favourite products found for user {userId}.");
                return Enumerable.Empty<long>();
            }
            
            _logger.LogInformation($"GetFavouriteProductIdsAsync: Favourite product IDs retrieved for user {userId} successfully.");
            
            return productIds;
        }

        public async Task<IEnumerable<GetProductResponse>> GetFavouriteProductsAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetFavouriteProductsAsync: Invalid user ID provided.");
                return Enumerable.Empty<GetProductResponse>();
            }
            
            var products = await _favouriteRepository.GetFavouriteProductsAsync(userId);
           
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"GetFavouriteProductsAsync: No favourite products found for user {userId}.");
                return Enumerable.Empty<GetProductResponse>();
            }
            
            var response = products.Select(p => new GetProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                PriceAmount = p.Price.Amount,
                PriceCurrency = p.Price.Currency,
                LocationName = p.Location.Name,
                Description = p.Description,
                MediaUrls = p.ProductMedias?
                    .Select(pm => pm?.Url)
                    .Where(url => url is not null)
                    .ToList() ?? []
            }).ToList();

            _logger.LogInformation($"GetFavouriteProductsAsync: Favourite products retrieved for user {userId} successfully.");
            
            return response;
        }

        public async Task<bool> AddFavouritesAsync(long userId, long productId)
        {
            if (userId < 0 || productId < 0)
            {
                _logger.LogError("AddToFavouritesAsync: Invalid user ID or product ID provided.");
                return false;
            }

            var response = await _favouriteRepository.AddToFavouritesAsync(userId, productId);

            if (!response)
            {
                _logger.LogWarning($"AddToFavouritesAsync: Failed to add product {productId} to user {userId}'s favourites.");
                return false;
            }

            _logger.LogInformation($"AddToFavouritesAsync: Product {productId} added to user {userId}'s favourites successfully.");

            return true;
        }

        public async Task<bool> RemoveFavouritesAsync(long userId, long productId)
        {
            if (userId < 0 || productId < 0)
            {
                _logger.LogError("RemoveFromFavouritesAsync: Invalid user ID or product ID provided.");
                return false;
            }
            
            var response = await _favouriteRepository.RemoveFromFavouritesAsync(userId, productId);
            
            if (!response)
            {
                _logger.LogWarning($"RemoveFromFavouritesAsync: Failed to remove product {productId} from user {userId}'s favourites.");
                return false;
            }
            
            _logger.LogInformation($"RemoveFromFavouritesAsync: Product {productId} removed from user {userId}'s favourites successfully.");
            
            return true;
        }

        public async Task<bool> IsFavouriteAsync(long userId, long productId)
        {
            if (userId < 0 || productId < 0)
            {
                _logger.LogError("IsProductInFavouritesAsync: Invalid user ID or product ID provided.");
                return false;
            }
            
            var isFavourite = await _favouriteRepository.IsProductInFavouritesAsync(userId, productId);
            
            if (!isFavourite)
            {
                _logger.LogWarning($"IsProductInFavouritesAsync: Product {productId} is not in user {userId}'s favourites.");
                return false;
            }

            _logger.LogInformation($"IsProductInFavouritesAsync: Product {productId} is in user {userId}'s favourites.");

            return true;
        }

        public async Task<bool> ClearFavouritesAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("ClearFavouritesAsync: Invalid user ID provided.");
                return false;
            }

            var response = await _favouriteRepository.ClearFavouritesAsync(userId);

            if (!response)
            {
                _logger.LogWarning($"ClearFavouritesAsync: Failed to clear favourites for user {userId}.");
                return false;
            }

            _logger.LogInformation($"ClearFavouritesAsync: Favourites cleared for user {userId} successfully.");
            
            return true;
        }
    }
}
