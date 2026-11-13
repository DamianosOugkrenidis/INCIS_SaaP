// Author: Damianos Pythagoras Ougkrenidis
// Resilience Policies - Polly-style retry, circuit breaker, timeout

namespace INCIS_SaaP.Infrastructure.Resilience;

/// <summary>
/// Resilience policy configuration for external service calls
/// Implements retry, circuit breaker, and timeout patterns
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Executes an action with exponential backoff retry policy
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var delay = initialDelay ?? TimeSpan.FromMilliseconds(200);
        var attempt = 0;

        while (true)
        {
            try
            {
                attempt++;
                return await action();
            }
            catch (Exception ex) when (attempt <= maxRetries && IsTransientException(ex))
            {
                logger?.LogWarning(ex, "Transient error in {Operation}, attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms",
                    operationName ?? "unknown", attempt, maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }
    }

    /// <summary>
    /// Executes an action with timeout
    /// </summary>
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> action,
        TimeSpan timeout,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            return await action(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger?.LogWarning("Operation {Operation} timed out after {Timeout}ms", 
                operationName ?? "unknown", timeout.TotalMilliseconds);
            throw new TimeoutException($"Operation '{operationName}' timed out after {timeout.TotalSeconds}s");
        }
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is HttpRequestException
            || ex is TimeoutException
            || ex is System.Net.Sockets.SocketException
            || (ex is Microsoft.Data.SqlClient.SqlException sqlEx && IsTransientSqlError(sqlEx.Number));
    }

    private static bool IsTransientSqlError(int errorNumber)
    {
        // SQL Server transient error numbers
        return errorNumber switch
        {
            -2 => true,     // Timeout
            20 => true,     // Instance not found
            64 => true,     // Connection was forcibly closed
            233 => true,    // Connection error
            10053 => true,  // Transport-level error
            10054 => true,  // Connection reset
            10060 => true,  // Connection attempt failed
            40143 => true,  // Connection could not be initialized
            40197 => true,  // Service error
            40501 => true,  // Service busy
            40613 => true,  // Database unavailable
            49918 => true,  // Cannot process request
            49919 => true,  // Cannot process create/update
            49920 => true,  // Cannot process request
            _ => false
        };
    }
}

/// <summary>
/// Circuit Breaker implementation for external service calls
/// States: Closed (normal) -> Open (failing) -> HalfOpen (testing)
/// </summary>
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly ILogger<CircuitBreaker>? _logger;
    private readonly string _name;
    
    private int _failureCount;
    private CircuitState _state = CircuitState.Closed;
    private DateTime _lastFailureTime;
    private readonly object _lock = new();

    public CircuitBreaker(string name, int failureThreshold = 5, TimeSpan? openDuration = null, ILogger<CircuitBreaker>? logger = null)
    {
        _name = name;
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && DateTime.UtcNow - _lastFailureTime > _openDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _logger?.LogInformation("Circuit breaker '{Name}' transitioning to HalfOpen", _name);
                }
                return _state;
            }
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (State == CircuitState.Open)
        {
            _logger?.LogWarning("Circuit breaker '{Name}' is OPEN. Rejecting call.", _name);
            throw new CircuitBreakerOpenException($"Circuit breaker '{_name}' is open.");
        }

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch (Exception)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Closed;
                _logger?.LogInformation("Circuit breaker '{Name}' CLOSED (recovered)", _name);
            }
        }
    }

    private void OnFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold || _state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Open;
                _logger?.LogWarning("Circuit breaker '{Name}' OPENED after {Count} failures", _name, _failureCount);
            }
        }
    }
}

public enum CircuitState
{
    Closed,    // Normal operation
    Open,      // Failing - reject calls
    HalfOpen   // Testing - allow one call through
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}

/// <summary>
/// Circuit breaker factory for managing multiple breakers
/// </summary>
public interface ICircuitBreakerFactory
{
    CircuitBreaker GetOrCreate(string name, int failureThreshold = 5, TimeSpan? openDuration = null);
}

public class CircuitBreakerFactory : ICircuitBreakerFactory
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CircuitBreaker> _breakers = new();
    private readonly ILoggerFactory _loggerFactory;

    public CircuitBreakerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public CircuitBreaker GetOrCreate(string name, int failureThreshold = 5, TimeSpan? openDuration = null)
    {
        return _breakers.GetOrAdd(name, n => new CircuitBreaker(
            n, failureThreshold, openDuration, 
            _loggerFactory.CreateLogger<CircuitBreaker>()));
    }
}
