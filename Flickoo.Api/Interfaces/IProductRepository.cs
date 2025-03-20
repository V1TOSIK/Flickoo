using Flickoo.Api.DTOs;
using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces
{
    public interface IProductRepository
    {
        public Task<IEnumerable<Product>> GetProductsAsync();
        public Task<Product?> GetProductByIdAsync(long id);
        public Task<Product?> AddProductAsync(CreateOrUpdateProductRequest product);
        public Task UpdateProductAsync(Product product);
        public Task DeleteProductAsync(long id);
    }
}
