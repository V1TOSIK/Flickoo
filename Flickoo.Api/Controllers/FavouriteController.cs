using Flickoo.Api.DTOs.Product.Get;
using Flickoo.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flickoo.Api.Controllers
{
    [Route("api/user/{userId}/favourites")]
    [ApiController]
    public class FavouriteController : ControllerBase
    {
        private readonly IFavouriteService _favouriteService;
        private readonly ILogger<FavouriteController> _logger;
        public FavouriteController(IFavouriteService favouriteService,
            ILogger<FavouriteController> logger)
        {
            _favouriteService = favouriteService;
            _logger = logger;
        }

        // GET
        #region GET
        [HttpGet]
        public async Task<IEnumerable<GetProductResponse>> GetFavouritesProduct([FromRoute] long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetFavouritesProduct: Invalid user ID provided.");
                return Enumerable.Empty<GetProductResponse>();
            }

            var products = await _favouriteService.GetFavouriteProductsAsync(userId);
            
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"GetFavouritesProduct: No favourite products found for user {userId}.");
                return Enumerable.Empty<GetProductResponse>();
            }

            _logger.LogInformation($"GetFavouritesProduct: Favourite products retrieved for user {userId}.");
            
            return products;
        }
        #endregion

        // POST
        #region POST
        [HttpPost("{productId}")]
        public async Task<ActionResult<string>> AddToFavourite([FromRoute] long productId, [FromRoute] long userId)
        {
            if (productId < 0 || userId < 0)
            {
                _logger.LogError("AddToFavourite: Invalid product ID or user ID provided.");
                return BadRequest("Invalid product ID or user ID");
            }

            var result = await _favouriteService.AddFavouritesAsync(productId, userId);
            if (result)
            {
                _logger.LogInformation($"Product {productId} added to user {userId}'s favourites.");
                return Ok("Added to favourites");
            }
            else
            {
                _logger.LogError($"Failed to add product {productId} to user {userId}'s favourites.");
                return BadRequest("Failed to add to favourites");
            }
        }
        #endregion

        // DELETE
        #region DELETE
        [HttpDelete("{productId}")]
        public async Task<ActionResult<string>> RemoveFromFavourite([FromRoute] long productId, [FromRoute] long userId)
        {
            if (productId < 0 || userId < 0)
            {
                _logger.LogError("RemoveFromFavourite: Invalid product ID or user ID provided.");
                return BadRequest("Invalid product ID or user ID");
            }

            var result = await _favouriteService.RemoveFavouritesAsync(productId, userId);

            if (result)
            {
                _logger.LogInformation($"Product {productId} removed from user {userId}'s favourites.");
                return Ok("Removed from favourites");
            }
            else
            {
                _logger.LogError($"Failed to remove product {productId} from user {userId}'s favourites.");
                return BadRequest("Failed to remove from favourites");
            }
        }
        #endregion
    }
}
