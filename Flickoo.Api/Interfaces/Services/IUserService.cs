using Flickoo.Api.DTOs.User.Create;
using Flickoo.Api.DTOs.User.Get;
using Flickoo.Api.DTOs.User.Update;

namespace Flickoo.Api.Interfaces.Services
{
    public interface IUserService
    {
        Task<GetUserResponse?> GetUserByIdAsync(long userId);
        Task<bool> AddUnregisteredUserAsync(CreateUserRequest request);
        Task<bool> RegisterUserAsync(CreateUserRequest request);
        Task<bool> UpdateUserAsync(long userId, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(long userId);
        Task<bool> CheckUserRegistrationAsync(long userId);
    }
}
