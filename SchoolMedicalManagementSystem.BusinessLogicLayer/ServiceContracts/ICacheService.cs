namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface ICacheService
{
    /// <summary>
    /// Get cached value by key
    /// </summary>
    Task<T> GetAsync<T>(string key);

    /// <summary>
    /// Set cache with expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Remove single cache entry
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove multiple cache entries by prefix pattern
    /// </summary>
    Task RemoveByPrefixAsync(string prefix);

    /// <summary>
    /// Add cache key to tracking set for easy invalidation
    /// </summary>
    Task AddToTrackingSetAsync(string key, string setName = null);

    /// <summary>
    /// Remove all cache entries in a tracking set
    /// </summary>
    Task InvalidateTrackingSetAsync(string setName);

    /// <summary>
    /// Generate cache key from multiple parts
    /// </summary>
    string GenerateCacheKey(params string[] keyParts);

    /// <summary>
    /// Check if cache key exists
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Get cache keys by pattern (for debugging/monitoring)
    /// </summary>
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);
}