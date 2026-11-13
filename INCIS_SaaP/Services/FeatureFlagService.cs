// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using System.Collections.Concurrent;

namespace INCIS_SaaP.Services
{
    /// <summary>
    /// Υπηρεσία διαχείρισης feature flags
    /// </summary>
    public interface IFeatureFlagService
    {
        bool IsEnabled(string featureKey);
        void SetEnabled(string featureKey, bool enabled);
        Dictionary<string, bool> GetAll();
    }

    /// <summary>
    /// Υλοποίηση feature flags με DB persistence + in-memory cache
    /// </summary>
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, bool> _cache = new();
        private bool _initialized;

        public FeatureFlagService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            // Load from configuration first (defaults)
            foreach (var section in _configuration.GetSection("FeatureFlags").GetChildren())
            {
                if (bool.TryParse(section.Value, out bool enabled))
                {
                    _cache[section.Key] = enabled;
                }
            }

            // Then override with DB values
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<INCISDbContext>();

                var dbFlags = context.Set<FeatureFlag>().ToList();
                foreach (var flag in dbFlags)
                {
                    _cache[flag.Key] = flag.IsEnabled;
                }
            }
            catch
            {
                // DB not available yet (during startup), use config values only
            }

            _initialized = true;
        }

        public bool IsEnabled(string featureKey)
        {
            EnsureInitialized();
            return _cache.GetOrAdd(featureKey, key =>
                _configuration.GetValue<bool>($"FeatureFlags:{featureKey}", false));
        }

        public void SetEnabled(string featureKey, bool enabled)
        {
            EnsureInitialized();
            _cache[featureKey] = enabled;

            // Persist to DB
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<INCISDbContext>();

                var existing = context.Set<FeatureFlag>().FirstOrDefault(f => f.Key == featureKey);
                if (existing != null)
                {
                    existing.IsEnabled = enabled;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    context.Set<FeatureFlag>().Add(new FeatureFlag
                    {
                        Key = featureKey,
                        IsEnabled = enabled,
                        Description = featureKey,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                context.SaveChanges();
            }
            catch
            {
                // Best effort - cache is already updated
            }
        }

        public Dictionary<string, bool> GetAll()
        {
            EnsureInitialized();
            return new Dictionary<string, bool>(_cache);
        }
    }

    /// <summary>
    /// Feature flag entity for DB persistence
    /// </summary>
    public class FeatureFlag
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
