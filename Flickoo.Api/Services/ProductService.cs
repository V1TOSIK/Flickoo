using Flickoo.Api.DTOs.Product.Create;
using Flickoo.Api.DTOs.Product.Get;
using Flickoo.Api.DTOs.Product.Update;
using Flickoo.Api.DTOs.User.Get;
using Flickoo.Api.Entities;
using Flickoo.Api.Interfaces.Repositories;
using Flickoo.Api.Interfaces.Services;
using Flickoo.Api.ValueObjects;
using Telegram.Bot.Types;

namespace Flickoo.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMediaService _mediaService;
        private readonly ILogger<ProductService> _logger;
        public ProductService(IProductRepository productRepository,
            IUserRepository userRepository,
            ILocationRepository locationRepository,
            ICategoryRepository categoryRepository,
            IMediaService mediaService,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _userRepository = userRepository;
            _locationRepository = locationRepository;
            _categoryRepository = categoryRepository;
            _mediaService = mediaService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetProductResponse>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetAllProductsAsync();

            if (products == null || !products.Any())
            {
                _logger.LogWarning("GetAllProducts: No products found.");
                return Enumerable.Empty<GetProductResponse>();
            }
            
            _logger.LogInformation("GetAllProducts: Products retrieved successfully.");
            
            var response = products.Select(p => new GetProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                PriceAmount = p.Price.Amount,
                PriceCurrency = p.Price.Currency,
                LocationName = p.Location?.Name ?? string.Empty,
                Description = p.Description,
                MediaUrls = p.ProductMedias?
                    .Select(pm => pm?.Url)
                    .Where(url => url is not null)
                    .ToList() ?? []
            }).ToList();
            
            return response;
        }

        public async Task<GetProductResponse?> GetProductByIdAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogError("GetProductById: Invalid product ID provided.");
                return null;
            }
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning($"GetProductById: Product with ID {productId} not found.");
                return null;
            }
            _logger.LogInformation($"GetProductById: Product with ID {productId} retrieved successfully.");
            var response = new GetProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                PriceAmount = product.Price.Amount,
                PriceCurrency = product.Price.Currency,
                LocationName = product.Location?.Name ?? string.Empty,
                Description = product.Description,
                MediaUrls = product.ProductMedias?
                    .Select(pm => pm?.Url)
                    .Where(url => url is not null)
                    .ToList() ?? []
            };
            return response;
        }

        public async Task<IEnumerable<GetProductResponse>> GetProductsByUserIdAsync(long userId)
        {
            if (userId < 0)
            {
                _logger.LogError("GetProductsByUserId: Invalid user ID provided.");
                return Enumerable.Empty<GetProductResponse>();
            }
            var products = await _productRepository.GetProductsByUserIdAsync(userId);
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"GetProductsByUserId: No products found for user ID {userId}.");
                return Enumerable.Empty<GetProductResponse>();
            }
            _logger.LogInformation($"GetProductsByUserId: Products for user ID {userId} retrieved successfully.");
            var response = products.Select(p => new GetProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                PriceAmount = p.Price.Amount,
                PriceCurrency = p.Price.Currency,
                LocationName = p.Location?.Name ?? string.Empty,
                Description = p.Description,
                MediaUrls = p.ProductMedias?
                    .Select(pm => pm?.Url)
                    .Where(url => url is not null)
                    .ToList() ?? []
            }).ToList();
            return response;
        }

        public async Task<IEnumerable<GetProductResponse>> GetProductsByCategoryIdAsync(long categoryId)
        {
            if (categoryId < 0)
            {
                _logger.LogError("GetProductsByCategoryId: Invalid category ID provided.");
                return Enumerable.Empty<GetProductResponse>();
            }
            var products = await _productRepository.GetProductsByCategoryIdAsync(categoryId);
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"GetProductsByCategoryId: No products found for category ID {categoryId}.");
                return Enumerable.Empty<GetProductResponse>();
            }
            _logger.LogInformation($"GetProductsByCategoryId: Products for category ID {categoryId} retrieved successfully.");
            var response = products.Select(p => new GetProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                PriceAmount = p.Price.Amount,
                PriceCurrency = p.Price.Currency,
                LocationName = p.Location?.Name ?? string.Empty,
                Description = p.Description,
                MediaUrls = p.ProductMedias?
                    .Select(pm => pm?.Url)
                    .Where(url => url is not null)
                    .ToList() ?? []
            }).ToList();
            return response;
        }

        public async Task<GetUserResponse?> GetSellerByProductIdAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogError("GetSellerByProductId: Invalid product ID provided.");
                return null;
            }

            var product = await _productRepository.GetProductByIdAsync(productId);

            if (product == null)
            {
                _logger.LogWarning($"GetSellerByProductId: Product with ID {productId} not found.");
                return null;
            }

            var user = await _userRepository.GetUserByIdAsync(product.UserId);

            if (user == null)
            {
                _logger.LogWarning($"GetSellerByProductId: User with ID {product.UserId} not found.");
                return null;
            }

            var response = new GetUserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                LocationName = user.Location?.Name ?? string.Empty
            };
            _logger.LogInformation($"GetSellerByProductId: Seller for product ID {productId} retrieved successfully.");
            return response;
        }

        public async Task<long> AddProductAsync(CreateProductRequest request)
        {
            if (request == null)
            {
                _logger.LogError("AddProduct: Product object is null.");
                return -1;
            }

            var user = await _userRepository.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning($"AddProduct: User with ID {request.UserId} not found.");
                return -1;
            }
            
            var location = await _locationRepository.GetLocationByNameAsync(user.Location?.Name ?? "Unknown");
            if (location == null)
            {
                _logger.LogWarning($"AddProduct: Location with name {user?.Location?.Name} not found.");
                return -1;
            }

            var category = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId);
            if (category == null)
            {
                _logger.LogWarning($"AddProduct: Category with ID {request.CategoryId} not found.");
                return -1;
            }


            var product = new Product
            {
                Name = request.Name,
                Price = new Price(request.PriceAmount, request.PriceCurrency) { },
                Description = request.Description,
                UserId = request.UserId,
                LocationId = location.Id,
                CategoryId = request.CategoryId,
                ProductMedias = [],
                User = user,
                Location = location,
                Category = category,
            };
            var savedProduct = await _productRepository.AddProductAsync(product);
            if (savedProduct == null)
            {
                _logger.LogError("AddProduct: Failed to save product.");
                return -1;
            }
            _logger.LogInformation($"AddProduct: Product with ID {savedProduct.Id} saved successfully.");
            return savedProduct.Id;
        }

        public async Task<bool> UpdateProductAsync(long productId, UpdateProductRequest request)
        {
            if (productId < 0)
            {
                _logger.LogError("UpdateProduct: Invalid product ID provided.");
                return false;
            }
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning($"UpdateProduct: Product with ID {productId} not found.");
                return false;
            }
            if (request == null)
            {
                _logger.LogError("UpdateProduct: Product object is null.");
                return false;
            }
            product.Name = request.Name ?? string.Empty;
            product.Price = new Price(
                request.PriceAmount,
                request.PriceCurrency ?? string.Empty);
            product.Description = request.Description ?? string.Empty;
            var updatedProduct = await _productRepository.UpdateProductAsync(product);
            if (!updatedProduct)
            {
                _logger.LogError("UpdateProduct: Failed to update product.");
                return false;
            }
            _logger.LogInformation($"UpdateProduct: Product with ID {productId} updated successfully.");
            return true;
        }

        public async Task<bool> DeleteProductAsync(long productId)
        {
            if (productId < 0)
            {
                _logger.LogError("DeleteProduct: Invalid product ID provided.");
                return false;
            }

            var deleteMediaResult = await _mediaService.DeleteMediaAsync(productId);
            
            if (!deleteMediaResult)
            {
                _logger.LogError($"DeleteProduct: Failed to delete media for product ID {productId}.");
                return false;
            
            }
            _logger.LogInformation($"DeleteProduct: Product with ID {productId} deleted successfully.");
            
            var deleteProductResult = await _productRepository.DeleteProductAsync(productId);
            
            if (!deleteProductResult)
            {
                _logger.LogError($"DeleteProduct: Failed to delete product with ID {productId}.");
                return false;
            }
            return true;
        }
    }
}
