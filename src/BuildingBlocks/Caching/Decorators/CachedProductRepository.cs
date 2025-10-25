using BuildingBlocks.Caching.Redis;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Decorators;

public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _productRepository;
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<CachedProductRepository> _logger;
    private const string CACHE_PREFIX = "product";
    private const int CACHE_DURATION_MINUTES = 30;

    public CachedProductRepository(
        IProductRepository productRepository,
        IRedisCacheService cacheService,
        ILogger<CachedProductRepository> logger)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = $"{CACHE_PREFIX}:{id}";
        
        // Try to get from cache first
        var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey, ct);
        if (cachedProduct != null)
        {
            _logger.LogDebug("✅ Product {ProductId} retrieved from cache", id);
            return cachedProduct;
        }

        // Get from database
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product != null)
        {
            // Cache the result
            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES), ct);
            _logger.LogDebug("💾 Product {ProductId} cached for {Duration} minutes", id, CACHE_DURATION_MINUTES);
        }

        return product;
    }

    public async Task<List<Product>> GetAllAsync(CancellationToken ct = default)
    {
        var cacheKey = $"{CACHE_PREFIX}:all";
        
        // Try to get from cache first
        var cachedProducts = await _cacheService.GetAsync<List<Product>>(cacheKey, ct);
        if (cachedProducts != null)
        {
            _logger.LogDebug("✅ All products retrieved from cache");
            return cachedProducts;
        }

        // Get from database
        var products = await _productRepository.GetAllAsync(ct);
        
        // Cache the result
        await _cacheService.SetAsync(cacheKey, products, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES), ct);
        _logger.LogDebug("💾 All products cached for {Duration} minutes", CACHE_DURATION_MINUTES);

        return products;
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var cacheKey = $"{CACHE_PREFIX}:sku:{sku}";
        
        // Try to get from cache first
        var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey, ct);
        if (cachedProduct != null)
        {
            _logger.LogDebug("✅ Product with SKU {Sku} retrieved from cache", sku);
            return cachedProduct;
        }

        // Get from database
        var product = await _productRepository.GetBySkuAsync(sku, ct);
        if (product != null)
        {
            // Cache the result
            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES), ct);
            _logger.LogDebug("💾 Product with SKU {Sku} cached for {Duration} minutes", sku, CACHE_DURATION_MINUTES);
        }

        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        // Update in database
        await _productRepository.UpdateAsync(product, ct);
        
        // Invalidate cache
        await InvalidateProductCache(product.Id, product.Sku, ct);
        
        _logger.LogDebug("🗑️ Product {ProductId} cache invalidated after update", product.Id);
    }

    public async Task CreateAsync(Product product, CancellationToken ct = default)
    {
        // Create in database
        await _productRepository.CreateAsync(product, ct);
        
        // Invalidate all products cache
        await _cacheService.RemoveByPatternAsync($"{CACHE_PREFIX}:*", ct);
        
        _logger.LogDebug("🗑️ All product caches invalidated after creating product {ProductId}", product.Id);
    }

    private async Task InvalidateProductCache(Guid productId, string sku, CancellationToken ct)
    {
        var keysToRemove = new[]
        {
            $"{CACHE_PREFIX}:{productId}",
            $"{CACHE_PREFIX}:sku:{sku}",
            $"{CACHE_PREFIX}:all"
        };

        foreach (var key in keysToRemove)
        {
            await _cacheService.RemoveAsync(key, ct);
        }
    }
}
