using Flickoo.Api.Data;
using Flickoo.Api.DTOs;
using Flickoo.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly FlickooDbContext _dbContext;

        public ProductController(FlickooDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/<ProductController>
        [HttpGet]
        public async Task<ActionResult<ICollection<Product>>> Get()
        {
            var productList = await _dbContext.Products
                .AsNoTracking()
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            return Ok(productList);
        }

        // GET api/<ProductController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ICollection<Product>>> GetByUserId([FromRoute]long id)
        {
            if (id == 0)
                return BadRequest();

            if (!await _dbContext.Users.AnyAsync(u => u.Id == id))
                return NotFound();

            var userProducts = await _dbContext.Products
                .AsNoTracking()
                .Where(p => p.UserId == id)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();


            return Ok(userProducts);
        }

        [HttpGet("category")]
        public async Task<ActionResult<ICollection<Category>>> GetCategories()
        {
            var categories = await _dbContext.Categories
                .AsNoTracking()
                .ToListAsync();
            return Ok(categories);
        }

        // POST api/<ProductController>
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CreateProductRequest product)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == product.UserId);
            var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == product.CategoryId);

            if (user == null || category == null)
                return NotFound("User or category is not found");

            var newProduct = new Product
            {
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                UserId = product.UserId,
                CategoryId = product.CategoryId,
                User = user,
                Category = category
            };
            await _dbContext.Products.AddAsync(newProduct);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        // PUT api/<ProductController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Put([FromRoute] long id,
            string name,
            decimal price,
            string description,
            long categoryId)
        {


            await _dbContext.Products
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(p => p.Name, name)
                    .SetProperty(p => p.Price, price)
                    .SetProperty(p => p.Description, description)
                    .SetProperty(p => p.CategoryId, categoryId)
                );

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
    }
}
