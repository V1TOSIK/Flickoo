using Flickoo.Api.Data;
using Flickoo.Api.DTOs;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly FlickooDbContext _dbContext;
        private readonly IMediaRepository _mediaRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductController> _logger;

        public ProductController(FlickooDbContext dbContext,
            IMediaRepository mediaRepository,
            IProductRepository productRepository,
            ILogger<ProductController> logger)
        {
            _dbContext = dbContext;
            _mediaRepository = mediaRepository;
            _productRepository = productRepository;
            _logger = logger;

        }

        // GET api/<ProductController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ICollection<Product>>> GetByUserId([FromRoute] long id)
        {
            if (id == 0)
                return BadRequest();

            if (!await _dbContext.Users.AnyAsync(u => u.Id == id && u.Registered))
                return NotFound();

            var userProducts = await _dbContext.Products
                .AsNoTracking()
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Category)
                .Include(p => p.MediaUrls)
                .ToListAsync();

            var productResponse = userProducts.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Description = p.Description,
                CategoryName = p.Category?.Name ?? "",
                MediaUrls = p.MediaUrls?.Select(m => m?.Url).ToList() ?? []
            }).ToList();

            return Ok(productResponse);
        }

        [HttpGet("liked/{userId}/{filter}")]
        public async Task<ActionResult<ICollection<ProductResponse>>> GetLikedProduct([FromRoute] long userId, [FromRoute] string filter)
        {
            if (userId == 0)
                return BadRequest();

            if (!await _dbContext.Users.AnyAsync(u => u.Id == userId))
                return NotFound();


            var query = await _dbContext.Likes
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .Include(l => l.Product)
                    .ThenInclude(p => p.Category)
                .Include(l => l.Product.MediaUrls)
                .ToListAsync();

            if (filter == "FirstNew")
                query = query.OrderByDescending(l => l.Product.CreatedAt).ToList();

            else if (filter == "FirstOld")
                query = query.OrderBy(l => l.Product.CreatedAt).ToList();

            else
                return BadRequest();

            var likedProducts = query
                .Select(l => l.Product)
                .ToList();

            var productResponse = likedProducts.Select(p => new ProductResponse()
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    CategoryName = p.Category?.Name ?? "",
                    MediaUrls = p.MediaUrls?.Select(m => m?.Url).ToList() ?? []
                }).ToList();
            return Ok(productResponse);
        }

        [HttpGet("bycategory/{categoryid}")]
        public async Task<ActionResult<ICollection<Product>>> GetByCategory([FromRoute] long categoryId)
        {
            if (categoryId == 0)
            {
                var productList = await _dbContext.Products
                    .AsNoTracking()
                    .OrderByDescending(p => p.CreatedAt)
                    .Include(p => p.Category)
                    .Include(p => p.MediaUrls)
                    .ToListAsync();

                var productResponse = productList.Select(p => new ProductResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    CategoryName = p.Category?.Name ?? "",
                    MediaUrls = p.MediaUrls?.Select(m => m?.Url).ToList() ?? []
                }).ToList();

                return Ok(productResponse);
            }

            if (!await _dbContext.Categories.AnyAsync(c => c.Id == categoryId))
                return NotFound();

            var categoryProducts = await _dbContext.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == categoryId)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Category)
                .Include(p => p.MediaUrls)
                .ToListAsync();
            var productByCategoryResponse = categoryProducts.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Description = p.Description,
                CategoryName = p.Category?.Name ?? "",
                MediaUrls = p.MediaUrls?.Select(m => m?.Url).ToList() ?? []
            }).ToList();

            return Ok(productByCategoryResponse);
        }

        [HttpGet("category")]
        public async Task<ActionResult<ICollection<Category>>> GetCategories()
        {
            var categories = await _dbContext.Categories
                .AsNoTracking()
                .ToListAsync();
            return Ok(categories);
        }

        [HttpGet("userId/{productId}")]
        public async Task<ActionResult<long>> GetSellerId([FromRoute] long productId)
        {
            var product = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product?.User == null)
                return NotFound();

            return Ok(product.User.Id);
        }

        // POST api/<ProductController>
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdateProductRequest product)
        {
            var newProduct = await _productRepository.AddProductAsync(product);

            if (newProduct == null)
                return BadRequest();

            foreach (var mediaUrl in product.MediaUrls)
            {
                if (mediaUrl == null)
                {
                    _logger.LogError("MediaUrl is null");
                    return BadRequest();
                }
                await _mediaRepository.AddMediaAsync(mediaUrl, newProduct.Id);
            }
            return Ok();
        }

        [HttpPost("like/{productId}/user/{userId}")]
        public async Task<ActionResult> PostLike([FromRoute] long productId, [FromRoute] long userId)
        {
            var productExists = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);

            if (productExists == null)
                return NotFound();

            var userExists = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userExists == null)
                return NotFound();

            var likeExists = await _dbContext.Likes
                .AnyAsync(l => l.ProductId == productId && l.UserId == userId);

            if (likeExists)
                return Ok();

            var newLike = new Like
            {
                ProductId = productId,
                Product = productExists,
                UserId = userId,
                User = userExists
            };

            await _dbContext.Likes.AddAsync(newLike);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        // PUT api/<ProductController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Put([FromRoute] long id, [FromBody] CreateOrUpdateProductRequest product)
        {

            var productFromDb = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (productFromDb == null)
                return NotFound();

            var name = product.Name ?? productFromDb.Name;
            var price = product.Price;
            var description = product.Description ?? productFromDb.Description;

            if (product.MediaUrls == null)
            {
                _logger.LogError("MediaUrls is null");
                return BadRequest();
            }

            _dbContext.MediaFiles
                .Where(m => m.ProductId == id)
                .ExecuteDelete();

            foreach (var mediaUrl in product.MediaUrls)
            {
                if (mediaUrl == null)
                {
                    _logger.LogError("MediaUrl is null");
                    return BadRequest();
                }
                productFromDb.MediaUrls.Add(new MediaFile
                {
                    ProductId = productFromDb.Id,
                    Url = mediaUrl
                });
            }

            await _dbContext.Products
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(p => p.Name, name)
                    .SetProperty(p => p.Price, price)
                    .SetProperty(p => p.Description, description)
                );
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        // DELETE api/<ProductController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute] long id)
        {
            var productExists = await _dbContext.Products.AnyAsync(p => p.Id == id);
            if (!productExists)
                return NotFound();

            await _dbContext.Products
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync();

            return Ok();
        }

        [HttpDelete("{productId}/user/{userId}")]
        public async Task<ActionResult> RemoveLike([FromRoute] long userId, [FromRoute] long productId)
        {
            var rowsAffected = await _dbContext.Likes
                .Where(l => l.UserId == userId && l.ProductId == productId)
                .ExecuteDeleteAsync();

            if (rowsAffected == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
