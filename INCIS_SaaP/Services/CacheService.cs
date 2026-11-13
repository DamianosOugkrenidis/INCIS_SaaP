// Author: Damianos Pythagoras Ougkrenidis
//
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace INCIS_SaaP.Services;

/// <summary>
/// Resilient distributed-cache abstraction πάνω από <see cref="IDistributedCache"/>
/// (SQL Server distributed cache σε production, in-memory ως fallback). Χειρίζεται JSON
/// (de)serialization και "καταπίνει" σφάλματα cache ώστε μια αστοχία του cache backend να
/// ΜΗΝ ρίχνει την εφαρμογή — απλώς εκτελείται ξανά το underlying query (graceful degradation).
/// </summary>
public interface ICacheService
{
    /// <summary>Επιστρέφει την τιμή από cache ή <c>null</c> αν δεν υπάρχει.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Αποθηκεύει τιμή στο cache με προαιρετικό absolute TTL (default 5 λεπτά).</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Αφαιρεί ένα key από το cache.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache-aside helper: επιστρέφει το cached value ή εκτελεί τη <paramref name="factory"/>,
    /// αποθηκεύει το αποτέλεσμα και το επιστρέφει.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default) where T : class;
}

/// <inheritdoc />
public class CacheService : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes is null || bytes.Length == 0)
                return null;

            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key {CacheKey}; falling back to source.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? DefaultTtl
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await _cache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {CacheKey}; continuing without caching.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {CacheKey}.", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var value = await factory(cancellationToken);
        if (value is not null)
            await SetAsync(key, value, absoluteExpiration, cancellationToken);

        // Η factory είναι υπεύθυνη να επιστρέφει έγκυρη τιμή (cache-aside contract).
        return value!;
    }
}
