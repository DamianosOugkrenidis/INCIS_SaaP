using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace INCIS_SaaP.Tests.Helpers;

/// <summary>
/// No-op IAntiforgery για tests που στοχεύουν τη λογική των POST actions
/// (η πραγματική προστασία CSRF ελέγχεται ξεχωριστά στο EndpointAuthorizationTests).
/// </summary>
public sealed class NoOpAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) =>
        new("test-token", "test-cookie", "test", "test");

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) =>
        new("test-token", "test-cookie", "test", "test");

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

    public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;

    public void SetCookieTokenAndHeader(HttpContext httpContext) { }
}
