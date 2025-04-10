using Flickoo.Api.DTOs.Category.Create;
using Flickoo.Api.DTOs.Category.Get;
using Flickoo.Api.DTOs.Category.Update;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Flickoo.Api.Interfaces.Services;

namespace Flickoo.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;
        public CategoryService(ICategoryRepository categoryRepository,
            ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }
        public async Task<ICollection<GetCategoryResponse>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllCategoriesAsync();

            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("GetAllCategoriesAsync: No categories found.");
                return new List<GetCategoryResponse>();
            }

            _logger.LogInformation($"GetAllCategoriesAsync: {categories.Count()} categories retrieved successfully.");

            var response = categories.Select(c => new GetCategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
            }).ToList();

            return response;
        }

        public async Task<GetCategoryResponse?> GetCategoryByIdAsync(long categoryId)
        {
            if (categoryId == 0)
            {
                _logger.LogError("GetCategoryByIdAsync: Invalid category ID provided.");
                return null;
            }
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                _logger.LogWarning($"GetCategoryByIdAsync: Category with ID {categoryId} not found.");
                return null;
            }

            _logger.LogInformation($"GetCategoryByIdAsync: Category with ID {categoryId} retrieved successfully.");
            
            var response = new GetCategoryResponse
            {
                Name = category.Name,
            };

            return response;
        }

        public async Task<GetCategoryResponse?> GetCategoryByNameAsync(GetCategoryRequest request)
        {
            if (request == null)
            {
                _logger.LogError("GetCategoryByNameAsync: Request is null.");
                return null;
            }

            var category = await _categoryRepository.GetCategoryByNameAsync(request.Name);

            if (category == null)
            {
                _logger.LogWarning($"GetCategoryByNameAsync: Category with name {request.Name} not found.");
                return null;
            }

            _logger.LogInformation($"GetCategoryByNameAsync: Category with name {request.Name} retrieved successfully.");

            var response = new GetCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
            };

            return response;
        }

        public async Task<bool> AddCategoryAsync(CreateCategoryRequest request)
        {
            if (request == null)
            {
                _logger.LogError("AddCategoryAsync: Request is null.");
                return false;
            }

            var category = new Category
            {
                Name = request.Name,
            };

            var categoryId = await _categoryRepository.AddCategoryAsync(category);
            if (categoryId == 0)
            {
                _logger.LogError("AddCategoryAsync: Failed to add category.");
                return false;
            }

            _logger.LogInformation($"AddCategoryAsync: Category with ID {categoryId} added successfully.");
            return true;
        }

        public async Task<bool> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request)
        {
            if (request == null)
            {
                _logger.LogError("UpdateCategoryAsync: Request is null.");
                return false;
            }

            var category = new Category
            {
                Id = categoryId,
                Name = request.Name ?? string.Empty,
            };

            var result = await _categoryRepository.UpdateCategoryAsync(category);

            if (!result)
            {
                _logger.LogError($"UpdateCategoryAsync: Failed to update category with ID {categoryId}.");
                return false;
            }

            _logger.LogInformation($"UpdateCategoryAsync: Category with ID {categoryId} updated successfully.");
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(long categoryId)
        {
            if (categoryId == 0)
            {
                _logger.LogError("DeleteCategoryAsync: Invalid category ID provided.");
                return false;
            }
            
            var result = await _categoryRepository.DeleteCategoryAsync(categoryId);
            
            if (!result)
            {
                _logger.LogError($"DeleteCategoryAsync: Failed to delete category with ID {categoryId}.");
                return false;
            }

            _logger.LogInformation($"DeleteCategoryAsync: Category with ID {categoryId} deleted successfully.");
            return true;
        }
    }
}
