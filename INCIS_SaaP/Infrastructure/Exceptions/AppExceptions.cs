namespace INCIS_SaaP.Infrastructure.Exceptions;

/// <summary>
/// Base application exception
/// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    protected AppException(string message, int statusCode, string errorCode, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Resource not found (404)
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with ID '{id}' was not found.", 404, "NOT_FOUND") { }

    public NotFoundException(string message)
        : base(message, 404, "NOT_FOUND") { }
}

/// <summary>
/// Validation error (400)
/// </summary>
public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message, 400, "VALIDATION_ERROR")
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}

/// <summary>
/// Business rule violation (422)
/// </summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message)
        : base(message, 422, "BUSINESS_RULE_VIOLATION") { }
}

/// <summary>
/// Unauthorized access (401)
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication required.")
        : base(message, 401, "UNAUTHORIZED") { }
}

/// <summary>
/// Forbidden access (403)
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, 403, "FORBIDDEN") { }
}

/// <summary>
/// Conflict (409)
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409, "CONFLICT") { }
}
