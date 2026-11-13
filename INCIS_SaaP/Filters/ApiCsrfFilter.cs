using System.Security.Claims;
using INCIS_SaaP.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace INCIS_SaaP.Filters;

/// <summary>
/// CSRF filter για τα cookie-authenticated API write endpoints (<c>ApiExtendedController</c>).
/// <list type="bullet">
/// <item>Phase 0 (<c>Enforce=false</c>): log-only — καταγράφει απόντα/άκυρα tokens, ΔΕΝ αποκλείει.</item>
/// <item>Phase 1 (<c>Enforce=true</c>): 400 για απόν/άκυρο antiforgery token.</item>
/// </list>
/// Ενεργεί μόνο σε POST/PUT/PATCH/DELETE του ApiExtendedController (τα GET δεν χρειάζονται CSRF).
/// </summary>
public class ApiCsrfFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> WriteMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly ApiCsrfOptions _options;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<ApiCsrfFilter> _logger;

    public ApiCsrfFilter(
        IOptions<ApiCsrfOptions> options,
        IAntiforgery antiforgery,
        ILogger<ApiCsrfFilter> logger)
    {
        _options = options.Value;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var controller = context.RouteData.Values["controller"]?.ToString();

        if (!WriteMethods.Contains(http.Request.Method) ||
            !string.Equals(controller, "ApiExtended", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var valid = false;
        try
        {
            await _antiforgery.ValidateRequestAsync(http);
            valid = true;
        }
        catch (AntiforgeryValidationException)
        {
            valid = false;
        }

        var identity = http.User.Identity?.Name ?? "(anonymous)";
        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-";
        var path = http.Request.Path.Value ?? "-";
        var header = _options.HeaderName;

        if (valid)
        {
            await next();
            return;
        }

        if (_options.Enforce)
        {
            _logger.LogWarning(
                "API-CSRF DENY (enforced) {Method} {Path} user={User} id={UserId} header={Header}",
                http.Request.Method, path, identity, userId, header);
            context.Result = new ObjectResult(new
            {
                error = "BadRequest",
                message = $"Απαιτείται έγκυρο CSRF token (header {header})."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }

        // Phase 0 — ΠΑΡΑΤΗΡΗΣΗ ΜΟΝΟ. Δεν αποκλείει.
        _logger.LogWarning(
            "API-CSRF PHASE-0 OBSERVATION (no enforcement) {Method} {Path} user={User} id={UserId} header={Header} — θα αποκλειόταν μετά το enforce",
            http.Request.Method, path, identity, userId, header);
        await next();
    }
}
