using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using INCIS_SaaP.Configuration;
using INCIS_SaaP.Filters;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// CSRF — Phase 0 (log-only) / Phase 1 (400) για τα cookie-authenticated API writes,
/// καθώς και επαλήθευση cookie flags (SameSite=Lax, HttpOnly, Secure) και CSP header.
/// Καμία αλλαγή στην παραγωγική συμπεριφορά (default Enforce=false).
/// </summary>
[Trait("Category", "Security")]
public class ApiCsrfTests
{
    private sealed class CapturingLogger : ILogger<ApiCsrfFilter>
    {
        public List<string> Messages { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }

    private static ActionExecutingContext BuildContext()
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "csrf@incis.local"),
            new Claim(ClaimTypes.NameIdentifier, "u-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "test"));
        http.Request.Method = "POST";
        http.Request.Path = "/api/v1/incis";

        var route = new RouteData();
        route.Values["controller"] = "ApiExtended";

        return new ActionExecutingContext(
            new ActionContext(http, route, new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: null!);
    }

    private static ApiCsrfFilter Filter(bool enforce, bool tokenValid, CapturingLogger logger)
    {
        var af = new Mock<IAntiforgery>();
        if (tokenValid)
            af.Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        else
            af.Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
              .ThrowsAsync(new AntiforgeryValidationException("missing/invalid token"));

        return new ApiCsrfFilter(
            Options.Create(new ApiCsrfOptions { Enforce = enforce, HeaderName = "X-CSRF-TOKEN" }),
            af.Object,
            logger);
    }

    // ---------------- Unit: Phase 0 / Phase 1 ----------------
    [Fact]
    public async Task Phase0_MissingToken_IsLogged_AndAllowed()
    {
        var logger = new CapturingLogger();
        var ctx = BuildContext();
        var next = false;

        await Filter(enforce: false, tokenValid: false, logger)
            .OnActionExecutionAsync(ctx, () => { next = true; return Task.FromResult<ActionExecutedContext>(null!); });

        next.Should().BeTrue("Phase 0 ΔΕΝ αποκλείει");
        logger.Messages.Should().Contain(m => m.Contains("API-CSRF PHASE-0 OBSERVATION"));
    }

    [Fact]
    public async Task Phase1_MissingToken_Returns400_AndBlocks()
    {
        var logger = new CapturingLogger();
        var ctx = BuildContext();
        var next = false;

        await Filter(enforce: true, tokenValid: false, logger)
            .OnActionExecutionAsync(ctx, () => { next = true; return Task.FromResult<ActionExecutedContext>(null!); });

        next.Should().BeFalse("Phase 1 αποκλείει");
        ctx.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
        logger.Messages.Should().Contain(m => m.Contains("API-CSRF DENY"));
    }

    [Fact]
    public async Task ValidToken_IsAllowed()
    {
        var logger = new CapturingLogger();
        var ctx = BuildContext();
        var next = false;

        await Filter(enforce: true, tokenValid: true, logger)
            .OnActionExecutionAsync(ctx, () => { next = true; return Task.FromResult<ActionExecutedContext>(null!); });

        next.Should().BeTrue();
        ctx.Result.Should().BeNull();
    }

    // ---------------- HTTP ----------------
    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static StringContent Json(object o) => new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Http_DefaultPhase0_MissingToken_IsAllowed()
    {
        // Πραγματικό IAntiforgery, Enforce=false (default) -> καμία αλλαγή συμπεριφοράς.
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "Admin" });
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/incis",
            Json(new { name = "CSRF P0 " + Guid.NewGuid().ToString("N")[..6], casNumber = "1-1-1" }));

        ((int)res.StatusCode).Should().BeLessThan(400, "Phase 0 log-only δεν αλλάζει συμπεριφορά");
    }

    [Fact]
    public async Task Http_Phase1_MissingToken_Returns400()
    {
        using var factory = new RoleWebApplicationFactory(
            s => s.Roles = new[] { "Admin" },
            settings: new Dictionary<string, string> { ["Api:Csrf:Enforce"] = "true" });
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/incis",
            Json(new { name = "CSRF P1 " + Guid.NewGuid().ToString("N")[..6], casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------- Cookie flags / CSP ----------------
    [Fact]
    public void IdentityCookie_HasSameSiteLax_HttpOnly_AndSecurePolicy()
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "Admin" });
        _ = factory.EnsureSeeded();
        var opts = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        opts.Cookie.HttpOnly.Should().BeTrue();
        opts.Cookie.SameSite.Should().Be(SameSiteMode.Lax, "SameSite=Lax είναι μετριασμός CSRF");
        opts.Cookie.SecurePolicy.Should().NotBe(CookieSecurePolicy.None, "το cookie πρέπει να είναι Secure σε HTTPS");
    }

    [Fact]
    public void Antiforgery_HasHttpOnlyCookie_AndHeaderName()
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "Admin" });
        _ = factory.EnsureSeeded();
        var opts = factory.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        opts.HeaderName.Should().Be("X-CSRF-TOKEN");
        opts.Cookie.HttpOnly.Should().BeTrue();
        opts.Cookie.SecurePolicy.Should().NotBe(CookieSecurePolicy.None);
    }

    [Fact]
    public async Task CspHeader_IsPresent()
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "Admin" });
        var client = Client(factory);

        var res = await client.GetAsync("/");

        res.Headers.Should().ContainKey("Content-Security-Policy", "πρέπει να υπάρχει CSP (μετριασμός XSS)");
    }
}
