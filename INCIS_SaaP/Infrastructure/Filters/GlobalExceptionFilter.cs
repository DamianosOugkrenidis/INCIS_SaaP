using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using INCIS_SaaP.Infrastructure.Exceptions;

namespace INCIS_SaaP.Infrastructure.Filters;

/// <summary>
/// Global exception filter that returns standardized error responses for API endpoints
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        // Only handle API requests (not MVC views)
        if (!IsApiRequest(context))
            return;

        var exception = context.Exception;

        var errorResponse = exception switch
        {
            AppException appEx => new ApiErrorResponse
            {
                Status = appEx.StatusCode,
                ErrorCode = appEx.ErrorCode,
                Message = appEx.Message,
                Errors = appEx is ValidationException validationEx ? validationEx.Errors : null
            },
            _ => new ApiErrorResponse
            {
                Status = 500,
                ErrorCode = "INTERNAL_ERROR",
                Message = _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
            }
        };

        if (exception is not AppException)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Application exception: {ErrorCode} - {Message}", errorResponse.ErrorCode, errorResponse.Message);
        }

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = errorResponse.Status
        };
        context.ExceptionHandled = true;
    }

    private static bool IsApiRequest(ExceptionContext context)
    {
        var path = context.HttpContext.Request.Path.Value ?? "";
        return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
               || context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json");
    }
}

/// <summary>
/// Standardized API error response DTO
/// </summary>
public class ApiErrorResponse
{
    public int Status { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
    public string? StackTrace { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N")[..12];
}
