using Flickoo.Api.Entities;

namespace Flickoo.Api.Interfaces.Repositories
{
    public interface ILocationRepository
    {
        Task<Location?> GetLocationByIdAsync(long id);
        Task<Location?> GetLocationByNameAsync(string name);
        Task<long> AddLocationAsync(Location location);
        Task<bool> UpdateLocationAsync(Location location);
        Task<bool> DeleteLocationAsync(long id);
        Task<bool> CheckLocationExistAsync(string name);
    }
}
