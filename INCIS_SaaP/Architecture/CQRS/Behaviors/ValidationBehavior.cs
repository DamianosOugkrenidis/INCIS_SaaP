// Author: Damianos Pythagoras Ougkrenidis
// CQRS Pipeline Behaviors - Cross-cutting concerns (MediatR-style)

using INCIS_SaaP.Architecture.CQRS.Commands;
using INCIS_SaaP.Architecture.CQRS.Queries;
using INCIS_SaaP.Domain.Common;
using System.Diagnostics;

namespace INCIS_SaaP.Architecture.CQRS.Behaviors;

/// <summary>
/// Interface for command handlers
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for query handlers
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pipeline behavior interface (similar to MediatR IPipelineBehavior)
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation behavior - validates commands before execution
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly IEnumerable<ICommandValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<ICommandValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        if (!_validators.Any())
            return await next();

        _logger.LogDebug("Validating {RequestType}", typeof(TRequest).Name);

        var errors = new List<string>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
                errors.AddRange(result.Errors);
        }

        if (errors.Any())
        {
            _logger.LogWarning("Validation failed for {RequestType}: {Errors}", 
                typeof(TRequest).Name, string.Join("; ", errors));
            
            // If TResponse is Result or Result<T>, return failure
            if (typeof(TResponse) == typeof(Result))
                return (TResponse)(object)Result.Failure(errors);
            
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(TResponse).GetMethod("Failure", new[] { typeof(IEnumerable<string>) });
                if (failureMethod != null)
                    return (TResponse)failureMethod.Invoke(null, new object[] { errors })!;
            }

            throw new ValidationException(errors);
        }

        return await next();
    }
}

/// <summary>
/// Logging behavior - logs command/query execution
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Performance behavior - logs slow operations
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowThresholdMs)
        {
            _logger.LogWarning("SLOW OPERATION: {RequestName} took {ElapsedMs}ms (threshold: {Threshold}ms)",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, SlowThresholdMs);
        }

        return response;
    }
}

/// <summary>
/// Authorization behavior - checks user permissions before execution
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly IEnumerable<IAuthorizationRule<TRequest>> _authRules;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        IEnumerable<IAuthorizationRule<TRequest>> authRules,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _authRules = authRules;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        foreach (var rule in _authRules)
        {
            var result = await rule.AuthorizeAsync(request, cancellationToken);
            if (!result.IsAuthorized)
            {
                _logger.LogWarning("Authorization denied for {RequestType}: {Reason}", 
                    typeof(TRequest).Name, result.Reason);
                throw new UnauthorizedAccessException(result.Reason);
            }
        }

        return await next();
    }
}

// ============================================================
// SUPPORTING INTERFACES
// ============================================================

public interface ICommandValidator<in TRequest>
{
    Task<ValidationResult> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(params string[] errors) => new() { IsValid = false, Errors = errors.ToList() };
}

public class ValidationException : Exception
{
    public List<string> Errors { get; }
    public ValidationException(List<string> errors) : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}

public interface IAuthorizationRule<in TRequest>
{
    Task<AuthorizationResult> AuthorizeAsync(TRequest request, CancellationToken cancellationToken = default);
}

public class AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string? Reason { get; init; }

    public static AuthorizationResult Authorized() => new() { IsAuthorized = true };
    public static AuthorizationResult Denied(string reason) => new() { IsAuthorized = false, Reason = reason };
}

/// <summary>
/// Dispatcher that routes commands/queries through the pipeline
/// </summary>
public interface ICQRSDispatcher
{
    Task<TResult> SendCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    Task<TResult> SendQueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}

public class CQRSDispatcher : ICQRSDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CQRSDispatcher> _logger;

    public CQRSDispatcher(IServiceProvider serviceProvider, ILogger<CQRSDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResult> SendCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            _logger.LogError("No handler found for command: {CommandType}", commandType.Name);
            throw new InvalidOperationException($"No handler registered for {commandType.Name}");
        }

        var method = handlerType.GetMethod("HandleAsync");
        return await (Task<TResult>)method!.Invoke(handler, new object[] { command, cancellationToken })!;
    }

    public async Task<TResult> SendQueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            _logger.LogError("No handler found for query: {QueryType}", queryType.Name);
            throw new InvalidOperationException($"No handler registered for {queryType.Name}");
        }

        var method = handlerType.GetMethod("HandleAsync");
        return await (Task<TResult>)method!.Invoke(handler, new object[] { query, cancellationToken })!;
    }
}
