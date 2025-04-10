using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FlickooDbContext _dbContext;
        private readonly ILogger<UserRepository> _logger;
        public UserRepository(FlickooDbContext dbContext, ILogger<UserRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(long id)
        {
            if (id == 0)
            {
                _logger.LogError("GetUserByIdAsync: Invalid user ID provided.");
                return null;
            }

            var users = await _dbContext.Users
                .AsNoTracking()
                .Include(u => u.Location)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (users == null)
            {
                _logger.LogWarning($"GetUserByIdAsync: User with ID {id} not found.");
                return null;
            }
            _logger.LogInformation($"GetUserByIdAsync: User with ID {id} retrieved successfully.");

            return users;
        }

        public async Task<User?> AddUserAsync(User user)
        {
            if (user == null)
                return null;

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            if (user == null)
            {
                _logger.LogError("UpdateUserAsync: User object is null.");
                return false;
            }
            var existingUser = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existingUser == null)
            {
                _logger.LogWarning($"UpdateUserAsync: User with ID {user.Id} not found.");
                return false;
            }
            if(string.IsNullOrEmpty(user.Username))
                user.Username = existingUser.Username;
            if (existingUser.Registered == true)
                user.Registered = true;

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"UpdateUserAsync: User with ID {user.Id} updated successfully.");
            return true;
        }

        public async Task<bool> DeleteUserAsync(long id)
        {
            if (id == 0)
            {
                _logger.LogError("DeleteUserAsync: Invalid user ID provided.");
                return false;
            }
            var userExist = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == id);
            if (userExist)
            {
                await _dbContext.Users
                    .Where(u => u.Id == id)
                    .ExecuteDeleteAsync();
                _logger.LogInformation($"DeleteUserAsync: User with ID {id} deleted successfully.");
                return true;
            }
            else
            {
                _logger.LogWarning($"DeleteUserAsync: User with ID {id} not found.");
                return false;
            }
        }

        public async Task<bool> CheckUserExistAsync(long id)
        {
            if (id == 0)
            {
                _logger.LogError("CheckUserExistAsync: Invalid user ID provided.");
                return false;
            }
            return await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == id);
        }

        public async Task<bool> CheckUserRegistrationAsync(long id)
        {
            if (id == 0)
            {
                _logger.LogError("CheckUserRegistrationAsync: Invalid user ID provided.");
                return false;
            }
            return await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == id && u.Registered);
        }
    }
}
