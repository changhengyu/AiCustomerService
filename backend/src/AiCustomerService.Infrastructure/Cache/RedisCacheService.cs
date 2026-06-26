using AiCustomerService.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AiCustomerService.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IMemoryCache? _memoryCache;

    public RedisCacheService(
        ILogger<RedisCacheService> logger,
        IConnectionMultiplexer? redis = null,
        IMemoryCache? memoryCache = null)
    {
        _redis = redis;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            var redisValue = await db.StringGetAsync(key);
            return redisValue.HasValue ? redisValue.ToString() : null;
        }

        if (_memoryCache != null && _memoryCache.TryGetValue(key, out var cached))
            return cached;
        return null;
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
            return;
        }

        _memoryCache?.Set(key, value, expiry ?? TimeSpan.FromHours(1));
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
            return;
        }

        _memoryCache?.Remove(key);
        await Task.CompletedTask;
    }
}

/// <summary>
/// IMemoryCache 接口的简化定义（避免引入整个包）
/// </summary>
public interface IMemoryCache
{
    bool TryGetValue(object key, out string? value);
    void Set(object key, string value, TimeSpan expiry);
    void Remove(object key);
}