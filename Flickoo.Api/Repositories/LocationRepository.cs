using Flickoo.Api.Data;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly FlickooDbContext _dbContext;
        private readonly ILogger<LocationRepository> _logger;
        public LocationRepository(FlickooDbContext dbContext, ILogger<LocationRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Location?> GetLocationByIdAsync(long id)
        {
            if (id == 0)
            {
                _logger.LogError("GetLocationByIdAsync: Invalid location ID provided.");
                return null;
            }
            var location = await _dbContext.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);
            if (location == null)
            {
                _logger.LogWarning($"GetLocationByIdAsync: Location with ID {id} not found.");
                return null;
            }
            _logger.LogInformation($"GetLocationByIdAsync: Location with ID {id} retrieved successfully.");
            return location;
        }

        public async Task<Location?> GetLocationByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogError("GetLocationByNameAsync: Location name is null or empty.");
                return null;
            }
            var location = await _dbContext.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Name == name);
            if (location == null)
            {
                _logger.LogWarning($"GetLocationByNameAsync: Location with name {name} not found.");
                return null;
            }
            _logger.LogInformation($"GetLocationByNameAsync: Location with name {name} retrieved successfully.");
            return location;
        }

        public async Task<long> AddLocationAsync(Location location)
        {
            if (location == null)
            {
                _logger.LogError("AddLocationAsync: Location is null.");
                return 0;
            }
            await _dbContext.Locations.AddAsync(location);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"AddLocationAsync: Location with ID {location.Id} added successfully.");
            return location.Id;

        }

        public async Task<bool> UpdateLocationAsync(Location location)
        {
            if (location == null)
            {
                _logger.LogError("UpdateLocationAsync: Location is null.");
                return false;
            }
            var locationExist = await _dbContext.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == location.Id);
            if (locationExist == null)
            {
                _logger.LogWarning($"UpdateLocationAsync: Location with ID {location.Id} not found.");
                return false;
            }
            if (string.IsNullOrEmpty(location.Name))
            {
                location.Name = locationExist.Name;
            }
            _dbContext.Locations.Update(location);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"UpdateLocationAsync: Location with ID {location.Id} updated successfully.");
            return true;
        }

        public async Task<bool> DeleteLocationAsync(long id)
        {
            if (id == 0)
            {
                _logger.LogError("DeleteLocationAsync: Invalid location ID provided.");
                return false;
            }
            var locationExist = await _dbContext.Locations
                .AsNoTracking()
                .AnyAsync(l => l.Id == id);
            if (!locationExist)
            {
                _logger.LogWarning($"DeleteLocationAsync: Location with ID {id} not found.");
                return false;
            }
            _dbContext.Locations
                .Where(l => l.Id == id)
                .ExecuteDelete();
            _logger.LogInformation($"DeleteLocationAsync: Location with ID {id} deleted successfully.");
            return true;
        }

        public async Task<bool> CheckLocationExistAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogError("CheckLocationExistAsync: Location name is null or empty.");
                return false;
            }
            var locationExists = await _dbContext.Locations
                .AsNoTracking()
                .AnyAsync(l => l.Name == name);
            return locationExists;
        }
    }
}
