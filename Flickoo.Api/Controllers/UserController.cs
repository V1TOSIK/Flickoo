using Flickoo.Api.Data;
using Flickoo.Api.DTOs;
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
        public async Task<ActionResult<User>> Get([FromRoute] long id)
        {
            var findUser = await _dBContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (findUser == null)
                return NotFound();

            var response = new GetUserResponse
            {
                Username = findUser.Username,
                LocationName = findUser.Location.Name
            };

            return Ok(response);
        }

        [HttpGet("check/{id}")]
        public async Task<ActionResult<bool>> CheckUser([FromRoute] long id)
        {
            if (id == 0) return BadRequest("Id is not valid");

            var userExists = await _dBContext.Users.AnyAsync(u => u.Id == id);
            
            return Ok(userExists);
        }

        [HttpGet("myProducts/{id}")]
        public async Task<ActionResult<ICollection<Product>>> GetUserProducts([FromRoute] long id)
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
        public async Task<ActionResult> Post([FromBody] CreateUserRequest request)
        {
            var locationExists = await _dBContext.Locations
                .FirstOrDefaultAsync(l => l.Name == request.LocationName);

            if (locationExists == null)
            { 
                var newLocation = new Location
                {
                    Name = request.LocationName
                };
                await _dBContext.Locations.AddAsync(newLocation);
                await _dBContext.SaveChangesAsync();
            }

            if (string.IsNullOrEmpty(request.Username))
                return BadRequest("Username is null or empty");

            if (await _dBContext.Users.AnyAsync(u => u.Id == request.Id))
                return BadRequest("User with this id already exists.");

            var newUser = new User
            {
                Id = request.Id,
                Username = request.Username,
                LocationId = locationId
            };

            await _dBContext.Users.AddAsync(newUser);

            await _dBContext.SaveChangesAsync();

            return Ok($"OK, user with name: {newUser.Username} and id: {newUser.Id}");
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(long id, string? userName)
        {
            var userExists = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (userExists == null)
                return NotFound("User not found");

            if (string.IsNullOrEmpty(userName))
                return BadRequest("no update data");

            if (string.IsNullOrEmpty(userName))
                userName = userExists.Username;


                await _dBContext.Users
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(u => u.Username, userName)
                    );

          
            return Ok();
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var findUser = await _dBContext.Users.AnyAsync(u => u.Id == id);
            if (!findUser)
                return NotFound();
 
            await _dBContext.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();
            return NoContent();
        }
    }
}
