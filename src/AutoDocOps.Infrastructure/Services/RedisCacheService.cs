using AutoDocOps.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using StackExchange.Redis;

namespace AutoDocOps.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer? _connectionMultiplexer;

    public RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger, IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connectionMultiplexer == null)
            {
                _logger.LogWarning("RemoveByPatternAsync requires IConnectionMultiplexer to be injected. Falling back to no-op. Pattern: {Pattern}", pattern);
                return;
            }

            var database = _connectionMultiplexer.GetDatabase();
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
            
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
            {
                await database.KeyDeleteAsync(keys);
                _logger.LogInformation("Removed {Count} keys matching pattern: {Pattern}", keys.Length, pattern);
            }
            else
            {
                _logger.LogDebug("No keys found matching pattern: {Pattern}", pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public bool TryGet<T>(string key, out T? value) where T : class
    {
        try
        {
            var cachedValue = _distributedCache.GetString(key);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                value = null;
                return false;
            }

            value = JsonSerializer.Deserialize<T>(cachedValue);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value synchronously for key: {Key}", key);
            value = null;
            return false;
        }
    }
}

