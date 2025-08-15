using AutoDocOps.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using StackExchange.Redis;
using AutoDocOps.Infrastructure.Logging;

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
                var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.CacheMiss(key);
                return null;
            }

            _logger.CacheHit(key);
            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.CacheGetError(key, ex);
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

                await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken).ConfigureAwait(false);
            _logger.CacheSet(key, expiration);
        }
        catch (Exception ex)
        {
            _logger.CacheSetError(key, ex);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
                await _distributedCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            _logger.CacheRemove(key);
        }
        catch (Exception ex)
        {
            _logger.CacheRemoveError(key, ex);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connectionMultiplexer == null)
            {
                _logger.CachePatternNoConnection(pattern);
                return;
            }

            var database = _connectionMultiplexer.GetDatabase();
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
            
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
            {
                    await database.KeyDeleteAsync(keys).ConfigureAwait(false);
                _logger.CachePatternRemoved(keys.Length, pattern);
            }
            else
            {
                _logger.CachePatternNone(pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.CachePatternError(pattern, ex);
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
            _logger.CacheGetSyncError(key, ex);
            value = null;
            return false;
        }
    }
}

