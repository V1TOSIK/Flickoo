using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetProductsByUserIdAsync(long userId);
        Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(long categoryId);
        Task<Product?> GetProductByIdAsync(long productId);
        Task<Product?> AddProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(long id);
    }
}
