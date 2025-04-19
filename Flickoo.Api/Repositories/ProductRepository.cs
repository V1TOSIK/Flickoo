using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Flickoo.Api.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly FlickooDbContext _dbContext;
        private readonly ILogger<ProductRepository> _logger;
        public ProductRepository(FlickooDbContext dbContext,
            ILogger<ProductRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            var products = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Location)
                .AsNoTracking()
                .ToListAsync();
            if (products == null || !products.Any())
            {
                _logger.LogWarning("No products found");
                return Enumerable.Empty<Product>();
            }
            _logger.LogInformation($"{products.Count} products found");
            return products;
        }

        public async Task<IEnumerable<Product>> GetProductsByUserIdAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogWarning("Id is 0");
                return [];
            }
            var products = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Location)
                .Include(p => p.ProductMedias)
                .Where(p => p.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"No products found for user with id {userId}");
                return [];
            }
            _logger.LogInformation($"{products.Count} products found for user with id {userId}");
            return products;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(long categoryId)
        {
            if (categoryId < 0)
            {
                _logger.LogWarning("Id is 0");
                return [];
            }
            var products = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Location)
                .Where(p => p.CategoryId == categoryId)
                .AsNoTracking()
                .ToListAsync();
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"No products found for category with id {categoryId}");
                return [];
            }
            _logger.LogInformation($"{products.Count} products found for category with id {categoryId}");
            return products;
        }

        public async Task<Product?> GetProductByIdAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogWarning("Id is 0");
                return null;
            }

            var product = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Location)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                _logger.LogWarning($"Product with id {productId} not found");
                return null;
            }

            _logger.LogInformation($"Product with id {productId} found");
            
            return product;
        }

        public async Task<Product?> AddProductAsync(Product product)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == product.UserId);

            var category = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == product.CategoryId);

            var location = await _dbContext.Locations
                .FirstOrDefaultAsync(l => l.Id == product.LocationId);

            if (user == null)
            {
                _logger.LogWarning($"User with id {product.UserId} not found");
                return null;
            }
            if (category == null)
            {
                _logger.LogWarning($"Category with id {product.CategoryId} not found");
                return null;
            }
            if (location == null)
            {
                _logger.LogWarning($"Location with id {product.LocationId} not found");
                return null;
            }

            var newProduct = new Product
            {
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                LocationId = product.LocationId,
                UserId = product.UserId,
                CategoryId = product.CategoryId,
                User = user,
                Location = location,
                Category = category,
                ProductMedias = []
            };
            await _dbContext.Products.AddAsync(newProduct);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Product with id {newProduct.Id} created");
            return newProduct;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            if (product == null)
            {
                _logger.LogWarning("Product is null");
                return false;
            }
            var existingProduct = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existingProduct == null)
            {
                _logger.LogWarning($"Product with id {product.Id} not found");
                return false;
            }

            existingProduct.Name = string.IsNullOrEmpty(product.Name)
                ? existingProduct.Name
                : product.Name;

            var updatedPrice = new Price(
                product.Price.Amount == 0 ? existingProduct.Price.Amount : product.Price.Amount,
                string.IsNullOrEmpty(product.Price.Currency) ? existingProduct.Price.Currency : product.Price.Currency
            );

            existingProduct.Price = updatedPrice;

            existingProduct.LocationId = product.LocationId < 0
                ? existingProduct.LocationId
                : product.LocationId;

            existingProduct.Description = string.IsNullOrEmpty(product.Description)
                ? existingProduct.Description
                : product.Description;

            existingProduct.ProductMedias = product.ProductMedias.Any()
                ? existingProduct.ProductMedias
                : product.ProductMedias;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Product with id {product.Id} updated");
            return true;
        }

        public async Task<bool> DeleteProductAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogWarning("Id is 0");
                return false;
            }
            var productExist = await _dbContext.Products
                .AsNoTracking()
                .AnyAsync(u => u.Id == productId);
            if (productExist)
            {
                await _dbContext.Products
                    .Where(u => u.Id == productId)
                    .ExecuteDeleteAsync();
                _logger.LogInformation($"DeleteUserAsync: Product with ID {productId} deleted successfully.");
                return true;
            }
            else
            {
                _logger.LogWarning($"DeleteUserAsync: Product with ID {productId} not found.");
                return false;
            }
        }
    }
}
