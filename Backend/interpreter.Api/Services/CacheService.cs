using Microsoft.Extensions.Caching.Memory;

namespace interpreter.Api.Services;

/// <summary>
/// Simple and easy-to-use cache service using IMemoryCache
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _defaultExpiration = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Get value from cache. Returns default(T) if not found.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue<T>(key, out var value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    /// <summary>
    /// Set value in cache with optional expiration.
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expiration ?? _defaultExpiration);

        _cache.Set(key, value, cacheOptions);
        _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", 
            key, expiration ?? _defaultExpiration);
    }

    /// <summary>
    /// Remove value from cache.
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Removed cache for key: {Key}", key);
    }

    /// <summary>
    /// Try to get value from cache. Returns true if found.
    /// </summary>
    public bool TryGetValue<T>(string key, out T? value)
    {
        var result = _cache.TryGetValue(key, out value);
        
        if (result)
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
        }
        else
        {
            _logger.LogDebug("Cache miss for key: {Key}", key);
        }

        return result;
    }

    /// <summary>
    /// Get or create cached value. If key doesn't exist, factory function is called.
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue<T>(key, out var cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue!;
        }

        _logger.LogDebug("Cache miss for key: {Key}, creating new value", key);
        var value = await factory();
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expiration ?? _defaultExpiration);

        _cache.Set(key, value, cacheOptions);
        _logger.LogDebug("Cached new value for key: {Key}", key);
        
        return value;
    }

    /// <summary>
    /// Get or create cached value synchronously. If key doesn't exist, factory function is called.
    /// </summary>
    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration;
            _logger.LogDebug("Creating and caching value for key: {Key}", key);
            return factory();
        })!;
    }

    /// <summary>
    /// Clear all cache entries (Note: not supported by IMemoryCache by default)
    /// </summary>
    public void Clear()
    {
        _logger.LogWarning("Clear method called but IMemoryCache doesn't support clearing all entries");
    }
}

