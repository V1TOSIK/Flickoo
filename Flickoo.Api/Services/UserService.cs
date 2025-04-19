using Flickoo.Api.DTOs.User.Create;
using Flickoo.Api.DTOs.User.Get;
using Flickoo.Api.DTOs.User.Update;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Flickoo.Api.Interfaces.Services;

namespace Flickoo.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ILogger<UserService> _logger;
        public UserService(IUserRepository userRepository,
            ILocationRepository locationRepository,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _locationRepository = locationRepository;
            _logger = logger;
        }

        public async Task<GetUserResponse?> GetUserByIdAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetUserById: Invalid user ID provided.");
                return null;
            }
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"GetUserById: User with ID {userId} not found.");
                return null;
            }
            if (user.Location == null)
            {
                _logger.LogWarning($"GetUserById: User with ID {userId} has no location.");
                return null;
            }
            _logger.LogInformation($"GetUserById: User with ID {userId} retrieved successfully.");
            var response = new GetUserResponse
            {
                Id = user.Id,
                Nickname = user.Nickname,
                Username = user.Username,
                LocationName = user.Location.Name,
                Registered = user.Registered,
            };
            return response;
        }

        public async Task<bool> AddUnregisteredUserAsync(CreateUserRequest request)
        {
            if (request == null)
            {
                _logger.LogError("AddUnregisteredUser: User object is null.");
                return false;
            }
            var existingUser = await _userRepository.GetUserByIdAsync(request.Id);

            var location = await _locationRepository.GetLocationByNameAsync(request.LocationName);
            long locationId = -1;
            if (location == null)
            {
                _logger.LogError($"AddUnregisteredUser: Location with name {request.LocationName} not found.");
                locationId = await _locationRepository.AddLocationAsync(new Location
                {
                    Name = request.LocationName
                });
                _logger.LogInformation($"AddUnregisteredUser: Location with name {request.LocationName} added successfully.");
            }
            else
                locationId = location.Id;

            var user = new User
            {
                Id = request.Id,
                Username = request.Username,
                Nickname = request.Nickname,
                LocationId = locationId,
                Registered = false
            };
            if (existingUser == null)
            {
                var newUser = await _userRepository.AddUserAsync(user);
                if (newUser != null)
                {
                    _logger.LogInformation($"AddUnregisteredUser: User with ID {newUser.Id} added successfully.");
                    return true;
                }
                else
                {
                    _logger.LogError("AddUnregisteredUser: Failed to add user.");
                    return false;
                }
            }
            else
            {
                _logger.LogWarning($"AddUnregisteredUser: User with ID {request.Id} already exists.");
                return false;
            }

        }

        public async Task<bool> RegisterUserAsync(CreateUserRequest request)
        {
            if (request == null)
            {
                _logger.LogError("RegisterUser: User object is null.");
                return false;
            }

            var location = await _locationRepository.GetLocationByNameAsync(request.LocationName);
            long locationId = -1;
            if (location == null)
            {
                _logger.LogError($"AddUnregisteredUser: Location with name {request.LocationName} not found.");
                locationId = await _locationRepository.AddLocationAsync(new Location
                {
                    Name = request.LocationName
                });
                _logger.LogInformation($"AddUnregisteredUser: Location with name {request.LocationName} added successfully.");
            }
            else
                locationId = location.Id;

            var user = new User
            {
                Id = request.Id,
                Nickname = request.Nickname,
                Username = request.Username,
                LocationId = locationId,
                Registered = true
            };

            var existingUser = await _userRepository.GetUserByIdAsync(request.Id);
            if (existingUser != null)
            {
                var registerCheck = await _userRepository.CheckUserRegistrationAsync(request.Id);
                if (registerCheck)
                {
                    _logger.LogWarning($"RegisterUser: User with ID {request.Id} already registered.");
                    return false;
                }
                else
                {
                    _logger.LogInformation($"RegisterUser: User with ID {request.Id} is not registered.");
                    _logger.LogInformation($"RegisterUser: User with ID {request.Id} in update process .....");
                    return await _userRepository.UpdateUserAsync(user);
                }
            }
            else
            {
                var newUser = await _userRepository.AddUserAsync(user);
                if (newUser != null)
                {
                    _logger.LogInformation($"RegisterUser: User with ID {newUser.Id} added successfully.");
                    return true;
                }
                else
                {
                    _logger.LogError("RegisterUser: Failed to add user.");
                    return false;
                }
            }
        }

        public async Task<bool> UpdateUserAsync(long userId, UpdateUserRequest request)
        {
            if (request == null)
            {
                _logger.LogError("UpdateUser: User object is null.");
                return false;
            }

            if (userId < 0)
            {
                _logger.LogError("UpdateUser: Invalid user ID provided.");
                return false;
            }

            long locationId = -1;
            if (!string.IsNullOrEmpty(request.LocationName))
            {
                var location = await _locationRepository.GetLocationByNameAsync(request.LocationName);
                if (location == null)
                {
                    _logger.LogError($"AddUnregisteredUser: Location with name {request.LocationName} not found.");
                    locationId = await _locationRepository.AddLocationAsync(new Location
                    {
                        Name = request.LocationName
                    });
                    _logger.LogInformation($"AddUnregisteredUser: Location with name {request.LocationName} added successfully.");
                }
                else
                    locationId = location.Id;
            }


            var user = new User
            {
                Id = userId,
                Username = request.Username ?? "",
                Nickname = request.Nickname ?? "",
                LocationId = locationId,
                Registered = request.Registered
            };
            return await _userRepository.UpdateUserAsync(user);
        }

        public async Task<bool> DeleteUserAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("DeleteUser: Invalid user ID provided.");
                return false;
            }
            return await _userRepository.DeleteUserAsync(userId);
        }
    }
}