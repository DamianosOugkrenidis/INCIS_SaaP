// Author: Damianos Pythagoras Ougkrenidis
// Message Bus - Async communication between bounded contexts
// Supports in-process (for monolith) and external (RabbitMQ/Kafka) message brokers

using INCIS_SaaP.Domain.Common;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace INCIS_SaaP.Architecture.MessageBus;

/// <summary>
/// Message envelope for bus communication
/// </summary>
public class MessageEnvelope
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public string MessageType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? ReplyTo { get; init; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; init; } = 3;
    public Dictionary<string, string> Headers { get; init; } = new();
}

/// <summary>
/// Interface for the message bus (decoupled communication)
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to all subscribers
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Publishes a message to a specific topic/queue
    /// </summary>
    Task PublishAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Subscribes to messages of a specific type
    /// </summary>
    IDisposable Subscribe<TMessage>(Func<TMessage, CancellationToken, Task> handler) 
        where TMessage : class;

    /// <summary>
    /// Subscribes to a specific topic
    /// </summary>
    IDisposable Subscribe(string topic, Func<MessageEnvelope, CancellationToken, Task> handler);
}

/// <summary>
/// Interface for message handlers
/// </summary>
public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-process message bus implementation (for modular monolith)
/// Uses System.Threading.Channels for high-performance async messaging
/// </summary>
public class InProcessMessageBus : IMessageBus, IDisposable
{
    private readonly ConcurrentDictionary<string, List<Func<MessageEnvelope, CancellationToken, Task>>> _subscriptions = new();
    private readonly Channel<MessageEnvelope> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessMessageBus> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    public InProcessMessageBus(IServiceProvider serviceProvider, ILogger<InProcessMessageBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateUnbounded<MessageEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        _processingTask = ProcessMessagesAsync(_cts.Token);
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        var topic = typeof(TMessage).FullName ?? typeof(TMessage).Name;
        await PublishAsync(topic, message, cancellationToken);
    }

    public async Task PublishAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        var envelope = new MessageEnvelope
        {
            MessageType = typeof(TMessage).FullName ?? typeof(TMessage).Name,
            Payload = JsonSerializer.Serialize(message),
            Headers = { ["topic"] = topic }
        };

        await _channel.Writer.WriteAsync(envelope, cancellationToken);
        _logger.LogDebug("Published message: {MessageType} to topic: {Topic}", envelope.MessageType, topic);
    }

    public IDisposable Subscribe<TMessage>(Func<TMessage, CancellationToken, Task> handler) 
        where TMessage : class
    {
        var topic = typeof(TMessage).FullName ?? typeof(TMessage).Name;
        return Subscribe(topic, async (envelope, ct) =>
        {
            var message = JsonSerializer.Deserialize<TMessage>(envelope.Payload);
            if (message != null)
                await handler(message, ct);
        });
    }

    public IDisposable Subscribe(string topic, Func<MessageEnvelope, CancellationToken, Task> handler)
    {
        var handlers = _subscriptions.GetOrAdd(topic, _ => new List<Func<MessageEnvelope, CancellationToken, Task>>());
        handlers.Add(handler);

        _logger.LogDebug("Subscribed to topic: {Topic}", topic);
        return new SubscriptionDisposable(() => handlers.Remove(handler));
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var envelope in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var topic = envelope.Headers.GetValueOrDefault("topic") ?? envelope.MessageType;
                
                if (_subscriptions.TryGetValue(topic, out var handlers))
                {
                    var tasks = handlers.Select(h => ProcessWithRetryAsync(h, envelope, cancellationToken));
                    await Task.WhenAll(tasks);
                }

                // Also dispatch to IMessageHandler<T> implementations via DI
                await DispatchToHandlersAsync(envelope, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {MessageId} ({MessageType})", 
                    envelope.MessageId, envelope.MessageType);
            }
        }
    }

    private async Task ProcessWithRetryAsync(Func<MessageEnvelope, CancellationToken, Task> handler, 
        MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        while (envelope.RetryCount <= envelope.MaxRetries)
        {
            try
            {
                await handler(envelope, cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                envelope.RetryCount++;
                if (envelope.RetryCount > envelope.MaxRetries)
                {
                    _logger.LogError(ex, "Message {MessageId} failed after {MaxRetries} retries", 
                        envelope.MessageId, envelope.MaxRetries);
                    // Dead letter queue
                    return;
                }

                _logger.LogWarning(ex, "Message {MessageId} failed, retry {Retry}/{MaxRetries}", 
                    envelope.MessageId, envelope.RetryCount, envelope.MaxRetries);
                
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, envelope.RetryCount)), cancellationToken);
            }
        }
    }

    private async Task DispatchToHandlersAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        // Resolve message type and find registered handlers
        var messageType = Type.GetType(envelope.MessageType);
        if (messageType == null) return;

        var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
        
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync");
            var message = JsonSerializer.Deserialize(envelope.Payload, messageType);
            if (method != null && message != null)
            {
                await (Task)method.Invoke(handler, new[] { message, cancellationToken })!;
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.Complete();
        _cts.Dispose();
    }

    private class SubscriptionDisposable : IDisposable
    {
        private readonly Action _onDispose;
        public SubscriptionDisposable(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}
