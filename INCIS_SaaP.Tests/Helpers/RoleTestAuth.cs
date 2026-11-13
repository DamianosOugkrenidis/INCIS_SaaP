using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace INCIS_SaaP.Tests.Helpers;

/// <summary>
/// Σενάριο αυθεντικοποίησης για tests: επιτρέπει ρύθμιση anonymous / συγκεκριμένων ρόλων.
/// </summary>
public class TestAuthScenario
{
    public bool IsAuthenticated { get; set; } = true;
    public string[] Roles { get; set; } = new[] { "Admin", "SuperUser" };
}

/// <summary>
/// Authentication handler που σέβεται το <see cref="TestAuthScenario"/>.
/// Αν <c>IsAuthenticated=false</c> επιστρέφει NoResult (→ challenge: 401/redirect),
/// αλλιώς δημιουργεί principal με τους ρόλους του σεναρίου.
/// </summary>
public class RoleTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "RoleTestScheme";

    private readonly TestAuthScenario _scenario;

    public RoleTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TestAuthScenario scenario)
        : base(options, logger, encoder)
    {
        _scenario = scenario;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_scenario.IsAuthenticated)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "test@incis.local"),
            new(ClaimTypes.Email, "test@incis.local")
        };
        foreach (var role in _scenario.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
