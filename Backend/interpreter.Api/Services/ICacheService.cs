namespace interpreter.Api.Services;

/// <summary>
/// Service for managing application-wide caching - Simple and easy to use
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get value from cache. Returns default(T) if not found.
    /// </summary>
    T? Get<T>(string key);
    
    /// <summary>
    /// Set value in cache with optional expiration.
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Remove value from cache.
    /// </summary>
    void Remove(string key);
    
    /// <summary>
    /// Try to get value from cache. Returns true if found.
    /// </summary>
    bool TryGetValue<T>(string key, out T? value);
    
    /// <summary>
    /// Get or create cached value asynchronously. If key doesn't exist, factory function is called.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    
    /// <summary>
    /// Get or create cached value synchronously. If key doesn't exist, factory function is called.
    /// </summary>
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);
    
    /// <summary>
    /// Clear all cache entries.
    /// </summary>
    void Clear();
}


