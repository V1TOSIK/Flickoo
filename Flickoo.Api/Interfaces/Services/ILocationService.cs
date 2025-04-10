using Flickoo.Api.DTOs.Location.Create;
using Flickoo.Api.DTOs.Location.Get;
using Flickoo.Api.DTOs.Location.Update;

namespace Flickoo.Api.Interfaces.Services
{
    public interface ILocationService
    {
        Task<GetLocationNameResponse?> GetLocationByIdAsync(long locationId);
        Task<GetLocationIdResponse?> GetLocationByNameAsync(GetLocationByNameRequest request);
        Task<CreatedLocationIdResponse> AddLocationAsync(CreateLocationRequest request);
        Task<bool> UpdateLocationAsync(long locationId, UpdateLocationRequest request);
        Task<bool> DeleteLocationAsync(long locationId);
        Task<bool> CheckLocationExistAsync(GetLocationByNameRequest request);
    }
}
