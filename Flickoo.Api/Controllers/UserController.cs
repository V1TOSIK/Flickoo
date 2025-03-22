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
        public async Task<ActionResult<User>> GetUserById([FromRoute] long id)
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

            var userExists = await _dBContext.Users.AnyAsync(u => u.Id == id && u.Registered == true);

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
        public async Task<ActionResult<string>> Post([FromBody] CreateOrUpdateUserRequest request)
        {
            if (request.Id == 0)
                return BadRequest("Id is not valid");

            var userExists = await _dBContext.Users.AnyAsync(u => u.Id == request.Id);

            if (userExists)
                return Ok("userExist");

            if (string.IsNullOrEmpty(request.Username))
                return BadRequest("Username is null or empty");

            var locationExists = await _dBContext.Locations
                .FirstOrDefaultAsync(l => l.Name == request.LocationName);

            long locationId;
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


            if (!userExists)
            {
                var newUnRegisteredUser = new User
                {
                    Id = request.Id,
                    Username = request.Username,
                    LocationId = locationId,
                    Registered = false
                };
                await _dBContext.Users.AddAsync(newUnRegisteredUser);
                await _dBContext.SaveChangesAsync();
                return Ok($"OK, user with name: {newUnRegisteredUser.Username} and id: {newUnRegisteredUser.Id} was added to notRegistered");
            }

            return Ok("Finish anyone check not do");

        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] CreateOrUpdateUserRequest request)
        {
            if (request.Id == 0)
                return BadRequest("Id is not valid");

            var userFromDb = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id);

            if (userFromDb == null)
                return NotFound("User not found");

            if (userFromDb.Registered)
                return BadRequest("User is already registered");

            var locationExists = await _dBContext.Locations
                 .FirstOrDefaultAsync(l => l.Name == request.LocationName);

            long locationId;
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

            userFromDb.Username = request.Username;
            userFromDb.LocationId = locationId;
            userFromDb.Registered = true;

            await _dBContext.SaveChangesAsync();
            return Ok();
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
