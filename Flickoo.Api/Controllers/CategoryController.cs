using Flickoo.Api.DTOs.Category.Create;
using Flickoo.Api.DTOs.Category.Get;
using Flickoo.Api.DTOs.Category.Update;
using Flickoo.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flickoo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;
        public CategoryController(ICategoryService categoryService,
            ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        // GET
        #region GET
        [HttpGet]
        public async Task<ActionResult<ICollection<GetCategoryResponse>>> GetAllCategoriesAsync()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("GetAllCategoriesAsync: No categories found.");
                return NotFound("No categories found.");
            }

            _logger.LogInformation($"GetAllCategoriesAsync: {categories.Count} categories retrieved successfully.");
            return Ok(categories);
        }
        
        [HttpGet("{categoryId}")]
        public async Task<ActionResult<GetCategoryResponse>> GetCategoryByIdAsync(long categoryId)
        {
            if (categoryId <= 0)
            {
                _logger.LogError("GetCategoryByIdAsync: Invalid category ID provided.");
                return BadRequest("Invalid category ID provided.");
            }

            var category = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (category == null)
            {
                _logger.LogWarning($"GetCategoryByIdAsync: Category with ID {categoryId} not found.");
                return NotFound($"Category with ID {categoryId} not found.");
            }

            _logger.LogInformation($"GetCategoryByIdAsync: Category with ID {categoryId} retrieved successfully.");

            return Ok(category);
        }
        #endregion
        // POST
        #region POST
        [HttpPost]
        public async Task<ActionResult<string>> CreateCategoryAsync([FromBody] CreateCategoryRequest request)
        {
            if (request == null)
            {
                _logger.LogError("CreateCategoryAsync: Request is null.");
                return BadRequest("Request is null.");
            }
            if (string.IsNullOrEmpty(request.Name))
            {
                _logger.LogError("CreateCategoryAsync: Category name is null or empty.");
                return BadRequest("Category name is null or empty.");
            }
            var response = await _categoryService.AddCategoryAsync(request);
            if (!response)
            {
                _logger.LogError("CreateCategoryAsync: Failed to create category.");
                return BadRequest("Failed to create category.");
            }
            _logger.LogInformation($"CreateCategoryAsync: Category created successfully.");

            return Ok("Category created successfully.");
        }
        #endregion

        // PUT
        #region PUT
        [HttpPut("{categoryId}")]
        public async Task<ActionResult<string>> UpdateCategoryAsync([FromRoute] long categoryId, [FromBody] UpdateCategoryRequest request)
        {
            if (categoryId <= 0)
            {
                _logger.LogError("UpdateCategory: Invalid category ID provided.");
                return BadRequest("Invalid category ID provided.");
            }
            if (request == null)
            {
                _logger.LogError("UpdateCategory: Request is null.");
                return BadRequest("Request is null.");
            }

            var response = await _categoryService.UpdateCategoryAsync(categoryId, request);
            
            if (!response)
            {
                _logger.LogError("UpdateCategory: Failed to update category.");
                return BadRequest("Failed to update category.");
            }

            _logger.LogInformation($"UpdateCategory: Category with ID {categoryId} updated successfully.");
            
            return Ok("Category updated successfully.");
        }
        #endregion

        // DELETE
        #region DELETE
        [HttpDelete("{categoryId}")]
        public async Task<ActionResult<string>> DeleteCategoryAsync([FromRoute] long categoryId)
        {
            if (categoryId <= 0)
            {
                _logger.LogError("DeleteCategory: Invalid category ID provided.");
                return BadRequest("Invalid category ID provided.");
            }
            
            var response = await _categoryService.DeleteCategoryAsync(categoryId);
            
            if (!response)
            {
                _logger.LogError("DeleteCategory: Failed to delete category.");
                return BadRequest("Failed to delete category.");
            }

            _logger.LogInformation($"DeleteCategory: Category with ID {categoryId} deleted successfully.");
            
            return Ok("Category deleted successfully.");
        }
        #endregion
    }
}
