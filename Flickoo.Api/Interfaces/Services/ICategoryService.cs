using Flickoo.Api.DTOs.Category.Create;
using Flickoo.Api.DTOs.Category.Get;
using Flickoo.Api.DTOs.Category.Update;

namespace Flickoo.Api.Interfaces.Services
{
    public interface ICategoryService
    {
        Task<ICollection<GetCategoryResponse>> GetAllCategoriesAsync();
        Task<GetCategoryResponse?> GetCategoryByIdAsync(long categoryId);
        Task<GetCategoryResponse?> GetCategoryByNameAsync(GetCategoryRequest request);
        Task<bool> AddCategoryAsync(CreateCategoryRequest request);
        Task<bool> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request);
        Task<bool> DeleteCategoryAsync(long categoryId);
    }
}
