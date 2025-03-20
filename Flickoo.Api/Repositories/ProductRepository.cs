using Flickoo.Api.Data;
using Flickoo.Api.DTOs;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces;
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

        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            return await _dbContext.Products.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(long id)
        {
            if (id == 0)
            {
                _logger.LogWarning("Id is 0");
                return null;
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                _logger.LogWarning($"Product with id {id} not found");
            }
            return product;
        }

        public async Task<Product?> AddProductAsync(CreateOrUpdateProductRequest product)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == product.UserId);
            if (user == null)
            {
                _logger.LogWarning($"User with id {product.UserId} not found");
                return null;
            }

            var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == product.CategoryId);
            if (category == null)
            {
                _logger.LogWarning($"Category with id {product.CategoryId} not found");
                return null;
            }


            var newProduct = new Product
            {
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                UserId = product.UserId,
                CategoryId = product.CategoryId,
                User = user,
                Category = category,
                MediaUrls = []

            };
            await _dbContext.Products.AddAsync(newProduct);
            await _dbContext.SaveChangesAsync();
            return newProduct;
        }

        public Task UpdateProductAsync(Product product)
        {
            throw new NotImplementedException();
        }

        public Task DeleteProductAsync(long id)
        {
            throw new NotImplementedException();
        }
    }
}
