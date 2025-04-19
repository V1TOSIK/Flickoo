using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Repositories
{
    public class FavouriteRepository : IFavouriteRepository
    {
        private readonly FlickooDbContext _dbContext;
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<FavouriteRepository> _logger;
        public FavouriteRepository(FlickooDbContext dbContext,
            IUserRepository userRepository,
            IProductRepository productRepository,
            ILogger<FavouriteRepository> logger)
        {
            _dbContext = dbContext;
            _userRepository = userRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetFavouriteProductsAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetFavouriteProductsByUserId: Invalid user ID provided.");
                return [];
            }

            var products = await _dbContext.Favourites
                .AsNoTracking()
                .Include(f => f.Product)
                .Where(f => f.UserId == userId)
                .Select(f => f.Product)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                _logger.LogWarning($"GetFavouriteProductsByUserId: No favourite products found for user ID {userId}.");
                return [];
            }

            _logger.LogInformation($"GetFavouriteProductsByUserId: Favourite products for user ID {userId} retrieved successfully.");
            
            return products;
        }

        public async Task<IEnumerable<long>> GetFavouriteProductIdsAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetFavouriteProductIdsByUserId: Invalid user ID provided.");
                return [];
            }

            var productIds = await _dbContext.Favourites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToListAsync();

            if (productIds == null || !productIds.Any())
            {
                _logger.LogWarning($"GetFavouriteProductIdsByUserId: No favourite product IDs found for user ID {userId}.");
                return [];
            }

            _logger.LogInformation($"GetFavouriteProductIdsByUserId: Favourite product IDs for user ID {userId} retrieved successfully.");

            return productIds;
        }

        public async Task<bool> AddToFavouritesAsync(long userId, long productId)
        {
            if (userId < 0)
            {
                _logger.LogError("AddToFavouritesAsync: Invalid user ID provided.");
                return false;
            }

            if (productId < 0)
            {
                _logger.LogError("AddToFavouritesAsync: Invalid product ID provided.");
                return false;
            }

            var favourite = new Favourite
            {
                UserId = userId,
                ProductId = productId,
            };

            await _dbContext.Favourites.AddAsync(favourite);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"AddToFavouritesAsync: Product with ID {productId} added to favourites for user ID {userId}.");
            return true;
        }

        public async Task<bool> RemoveFromFavouritesAsync(long userId, long productId)
        {
            if (userId < 0)
            {
                _logger.LogError("RemoveFromFavouritesAsync: Invalid user ID provided.");
                return false;
            }
            if (productId < 0)
            {
                _logger.LogError("RemoveFromFavouritesAsync: Invalid product ID provided.");
                return false;
            }

            var favouriteExists = await _dbContext.Favourites
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

            if (!favouriteExists)
            {
                _logger.LogWarning($"RemoveFromFavouritesAsync: Favourite with user ID {userId} and product ID {productId} not found.");
                return false;
            }

            var response = await _dbContext.Favourites
                .Where(f => f.UserId == userId && f.ProductId == productId)
                .ExecuteDeleteAsync();
            
            if (response == 0)
            {
                _logger.LogWarning($"RemoveFromFavouritesAsync: Failed to remove favourite with user ID {userId} and product ID {productId}.");
                return false;
            }

            _logger.LogInformation($"RemoveFromFavouritesAsync: Favourite with user ID {userId} and product ID {productId} removed successfully.");
            
            return true;
        }

        public async Task<bool> IsProductInFavouritesAsync(long userId, long productId)
        {
            if (userId < 0)
            {
                _logger.LogError("IsProductInFavouritesAsync: Invalid user ID provided.");
                return false;
            }

            if (productId < 0)
            {
                _logger.LogError("IsProductInFavouritesAsync: Invalid product ID provided.");
                return false;
            }

            var favouriteExists = await _dbContext.Favourites
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

            if (!favouriteExists)
            {
                _logger.LogWarning($"IsProductInFavouritesAsync: Product with ID {productId} is not in favourites for user ID {userId}.");
                return false;
            }

            _logger.LogInformation($"IsProductInFavouritesAsync: Product with ID {productId} is in favourites for user ID {userId}.");
            return true;
        }

        public async Task<bool> ClearFavouritesAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("ClearFavouritesAsync: Invalid user ID provided.");
                return false;
            }

            var response = await _dbContext.Favourites
                .Where(f => f.UserId == userId)
                .ExecuteDeleteAsync();

            if (response == 0)
            {
                _logger.LogWarning($"ClearFavouritesAsync: No favourites found for user ID {userId}.");
                return false;
            }

            _logger.LogInformation($"ClearFavouritesAsync: All favourites cleared for user ID {userId}.");

            return true;
        }
    }
}
