using Flickoo.Api.DTOs.User.Create;
using Flickoo.Api.DTOs.User.Get;
using Flickoo.Api.DTOs.User.Update;
using Flickoo.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flickoo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;
        public UserController(ILogger<UserController> logger,
            IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }
        // GET
        #region GET
        [HttpGet("{userId}")]
        public async Task<ActionResult<GetUserResponse>> GetUserByIdAsync([FromRoute] long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetUserByIdAsync: Invalid user ID provided.");
                return BadRequest("Id is not valid");
            }

            var response = await _userService.GetUserByIdAsync(userId);
            if (response == null)
            {
                _logger.LogWarning($"GetUserByIdAsync: User with ID {userId} not found.");
                return NotFound(null);
            }
            
            _logger.LogInformation($"GetUserByIdAsync: User with ID {userId} retrieved successfully.");
            return Ok(response);
        }
        #endregion

        // POST
        #region POST
        [HttpPost]
        public async Task<ActionResult<string>> AddUnregisteredUserAsync([FromBody] CreateUserRequest request)
        {
            if (request.Id < 0)
            {
                _logger.LogError("AddUnregisteredUserAsync: Invalid user ID provided.");
                return BadRequest("Id is not valid");
            }

            var response = await _userService.AddUnregisteredUserAsync(request);

            if (response == false)
            {
                _logger.LogError("AddUnregisteredUserAsync: Failed to add user.");
                return BadRequest("Bad Request");
            }

            _logger.LogInformation($"AddUnregisteredUserAsync: User with ID {request.Id} added successfully.");
            return Ok("OK, user was added to notRegistered");
        }

        [HttpPost("registration")]
        public async Task<ActionResult<string>> RegisterUserAsync([FromBody] CreateUserRequest request)
        {
            if (request.Id < 0)
            {
                _logger.LogError("RegisterUserAsync: Invalid user ID provided.");
                return BadRequest("Id is not valid");
            }

            var response = await _userService.RegisterUserAsync(request);
            if (response == false)
            {
                _logger.LogError("RegisterUserAsync: Failed to register user.");
                return BadRequest("Bad Request");
            }

            _logger.LogInformation($"RegisterUserAsync: User with ID {request.Id} registered successfully.");
            return Ok("Ok, user was registered");
        }
        #endregion

        // PUT
        #region PUT
        [HttpPut("{userId}")]
        public async Task<ActionResult<string>> UpdateUserAsync([FromRoute] long userId, [FromBody] UpdateUserRequest request)
        {
            if (userId < 0)
            {
                _logger.LogError("UpdateUserAsync: Invalid user ID provided.");
                return BadRequest("Id is not valid");
            }
            
            var response = await _userService.UpdateUserAsync(userId, request);
            if (response == false)
            {
                _logger.LogError("UpdateUserAsync: Failed to update user.");
                return BadRequest("Bad Request");
            }

            _logger.LogInformation($"UpdateUserAsync: User with ID {userId} updated successfully.");
            return Ok("User was updated successful");
        }
        #endregion

        // DELETE
        #region DELETE
        [HttpDelete("{userId}")]
        public async Task<ActionResult<string>> DeleteUserAsync([FromRoute] long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("DeleteUserAsync: Invalid user ID provided.");
                return BadRequest("Id is not valid");
            }
            
            var response = await _userService.DeleteUserAsync(userId);

            if (response == false)
            {
                _logger.LogError("DeleteUserAsync: Failed to delete user.");
                return BadRequest("Bad Request");
            }

            _logger.LogInformation($"DeleteUserAsync: User with ID {userId} deleted successfully.");
            return NoContent();
        }
        #endregion
    }
}
