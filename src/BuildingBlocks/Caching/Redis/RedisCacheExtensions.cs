using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace BuildingBlocks.Caching.Redis;

public static class RedisCacheExtensions
{
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") 
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "EcommerceCase";
        });

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<RedisCacheExtensions>>();
            logger.LogInformation("🔗 Connecting to Redis: {ConnectionString}", redisConnectionString);
            
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });

        services.AddScoped<IRedisCacheService, RedisCacheService>();

        return services;
    }
}

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<TimeSpan?> GetExpirationAsync(string key, CancellationToken cancellationToken = default);
}

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🔍 Getting cache key: {Key}", key);
            
            var value = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("❌ Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("✅ Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting cache key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("💾 Setting cache key: {Key} with expiration: {Expiration}", key, expiration);
            
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Default 1 hour
            }

            var serializedValue = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            
            _logger.LogDebug("✅ Cache set successfully for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error setting cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🗑️ Removing cache key: {Key}", key);
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("✅ Cache key removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🗑️ Removing cache keys by pattern: {Pattern}", pattern);
            
            var database = _connectionMultiplexer.GetDatabase();
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            
            var keys = server.Keys(pattern: pattern);
            var keyArray = keys.ToArray();
            
            if (keyArray.Length > 0)
            {
                await database.KeyDeleteAsync(keyArray);
                _logger.LogDebug("✅ Removed {Count} cache keys matching pattern: {Pattern}", keyArray.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task<TimeSpan?> GetExpirationAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var timeToLive = await database.KeyTimeToLiveAsync(key);
            return timeToLive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting cache key expiration: {Key}", key);
            return null;
        }
    }
}
