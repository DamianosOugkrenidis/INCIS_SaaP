using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using INCIS_SaaP.Configuration;
using INCIS_SaaP.Filters;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// E2 — Επαλήθευση ότι ο μηχανισμός Phase 0 ΕΚΠΕΜΠΕΙ πραγματικό log για κλήσεις που θα
/// αποκλείονταν (log-only), και ότι με Enforce=true απορρίπτει (403). Αποδεικτικό μηχανισμού,
/// όχι production logs.
/// </summary>
[Trait("Category", "Security")]
public class ApiWritePolicyLoggingTests
{
    private sealed class CapturingLogger : ILogger<ApiWritePolicyFilter>
    {
        public List<string> Messages { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private static ActionExecutingContext BuildContext(string method, string controller, bool authenticated, string[] roles)
    {
        var claims = new List<Claim>();
        if (authenticated)
        {
            claims.Add(new Claim(ClaimTypes.Name, "phase0@incis.local"));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "u-1"));
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
        }
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "test" : null));
        http.Request.Method = method;
        http.Request.Path = "/api/v1/incis";

        var route = new RouteData();
        route.Values["controller"] = controller;

        return new ActionExecutingContext(
            new ActionContext(http, route, new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: null!);
    }

    private static ApiWritePolicyFilter Filter(bool enforce, CapturingLogger logger) =>
        new(Options.Create(new ApiWritePolicyOptions { Enforce = enforce, Roles = "Admin,SuperUser" }), logger);

    [Fact]
    public async Task Phase0_NonAdminWrite_EmitsPhase0ObservationLog_AndAllows()
    {
        var logger = new CapturingLogger();
        var filter = Filter(enforce: false, logger);
        var ctx = BuildContext("POST", "ApiExtended", authenticated: true, roles: new[] { "User" });
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });

        nextCalled.Should().BeTrue("Phase 0 ΔΕΝ αποκλείει");
        logger.Messages.Should().Contain(m => m.Contains("PHASE-0 OBSERVATION") && m.Contains("/api/v1/incis"),
            "ο μηχανισμός πρέπει να καταγράφει τις κλήσεις που θα αποκλείονταν");
    }

    [Fact]
    public async Task Phase1_NonAdminWrite_EmitsDenyLog_AndBlocks403()
    {
        var logger = new CapturingLogger();
        var filter = Filter(enforce: true, logger);
        var ctx = BuildContext("POST", "ApiExtended", authenticated: true, roles: new[] { "User" });
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });

        nextCalled.Should().BeFalse("Phase 1 αποκλείει");
        ctx.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.ObjectResult>()
            .Which.StatusCode.Should().Be(403);
        logger.Messages.Should().Contain(m => m.Contains("DENY"));
    }

    [Fact]
    public async Task AuthorizedWrite_EmitsAllowLog()
    {
        var logger = new CapturingLogger();
        var filter = Filter(enforce: true, logger);
        var ctx = BuildContext("POST", "ApiExtended", authenticated: true, roles: new[] { "Admin" });

        await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(null!));

        logger.Messages.Should().Contain(m => m.Contains("API-write ALLOW"));
    }
}
