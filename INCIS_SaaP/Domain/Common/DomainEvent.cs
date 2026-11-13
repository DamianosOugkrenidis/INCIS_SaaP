// Author: Damianos Pythagoras Ougkrenidis
// Domain-Driven Design - Base Domain Event

namespace INCIS_SaaP.Domain.Common;

/// <summary>
/// Base class for all domain events in the system.
/// Domain events represent something that happened in the domain that other parts
/// of the system might be interested in.
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
    public int? AggregateId { get; init; }
    public string? AggregateType { get; init; }
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
    public int Version { get; init; } = 1;

    /// <summary>
    /// Metadata dictionary for extensible event properties
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Interface for entities that raise domain events
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void AddDomainEvent(DomainEvent domainEvent);
    void RemoveDomainEvent(DomainEvent domainEvent);
    void ClearDomainEvents();
}

/// <summary>
/// Interface for domain event handlers
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the domain event dispatcher
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for aggregate roots that track domain events
/// </summary>
public abstract class AggregateRoot : IHasDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Raises a domain event and adds it to the pending events list
    /// </summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

/// <summary>
/// Domain event dispatcher implementation using DI
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Dispatching domain event: {EventType} (ID: {EventId})", 
            domainEvent.EventType, domainEvent.EventId);

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync");
            if (method != null)
            {
                await (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
            }
        }
    }

    public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
