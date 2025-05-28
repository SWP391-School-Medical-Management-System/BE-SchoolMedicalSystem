using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly IConfiguration _configuration;

    // Default cache settings
    private readonly TimeSpan _defaultExpiration;
    private readonly string _defaultTrackingSet;

    // JSON serialization options
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(
        IDistributedCache cache,
        ILogger<CacheService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        _configuration = configuration;

        // Load cache settings from configuration
        _defaultExpiration = TimeSpan.FromMinutes(
            _configuration.GetValue<int>("Cache:DefaultExpirationMinutes", 15));

        _defaultTrackingSet = _configuration.GetValue<string>("Cache:DefaultTrackingSet", "cache_keys");

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T> GetAsync<T>(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Cache key is null or empty");
                return default(T);
            }

            var cachedValue = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default(T);
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for cache key: {Key}", key);
            // Remove corrupted cache entry
            await RemoveAsync(key);
            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return default(T);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Cannot set cache with null or empty key");
                return;
            }

            if (value == null)
            {
                _logger.LogWarning("Cannot set cache with null value for key: {Key}", key);
                return;
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
            };

            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            await _cache.SetStringAsync(key, serializedValue, options);

            // Add to default tracking set
            await AddToTrackingSetAsync(key);

            _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}",
                key, options.AbsoluteExpirationRelativeToNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Cannot remove cache with null or empty key");
                return;
            }

            await _cache.RemoveAsync(key);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        try
        {
            if (string.IsNullOrEmpty(prefix))
            {
                _logger.LogWarning("Cannot remove cache with null or empty prefix");
                return;
            }

            var trackingSetKeys = await GetTrackingSetKeysAsync(_defaultTrackingSet);
            var keysToRemove = trackingSetKeys.Where(key => key.StartsWith(prefix)).ToList();

            if (!keysToRemove.Any())
            {
                _logger.LogDebug("No cache keys found with prefix: {Prefix}", prefix);
                return;
            }

            // Remove cache entries
            var removeTasks = keysToRemove.Select(RemoveAsync);
            await Task.WhenAll(removeTasks);

            // Update tracking set
            await RemoveKeysFromTrackingSetAsync(keysToRemove, _defaultTrackingSet);

            _logger.LogDebug("Removed {Count} cache entries with prefix: {Prefix}",
                keysToRemove.Count, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by prefix: {Prefix}", prefix);
        }
    }

    public async Task AddToTrackingSetAsync(string key, string setName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Cannot add null or empty key to tracking set");
                return;
            }

            setName ??= _defaultTrackingSet;

            var existingKeys = await GetTrackingSetKeysAsync(setName);
            var updatedKeys = existingKeys.Union(new[] { key }).ToHashSet();

            await SetTrackingSetKeysAsync(setName, updatedKeys);

            _logger.LogDebug("Added key {Key} to tracking set {SetName}", key, setName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding key {Key} to tracking set {SetName}", key, setName);
        }
    }

    public async Task InvalidateTrackingSetAsync(string setName)
    {
        try
        {
            if (string.IsNullOrEmpty(setName))
            {
                setName = _defaultTrackingSet;
            }

            var keys = await GetTrackingSetKeysAsync(setName);

            if (!keys.Any())
            {
                _logger.LogDebug("No keys found in tracking set: {SetName}", setName);
                return;
            }

            // Remove all cache entries
            var removeTasks = keys.Select(RemoveAsync);
            await Task.WhenAll(removeTasks);

            // Clear tracking set
            await RemoveAsync(setName);

            _logger.LogDebug("Invalidated {Count} cache entries from tracking set: {SetName}",
                keys.Count(), setName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tracking set: {SetName}", setName);
        }
    }

    public string GenerateCacheKey(params string[] keyParts)
    {
        if (keyParts == null || !keyParts.Any())
        {
            throw new ArgumentException("Key parts cannot be null or empty", nameof(keyParts));
        }

        // Filter out null/empty parts and join with colon
        var cleanParts = keyParts
            .Where(part => !string.IsNullOrEmpty(part))
            .Select(part => part.Replace(":", "_").Replace(" ", "_")); // Sanitize separators

        return string.Join(":", cleanParts);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            var value = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return Enumerable.Empty<string>();
            }

            var allKeys = await GetTrackingSetKeysAsync(_defaultTrackingSet);

            // Simple pattern matching (could be enhanced with regex if needed)
            return allKeys.Where(key => key.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    #region Private Helper Methods

    private async Task<HashSet<string>> GetTrackingSetKeysAsync(string setName)
    {
        try
        {
            var keysJson = await _cache.GetStringAsync(setName);

            if (string.IsNullOrEmpty(keysJson))
            {
                return new HashSet<string>();
            }

            return JsonSerializer.Deserialize<HashSet<string>>(keysJson, _jsonOptions)
                   ?? new HashSet<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracking set keys for: {SetName}", setName);
            return new HashSet<string>();
        }
    }

    private async Task SetTrackingSetKeysAsync(string setName, HashSet<string> keys)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Tracking sets live longer
            };

            var keysJson = JsonSerializer.Serialize(keys, _jsonOptions);
            await _cache.SetStringAsync(setName, keysJson, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tracking set keys for: {SetName}", setName);
        }
    }

    private async Task RemoveKeysFromTrackingSetAsync(IEnumerable<string> keysToRemove, string setName)
    {
        try
        {
            var existingKeys = await GetTrackingSetKeysAsync(setName);
            var updatedKeys = existingKeys.Except(keysToRemove).ToHashSet();

            await SetTrackingSetKeysAsync(setName, updatedKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys from tracking set: {SetName}", setName);
        }
    }

    #endregion
}