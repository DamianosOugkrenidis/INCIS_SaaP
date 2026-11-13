// Author: Damianos Pythagoras Ougkrenidis
// Event Sourcing - Complete event history for formulas

using INCIS_SaaP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace INCIS_SaaP.Architecture.EventSourcing;

/// <summary>
/// Stored event in the event store (append-only)
/// </summary>
public class StoredEvent
{
    [Key]
    public long SequenceNumber { get; set; }
    
    public Guid EventId { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string AggregateType { get; set; } = string.Empty;
    
    public int AggregateId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;
    
    public int Version { get; set; }
    
    [Required]
    public string EventData { get; set; } = string.Empty; // JSON serialized event
    
    [MaxLength(2000)]
    public string? Metadata { get; set; } // JSON metadata
    
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? UserId { get; set; }
    
    [MaxLength(100)]
    public string? TenantId { get; set; }
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    [MaxLength(100)]
    public string? CausationId { get; set; }

    /// <summary>
    /// Hash of previous event for cryptographic chaining (immutability proof)
    /// </summary>
    [MaxLength(64)]
    public string? PreviousEventHash { get; set; }
    
    /// <summary>
    /// Hash of this event (SHA-256)
    /// </summary>
    [MaxLength(64)]
    public string? EventHash { get; set; }
}

/// <summary>
/// Snapshot for aggregate state (optimization to avoid replaying all events)
/// </summary>
public class AggregateSnapshot
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string AggregateType { get; set; } = string.Empty;
    
    public int AggregateId { get; set; }
    
    public int Version { get; set; }
    
    [Required]
    public string StateData { get; set; } = string.Empty; // JSON serialized state
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event Store interface for persisting and retrieving domain events
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to the store for an aggregate
    /// </summary>
    Task AppendEventsAsync(string aggregateType, int aggregateId, IEnumerable<DomainEvent> events, 
        int expectedVersion, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all events for an aggregate
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> GetEventsAsync(string aggregateType, int aggregateId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets events for an aggregate from a specific version
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> GetEventsFromVersionAsync(string aggregateType, int aggregateId, 
        int fromVersion, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all events of a specific type (for projections/materialized views)
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> GetEventsByTypeAsync(string eventType, DateTime? since = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a snapshot of aggregate state
    /// </summary>
    Task SaveSnapshotAsync(string aggregateType, int aggregateId, int version, object state, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest snapshot for an aggregate
    /// </summary>
    Task<AggregateSnapshot?> GetLatestSnapshotAsync(string aggregateType, int aggregateId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current version of an aggregate
    /// </summary>
    Task<int> GetCurrentVersionAsync(string aggregateType, int aggregateId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Event Store implementation backed by SQL Server
/// </summary>
public class SqlEventStore : IEventStore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SqlEventStore> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public SqlEventStore(IServiceProvider serviceProvider, ILogger<SqlEventStore> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task AppendEventsAsync(string aggregateType, int aggregateId, 
        IEnumerable<DomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var currentVersion = await GetCurrentVersionAsync(aggregateType, aggregateId, cancellationToken);
        
        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(
                $"Concurrency conflict for {aggregateType}:{aggregateId}. " +
                $"Expected version: {expectedVersion}, Current version: {currentVersion}");
        }

        string? previousHash = null;
        var lastEvent = await dbContext.Set<StoredEvent>()
            .Where(e => e.AggregateType == aggregateType && e.AggregateId == aggregateId)
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (lastEvent != null)
            previousHash = lastEvent.EventHash;

        var version = expectedVersion;
        foreach (var domainEvent in events)
        {
            version++;
            var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
            var metadata = JsonSerializer.Serialize(domainEvent.Metadata, JsonOptions);

            var storedEvent = new StoredEvent
            {
                EventId = domainEvent.EventId,
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                EventType = domainEvent.EventType,
                Version = version,
                EventData = eventData,
                Metadata = metadata,
                OccurredOn = domainEvent.OccurredOn,
                UserId = domainEvent.UserId,
                TenantId = domainEvent.TenantId,
                CorrelationId = domainEvent.CorrelationId,
                CausationId = domainEvent.CausationId,
                PreviousEventHash = previousHash
            };

            // Compute cryptographic hash for immutability chain
            storedEvent.EventHash = ComputeEventHash(storedEvent);
            previousHash = storedEvent.EventHash;

            dbContext.Set<StoredEvent>().Add(storedEvent);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Appended {Count} events for {AggregateType}:{AggregateId} (version {Version})",
            events.Count(), aggregateType, aggregateId, version);
    }

    public async Task<IReadOnlyList<StoredEvent>> GetEventsAsync(string aggregateType, int aggregateId, 
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        return await dbContext.Set<StoredEvent>()
            .Where(e => e.AggregateType == aggregateType && e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredEvent>> GetEventsFromVersionAsync(string aggregateType, int aggregateId, 
        int fromVersion, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        return await dbContext.Set<StoredEvent>()
            .Where(e => e.AggregateType == aggregateType && e.AggregateId == aggregateId && e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredEvent>> GetEventsByTypeAsync(string eventType, DateTime? since = null, 
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var query = dbContext.Set<StoredEvent>().Where(e => e.EventType == eventType);
        
        if (since.HasValue)
            query = query.Where(e => e.OccurredOn >= since.Value);

        return await query.OrderBy(e => e.SequenceNumber).ToListAsync(cancellationToken);
    }

    public async Task SaveSnapshotAsync(string aggregateType, int aggregateId, int version, object state, 
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var snapshot = new AggregateSnapshot
        {
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Version = version,
            StateData = JsonSerializer.Serialize(state, JsonOptions)
        };

        dbContext.Set<AggregateSnapshot>().Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AggregateSnapshot?> GetLatestSnapshotAsync(string aggregateType, int aggregateId, 
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        return await dbContext.Set<AggregateSnapshot>()
            .Where(s => s.AggregateType == aggregateType && s.AggregateId == aggregateId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetCurrentVersionAsync(string aggregateType, int aggregateId, 
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var lastEvent = await dbContext.Set<StoredEvent>()
            .Where(e => e.AggregateType == aggregateType && e.AggregateId == aggregateId)
            .OrderByDescending(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return lastEvent?.Version ?? 0;
    }

    /// <summary>
    /// Computes SHA-256 hash for cryptographic chaining (blockchain-lite immutability)
    /// </summary>
    private static string ComputeEventHash(StoredEvent storedEvent)
    {
        var data = $"{storedEvent.EventId}|{storedEvent.AggregateType}|{storedEvent.AggregateId}|" +
                   $"{storedEvent.Version}|{storedEvent.EventData}|{storedEvent.OccurredOn:O}|" +
                   $"{storedEvent.PreviousEventHash ?? "GENESIS"}";
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}

