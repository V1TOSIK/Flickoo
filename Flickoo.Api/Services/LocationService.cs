using Flickoo.Api.DTOs.Location.Create;
using Flickoo.Api.DTOs.Location.Get;
using Flickoo.Api.DTOs.Location.Update;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Flickoo.Api.Interfaces.Services;

namespace Flickoo.Api.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ILogger<LocationService> _logger;
        public LocationService(ILocationRepository locationRepository, ILogger<LocationService> logger)
        {
            _locationRepository = locationRepository;
            _logger = logger;
        }

        public async Task<GetLocationNameResponse?> GetLocationByIdAsync(long locationId)
        {
            if (locationId == 0)
            {
                _logger.LogError("GetLocationByIdAsync: Invalid location ID provided.");
                return null;
            }
            var location = await _locationRepository.GetLocationByIdAsync(locationId);
            if (location == null)
            {
                _logger.LogWarning($"GetLocationByIdAsync: Location with ID {locationId} not found.");
                return null;
            }
            _logger.LogInformation($"GetLocationByIdAsync: Location with ID {locationId} retrieved successfully.");
            var response = new GetLocationNameResponse
            {
                Name = location.Name,
            };
            return response;
        }

        public async Task<GetLocationIdResponse?> GetLocationByNameAsync(GetLocationByNameRequest request)
        {
            if (request == null)
            {
                _logger.LogError("GetLocationByNameAsync: Request is null.");
                return null;
            }
            var location = await _locationRepository.GetLocationByNameAsync(request.Name);
            if (location == null)
            {
                _logger.LogWarning($"GetLocationByNameAsync: Location with name {request.Name} not found.");
                return null;
            }
            _logger.LogInformation($"GetLocationByNameAsync: Location with name {request.Name} retrieved successfully.");
            var response = new GetLocationIdResponse
            {
                Id = location.Id,
            };
            return response;
        }

        public async Task<CreatedLocationIdResponse> AddLocationAsync(CreateLocationRequest request)
        {
            var location = new Location
            {
                Name = request.Name
            };
            var locationId = await _locationRepository.AddLocationAsync(location);

            _logger.LogInformation($"AddLocationAsync: Location with ID {locationId} added successfully.");
            var response = new CreatedLocationIdResponse
            {
                Id = locationId
            };
            return response;
        }

        public async Task<bool> UpdateLocationAsync(long locationId, UpdateLocationRequest request)
        {
            if (locationId == 0 || request == null)
            {
                _logger.LogError("UpdateLocationAsync: Invalid location ID or request provided.");
                return false;
            }
            var location = new Location
            {
                Id = locationId,
                Name = request.Name ?? string.Empty,
            };
            var response = await _locationRepository.UpdateLocationAsync(location);
            if (!response)
            {
                _logger.LogWarning($"UpdateLocationAsync: Location with ID {locationId} not found.");
                return false;
            }
            _logger.LogInformation($"UpdateLocationAsync: Location with ID {locationId} updated successfully.");
            return true;
        }

        public async Task<bool> DeleteLocationAsync(long locationId)
        {
            if (locationId == 0)
            {
                _logger.LogError("DeleteLocationAsync: Invalid location ID provided.");
                return false;
            }
            var response = await _locationRepository.DeleteLocationAsync(locationId);
            if (!response)
            {
                _logger.LogWarning($"DeleteLocationAsync: Location with ID {locationId} not found.");
                return false;
            }
            _logger.LogInformation($"DeleteLocationAsync: Location with ID {locationId} deleted successfully.");
            return true;
        }

        public async Task<bool> CheckLocationExistAsync(GetLocationByNameRequest request)
        {
            if (request == null)
            {
                _logger.LogError("CheckLocationExistAsync: Request is null.");
                return false;
            }
            var response = await _locationRepository.CheckLocationExistAsync(request.Name);
            if (!response)
            {
                _logger.LogWarning($"CheckLocationExistAsync: Location with name {request.Name} not found.");
                return false;
            }
            _logger.LogInformation($"CheckLocationExistAsync: Location with name {request.Name} exists.");
            return true;
        }
    }
}
