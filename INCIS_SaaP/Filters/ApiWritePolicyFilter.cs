using System.Security.Claims;
using INCIS_SaaP.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace INCIS_SaaP.Filters;

/// <summary>
/// H-2 — Filter πολιτικής εγγραφής για τα API write endpoints (<c>ApiExtendedController</c>, POST/PUT/DELETE).
/// <list type="bullet">
/// <item>Phase 0 (<c>Enforce=false</c>): καταγράφει (log-only) τις κλήσεις που θα αποκλείονταν — ΔΕΝ προστατεύει.</item>
/// <item>Phase 1 (<c>Enforce=true</c>): απορρίπτει με 403 τους μη εξουσιοδοτημένους.</item>
/// </list>
/// Καταγράφει endpoint, μέθοδο, ταυτότητα/ρόλο χρήστη και απόφαση — <b>χωρίς</b> passwords/tokens/ευαίσθητα δεδομένα.
/// </summary>
public class ApiWritePolicyFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> WriteMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly ApiWritePolicyOptions _options;
    private readonly ILogger<ApiWritePolicyFilter> _logger;

    public ApiWritePolicyFilter(IOptions<ApiWritePolicyOptions> options, ILogger<ApiWritePolicyFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var controller = context.RouteData.Values["controller"]?.ToString();
        var method = http.Request.Method;

        if (!WriteMethods.Contains(method) ||
            !string.Equals(controller, "ApiExtended", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var user = http.User;
        var allowed = string.IsNullOrWhiteSpace(_options.Roles)
            ? Array.Empty<string>()
            : _options.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matched = allowed.Where(user.IsInRole).ToArray();
        var identity = user.Identity?.Name ?? "(anonymous)";
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-";
        var path = http.Request.Path.Value ?? "-";

        if (matched.Length > 0)
        {
            _logger.LogInformation(
                "API-write ALLOW {Method} {Path} user={User} id={UserId} matchedRole={Role}",
                method, path, identity, userId, string.Join("|", matched));
            await next();
            return;
        }

        if (_options.Enforce)
        {
            _logger.LogWarning(
                "API-write DENY (enforced) {Method} {Path} user={User} id={UserId} requiredRoles={Roles}",
                method, path, identity, userId, _options.Roles);
            context.Result = new ObjectResult(new
            {
                error = "Forbidden",
                message = "Απαιτείται ρόλος Admin ή SuperUser για εγγραφές μέσω API."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Phase 0 — ΠΑΡΑΤΗΡΗΣΗ ΜΟΝΟ. Δεν αποκλείει. Δεν αποτελεί προστασία πρόσβασης.
        _logger.LogWarning(
            "API-write PHASE-0 OBSERVATION (no enforcement) {Method} {Path} user={User} id={UserId} requiredRoles={Roles} — θα αποκλειόταν μετά το enforce",
            method, path, identity, userId, _options.Roles);
        await next();
    }
}
