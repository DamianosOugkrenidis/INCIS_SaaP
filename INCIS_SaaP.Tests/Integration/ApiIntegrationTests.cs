using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Hangfire;
using INCIS_SaaP.Data;
using System.Net;
using Xunit;

namespace INCIS_SaaP.Tests.Integration;

/// <summary>
/// Full integration tests using WebApplicationFactory (real HTTP pipeline).
/// These tests require the application to start fully, including Hangfire (requires SQL Server).
/// Run with: dotnet test --filter "Category=Integration" with SQL Server available.
/// </summary>
[Trait("Category", "Integration")]
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // Απενεργοποίηση background services (Hangfire) που απαιτούν SQL Server στο CI
            builder.UseSetting("DisableBackgroundServices", "true");
            builder.ConfigureServices(services =>
            {
                // Αφαίρεση ΟΛΩΝ των EF Core descriptors (incl. SqlServer provider services)
                // ώστε να μην συνυπάρχουν δύο providers (SqlServer + InMemory).
                var efDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<INCISDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(INCISDbContext)
                    || (d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") ?? false)
                    || (d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") ?? false))
                    .ToList();
                foreach (var d in efDescriptors) services.Remove(d);

                // Remove Hangfire (causes issues in test)
                services.RemoveAll(typeof(IGlobalConfiguration));

                // Add InMemory DB (μοναδικός provider πλέον)
                services.AddDbContext<INCISDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTest_" + Guid.NewGuid()));
            });
        });
        _client = customFactory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiHealth_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/api/v1/Api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("INCIS SaaP API");
    }

    [Fact]
    public async Task ApiSecurityInfo_ShouldReturnEncryptionDetails()
    {
        var response = await _client.GetAsync("/api/v1/Api/security");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("AES-256-GCM");
    }

    [Fact]
    public async Task SubscriptionPlans_ShouldBePubliclyAccessible()
    {
        var response = await _client.GetAsync("/api/v1/subscription/plans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresent()
    {
        var response = await _client.GetAsync("/api/v1/Api/health");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
    }
}
