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
        
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get([FromRoute] long id)
        {
            var findUser = await _dBContext.Users
                .AsNoTracking()
                .Include(u => u.Location)
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
        
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdateUserRequest request)
        {
            long locationId;
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
                locationId = newLocation.Id;
            }
            else
            {
                locationId = locationExists.Id;
            }

            if (string.IsNullOrEmpty(request.Username))
                return BadRequest("Username is null or empty");

            if (await _dBContext.Users.AnyAsync(u => u.Id == request.Id))
                return BadRequest("User with this id already exists.");

            var newUser = new User
            {
                Id = request.Id,
                Username = request.Username,
                LocationId = locationId,
            };

            await _dBContext.Users.AddAsync(newUser);

            await _dBContext.SaveChangesAsync();

            return Ok($"OK, user with name: {newUser.Username} and id: {newUser.Id} was added");
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdateUserRequest updateUser)
        {
            var userExists = await _dBContext.Users
                .Include(user => user.Location)
                .FirstOrDefaultAsync(u => u.Id == updateUser.Id);
            
            if (userExists == null)
                return NotFound("User not found");

            if (!string.IsNullOrEmpty(updateUser.Username))
            {
                userExists.Username = updateUser.Username;
            }

            if (!string.IsNullOrEmpty(updateUser.LocationName))
            {
                var existingLocation = await _dBContext.Locations
                    .FirstOrDefaultAsync(l => l.Name == updateUser.LocationName);

                if (existingLocation == null)
                {
                    var newLocation = new Location
                    {
                        Name = updateUser.LocationName
                    };
                    await _dBContext.Locations.AddAsync(newLocation);
                    await _dBContext.SaveChangesAsync();
                    userExists.LocationId = newLocation.Id;
                }
                else
                {
                    userExists.LocationId = existingLocation.Id;
                }
            }
            await _dBContext.SaveChangesAsync();
            return Ok();
        }
            /*
            await _dBContext.Users
                .Include(u => u.Location)
                .Where(u => u.Id == updateUser.Id)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(user => user.Username, updateUser.Username)
                    .SetProperty(user => user.LocationId, userExists.LocationId)
                );
                */

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var findUser = await _dBContext.Users.AnyAsync(u => u.Id == id);
            if (!findUser)
                return NotFound("User not found");
 
            await _dBContext.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();
            return NoContent();
        }
    }
}
