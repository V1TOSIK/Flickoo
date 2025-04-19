using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly FlickooDbContext _dbContext;
        private readonly ILogger<CategoryRepository> _logger;
        public CategoryRepository(FlickooDbContext dbContext,
            ILogger<CategoryRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var categories = await _dbContext.Categories
                .AsNoTracking()
                .ToListAsync();

            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("GetAllCategoriesAsync: No categories found.");
                return Enumerable.Empty<Category>();
            }

            _logger.LogInformation($"GetAllCategoriesAsync: {categories.Count} categories retrieved successfully.");

            return categories;
        }

        public async Task<Category?> GetCategoryByIdAsync(long categoryId)
        {
            if (categoryId < 0)
            {
                _logger.LogError("GetCategoryByIdAsync: Invalid category ID provided.");
                return null;
            }
            var category = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == categoryId);
            if (category == null)
            {
                _logger.LogWarning($"GetCategoryByIdAsync: Category with ID {categoryId} not found.");
                return null;
            }
            _logger.LogInformation($"GetCategoryByIdAsync: Category with ID {categoryId} retrieved successfully.");
            return category;

        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogError("GetCategoryByNameAsync: Category name is null or empty.");
                return null;
            }
            var category = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name);
            if (category == null)
            {
                _logger.LogWarning($"GetCategoryByNameAsync: Category with name {name} not found.");
                return null;
            }
            _logger.LogInformation($"GetCategoryByNameAsync: Category with name {name} retrieved successfully.");
            return category;
        }

        public async Task<long> AddCategoryAsync(Category category)
        {
            if (category == null)
            {
                _logger.LogError("AddCategoryAsync: Category object is null.");
                return -1;
            }

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                _logger.LogError("AddCategoryAsync: Category name is empty.");
                return -1;
            }

            var existingCategory = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == category.Name);

            if (existingCategory != null)
            {
                _logger.LogWarning($"AddCategoryAsync: Category with name {category.Name} already exists.");
                return -1;
            }

            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"AddCategoryAsync: Category with ID {category.Id} added successfully.");
            return category.Id;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            if (category == null)
            {
                _logger.LogError("UpdateCategoryAsync: Category object is null.");
                return false;
            }
            if (category.Id < 0)
            {
                _logger.LogError("UpdateCategoryAsync: Invalid category ID provided.");
                return false;
            }
            var existingCategory = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == category.Id);
            if (existingCategory == null)
            {
                _logger.LogWarning($"UpdateCategoryAsync: Category with ID {category.Id} not found.");
                return false;
            }

            category.Name = string.IsNullOrWhiteSpace(category.Name)
                ? existingCategory.Name
                : category.Name;

            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"UpdateCategoryAsync: Category with ID {category.Id} updated successfully.");
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(long categoryId)
        {
            if (categoryId < 0)
            {
                _logger.LogError("DeleteCategoryAsync: Invalid category ID provided.");
                return false;
            }
            var categoryExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == categoryId);
            if (!categoryExists)
            {
                _logger.LogWarning($"DeleteCategoryAsync: Category with ID {categoryId} not found.");
                return false;
            }

            var deleteResult = await _dbContext.Categories
                .Where(c => c.Id == categoryId)
                .ExecuteDeleteAsync();

            if (deleteResult == 0)
            {
                _logger.LogError($"DeleteCategoryAsync: Failed to delete category with ID {categoryId}.");
                return false;
            }

                _logger.LogInformation($"DeleteCategoryAsync: Category with ID {categoryId} deleted successfully.");
            return true;
        }
    }
}
