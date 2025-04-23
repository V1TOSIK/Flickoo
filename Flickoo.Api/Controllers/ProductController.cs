using Flickoo.Api.DTOs.Product.Create;
using Flickoo.Api.DTOs.Product.Get;
using Flickoo.Api.DTOs.Product.Update;
using Flickoo.Api.DTOs.User.Get;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService,
            IUserService userService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _userService = userService;
            _logger = logger;

        }

        //GET

        #region GET

        [HttpGet]
        public async Task<ActionResult<ICollection<GetProductResponse>>> GetAllProductsAsync()
        {
            var products = await _productService.GetAllProductsAsync();
            if (products == null)
                return NotFound();
            var productResponse = products.Select(p => new GetProductResponse()
            {
                Id = p.Id,
                Name = p.Name,
                PriceAmount = p.PriceAmount,
                PriceCurrency = p.PriceCurrency,
                Description = p.Description,
                MediaUrls = p.MediaUrls
            }).ToList();
            return Ok(productResponse);
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<GetProductResponse?>> GetProductByIdAsync([FromRoute] long productId)
        {
            if (productId < 0)
                return BadRequest();

            var product = await _productService.GetProductByIdAsync(productId);

            if (product == null)
                return NotFound();

            var productResponse = new GetProductResponse()
            {
                Id = product.Id,
                Name = product.Name,
                PriceAmount = product.PriceAmount,
                PriceCurrency = product.PriceCurrency,
                Description = product.Description,
                MediaUrls = product.MediaUrls
            };

            return Ok(productResponse);
        }

        [HttpGet("myproducts/{userId}")]
        public async Task<ActionResult<ICollection<GetProductResponse>>> GetProductsByUserIdAsync([FromRoute] long userId)
        {
            if (userId == 0)
                return BadRequest();

            var userRegistered = await _userService.CheckUserRegistrationAsync(userId);

            if (!userRegistered)
            {
                _logger.LogError("User not registered");
                return NotFound();
            }

            var products = await _productService.GetProductsByUserIdAsync(userId);

            if (products == null)
                return NotFound();


            return Ok(products);
        }

        [HttpGet("{productId}/seller")]
        public async Task<ActionResult<GetUserResponse>> GetSellerByProductIdAsync([FromRoute] long productId)
        {
            if (productId < 0)
                return BadRequest();
            var response = await _productService.GetSellerByProductIdAsync(productId);
            if (response == null)
            {
                _logger.LogError("Product not found");
                return NotFound();
            }
            _logger.LogInformation("Product found");
            return Ok(response);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<GetProductResponse>>> GetProductsByCategoryIdAsync([FromRoute] long categoryId)
        {
            if (categoryId < 0)
                return BadRequest();

            IEnumerable<GetProductResponse> products;

            if (categoryId == 0)
                products = await _productService.GetAllProductsAsync();

            else
                products = await _productService.GetProductsByCategoryIdAsync(categoryId);

            if (products == null)
                return NotFound();

            return Ok(products);
        }

        #endregion




        // POST
        #region POST
        [HttpPost]
        public async Task<ActionResult<long>> Post([FromBody] CreateProductRequest request)
        {
            if (request == null)
            {
                _logger.LogError("Product is null");
                return BadRequest("Error: Product is null");
            }

            if (string.IsNullOrEmpty(request.Name) && request.PriceAmount == 0 && string.IsNullOrEmpty(request.PriceCurrency) && string.IsNullOrEmpty(request.Description))
            {
                _logger.LogError("Product is empty");
                return BadRequest("Error: Product is empty");
            }

            var response = await _productService.AddProductAsync(request);

            if (response == -1)
            {
                _logger.LogError("Product was not added");
                return BadRequest("Error: Product was not added");
            }

            _logger.LogInformation("Product was added");
            return Ok(response);
        }
        #endregion

        // PUT
        #region PUT
        [HttpPut("{productId}")]
        public async Task<ActionResult<string>> UpdateProductAsync([FromRoute] long productId, [FromBody] UpdateProductRequest request)
        {
            if (productId < 0)
            {
                _logger.LogError("Product ID is 0");
                return BadRequest("Error: Product ID = 0");
            }
            if (request == null)
            {
                _logger.LogError("Product is null");
                return BadRequest("Error: Product is null");
            }

            if (string.IsNullOrEmpty(request.Name) && request.PriceAmount < 0 && string.IsNullOrEmpty(request.PriceCurrency) && string.IsNullOrEmpty(request.Description))
                return BadRequest("Error: Product is empty");

            var response = await _productService.UpdateProductAsync(productId, request);



            if (!response)
                return NotFound("Product not found");

            _logger.LogInformation("Product was updated");

            return Ok("Product was updated. Successful");
        }

        #endregion

        // DELETE
        #region DELETE
        [HttpDelete("{productId}")]
        public async Task<ActionResult<string>> DeleteProductAsync([FromRoute] long productId)
        {
            if (productId < 0)
                return BadRequest("Error: Product ID = 0");

            var response = await _productService.DeleteProductAsync(productId);
            if (!response)
                return NotFound("Product not found");

            _logger.LogInformation("Product was deleted");

            return Ok("Product was deleted. Successful");
        }
        
        #endregion
    }
}
