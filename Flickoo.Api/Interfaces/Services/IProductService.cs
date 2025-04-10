using Flickoo.Api.DTOs.Product.Create;
using Flickoo.Api.DTOs.Product.Get;
using Flickoo.Api.DTOs.Product.Update;
using Flickoo.Api.DTOs.User.Get;

namespace Flickoo.Api.Interfaces.Services
{
    public interface IProductService
    {
        Task<IEnumerable<GetProductResponse>> GetAllProductsAsync();
        Task<IEnumerable<GetProductResponse>> GetProductsByUserIdAsync(long userId);
        Task<IEnumerable<GetProductResponse>> GetProductsByCategoryIdAsync(long categoryId);
        Task<GetProductResponse?> GetProductByIdAsync(long productId);
        Task<GetUserResponse?> GetSellerByProductIdAsync(long productId);
        Task<bool> AddProductAsync(CreateProductRequest request);
        Task<bool> UpdateProductAsync(long productId, UpdateProductRequest request);
        Task<bool> DeleteProductAsync(long productId);

    }
}
