using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(long id);
        Task<User?> AddUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(long id);
        Task<bool> CheckUserExistAsync(long id);
        Task<bool> CheckUserRegistrationAsync(long id);
    }
}
