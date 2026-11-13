using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Tests.Helpers;

/// <summary>
/// WebApplicationFactory για integration tests:
/// - InMemory database (μοναδικός provider), σταθερό όνομα ώστε να μοιράζεται μεταξύ scopes
/// - Απενεργοποίηση Hangfire/background services
/// - Test authentication (κάθε αίτημα αυθεντικοποιείται ως Admin) ώστε να εκτελείται το
///   σώμα των [Authorize] controllers
/// - Seed βασικών δεδομένων (Client/Supplier/INCI/RawMaterial/Formula με Id=1)
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "IT_" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("DisableBackgroundServices", "true");

        builder.ConfigureServices(services =>
        {
            // Αφαίρεση ΟΛΩΝ των EF Core descriptors (SqlServer provider) -> μόνο InMemory
            var efDescriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<INCISDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(INCISDbContext)
                || (d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") ?? false)
                || (d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") ?? false))
                .ToList();
            foreach (var d in efDescriptors) services.Remove(d);

            services.RemoveAll(typeof(IGlobalConfiguration));

            services.AddDbContext<INCISDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Override auth: force TestScheme ως default (νικά το Identity cookie scheme,
            // επειδή αυτό το Configure τρέχει τελευταίο)
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Seed βασικών δεδομένων (idempotent). Επιστρέφει το factory για chaining.</summary>
    public CustomWebApplicationFactory EnsureSeeded()
    {
        if (_seeded) return this;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        db.Database.EnsureCreated();

        if (!db.Clients.Any())
        {
            var client = new Client { Name = "Test Client", Code = "CL-1" };
            var supplier = new Supplier { Name = "Test Supplier" };
            var inci = new INCI { Name = "Aqua", CASNumber = "7732-18-5" };
            db.Clients.Add(client);
            db.Suppliers.Add(supplier);
            db.INCIs.Add(inci);
            db.SaveChanges();

            db.RawMaterials.Add(new RawMaterial { Code = "RM-1", ProductName = "Glycerin", SupplierId = supplier.Id, Category = "Humectants" });
            db.Formulas.Add(new Formula
            {
                FormulaCode = "TEST-0001-T01",
                ProductName = "Test Cream",
                ProductType = "Leave on",
                ClientId = client.Id
            });
            db.SaveChanges();
        }

        _seeded = true;
        return this;
    }
}
