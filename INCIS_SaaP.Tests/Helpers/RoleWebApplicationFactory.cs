using Hangfire;
using Microsoft.AspNetCore.Antiforgery;
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
/// WebApplicationFactory με ρυθμιζόμενο σενάριο αυθεντικοποίησης (anonymous / ρόλοι),
/// για ελέγχους εξουσιοδότησης/ασφάλειας. InMemory DB + seed βασικών δεδομένων.
/// </summary>
public class RoleWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "IT_Role_" + Guid.NewGuid();
    private bool _seeded;

    public TestAuthScenario Scenario { get; }

    public bool DisableAntiforgery { get; }

    public IReadOnlyDictionary<string, string> Settings { get; }

    public RoleWebApplicationFactory(
        Action<TestAuthScenario>? configure = null,
        bool disableAntiforgery = false,
        IReadOnlyDictionary<string, string>? settings = null)
    {
        Scenario = new TestAuthScenario();
        configure?.Invoke(Scenario);
        DisableAntiforgery = disableAntiforgery;
        Settings = settings ?? new Dictionary<string, string>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("DisableBackgroundServices", "true");
        foreach (var kv in Settings) builder.UseSetting(kv.Key, kv.Value);

        builder.ConfigureServices(services =>
        {
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

            services.AddSingleton(Scenario);

            if (DisableAntiforgery)
            {
                services.RemoveAll<IAntiforgery>();
                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();
            }

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = RoleTestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = RoleTestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = RoleTestAuthHandler.SchemeName;
                options.DefaultForbidScheme = RoleTestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, RoleTestAuthHandler>(RoleTestAuthHandler.SchemeName, _ => { });
        });
    }

    public RoleWebApplicationFactory EnsureSeeded()
    {
        if (_seeded) return this;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        db.Database.EnsureCreated();

        if (!db.Clients.Any())
        {
            var client = new Client { Name = "Test Client", Code = "CL-1" };
            var supplier = new Supplier { Name = "Test Supplier" };
            db.Clients.Add(client);
            db.Suppliers.Add(supplier);
            db.INCIs.Add(new INCI { Name = "Aqua", CASNumber = "7732-18-5" });
            db.CosIngIngredients.Add(new CosIngIngredient
            {
                InciName = "AQUA",
                CASNumber = "7732-18-5",
                Function = "SOLVENT",
                DataSource = "github_csv_2016"
            });
            db.CosIngEntries.Add(new CosIngEntry
            {
                InciName = "AQUA",
                Annex = "II",
                EntryNumber = "1",
                DataSource = "cosingchecker_api_2026"
            });
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
