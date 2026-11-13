// Author: Damianos Pythagoras Ougkrenidis
// Outbox Pattern - Guaranteed event delivery (DB transaction + event publish atomically)

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace INCIS_SaaP.Architecture.OutboxPattern;

/// <summary>
/// Outbox message stored in the same transaction as the domain change.
/// A background processor picks these up and publishes them to the message bus.
/// This ensures at-least-once delivery guarantee.
/// </summary>
public class OutboxMessage
{
    [Key]
    public long Id { get; set; }
    
    public Guid MessageId { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string MessageType { get; set; } = string.Empty;
    
    [Required]
    public string Payload { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Topic { get; set; }
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the message was successfully published
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Number of publish attempts
    /// </summary>
    public int RetryCount { get; set; }
    
    /// <summary>
    /// Last error if publish failed
    /// </summary>
    [MaxLength(2000)]
    public string? LastError { get; set; }
    
    /// <summary>
    /// Status: Pending, Processing, Processed, Failed
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
}

/// <summary>
/// Interface for the outbox publisher (saves messages to DB in same transaction)
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Adds a message to the outbox (within the current DB transaction)
    /// </summary>
    Task AddToOutboxAsync<TMessage>(TMessage message, string? topic = null, 
        string? correlationId = null, CancellationToken cancellationToken = default) where TMessage : class;
}

/// <summary>
/// Interface for the outbox processor (background service that publishes pending messages)
/// </summary>
public interface IOutboxProcessor
{
    Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Outbox publisher implementation
/// </summary>
public class OutboxPublisher : IOutboxPublisher
{
    private readonly Data.INCISDbContext _dbContext;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(Data.INCISDbContext dbContext, ILogger<OutboxPublisher> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task AddToOutboxAsync<TMessage>(TMessage message, string? topic = null, 
        string? correlationId = null, CancellationToken cancellationToken = default) where TMessage : class
    {
        var outboxMessage = new OutboxMessage
        {
            MessageType = typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).FullName ?? typeof(TMessage).Name,
            Payload = JsonSerializer.Serialize(message),
            Topic = topic ?? typeof(TMessage).Name,
            CorrelationId = correlationId
        };

        _dbContext.Set<OutboxMessage>().Add(outboxMessage);
        // Note: SaveChanges will be called by the caller's transaction
        
        _logger.LogDebug("Added message to outbox: {MessageType} (ID: {MessageId})", 
            outboxMessage.MessageType, outboxMessage.MessageId);
    }
}

/// <summary>
/// Background processor that publishes outbox messages to the message bus
/// </summary>
public class OutboxProcessor : IOutboxProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();
        var messageBus = scope.ServiceProvider.GetRequiredService<MessageBus.IMessageBus>();

        var pendingMessages = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<OutboxMessage>()
                    .Where(m => m.Status == "Pending" || (m.Status == "Failed" && m.RetryCount < MaxRetries))
                    .OrderBy(m => m.CreatedAt)
                    .Take(BatchSize),
                cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                message.Status = "Processing";
                await dbContext.SaveChangesAsync(cancellationToken);

                // Publish to message bus
                var envelope = new MessageBus.MessageEnvelope
                {
                    MessageId = message.MessageId,
                    MessageType = message.MessageType,
                    Payload = message.Payload,
                    CorrelationId = message.CorrelationId,
                    Headers = { ["topic"] = message.Topic ?? "" }
                };

                await messageBus.PublishAsync(message.Topic ?? message.MessageType, envelope, cancellationToken);

                message.Status = "Processed";
                message.ProcessedAt = DateTime.UtcNow;
                
                _logger.LogDebug("Published outbox message: {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                message.Status = "Failed";
                message.RetryCount++;
                message.LastError = ex.Message;
                
                _logger.LogWarning(ex, "Failed to publish outbox message: {MessageId} (retry: {Retry})", 
                    message.MessageId, message.RetryCount);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (pendingMessages.Any())
        {
            _logger.LogInformation("Processed {Count} outbox messages", pendingMessages.Count);
        }
    }
}

/// <summary>
/// Hosted service that periodically processes outbox messages
/// </summary>
public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);

    public OutboxBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox background processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                await processor.ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox background processor");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }
    }
}
