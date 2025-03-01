using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly FlickooDbContext _dBContext;
        public UserController(FlickooDbContext dBContext)
        {
            _dBContext = dBContext;
        }
        // GET: api/<UserController>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(long id)
        {
            var findUser = await _dBContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (findUser == null)
                return NotFound();

            return Ok(findUser);
        }

        [HttpGet("MyProducts")]
        public async Task<ActionResult<ICollection<Product>>> GetAllUserProducts(long id)
        {
            var productList = await _dBContext.Products
                .Where(p => p.UserId == id)
                .AsNoTracking()
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
            return Ok(productList);
        }

        // POST api/<UserController>
        [HttpPost]
        public async Task<ActionResult> Post(User user)
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.PhoneNumber))
                return BadRequest();

            if (await _dBContext.Users.AnyAsync(u => u.Id == user.Id))
                return BadRequest("User with this id already exists.");

            var newUser = new User
            {
                Id = user.Id,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber
            };

            await _dBContext.Users.AddAsync(newUser);

            await _dBContext.SaveChangesAsync();

            return Ok($"OK, user with name: {newUser.Username} and id: {newUser.Id}");
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(long id, string userName, string phoneNumber)
        {
            var userExists = await _dBContext.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
                return NotFound();

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(phoneNumber))
                return BadRequest("Username and phone number cannot be empty.");


            await _dBContext.Users
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(u => u.Username, userName)
                    .SetProperty(u => u.PhoneNumber, phoneNumber)
                    );

          
            return Ok();
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var findUser = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (findUser == null)
                return NotFound();
 
            await _dBContext.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();
            return NoContent();
        }
    }
}
