using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Ξεκινά την εφαρμογή σε πραγματικό Kestrel (τυχαία θύρα) με ΑΠΟΜΟΝΩΜΕΝΗ βάση δεδομένων
/// (μοναδικό όνομα, διαγράφεται στο τέλος) και seeded admin, ώστε τα Playwright tests
/// να εκτελούν πραγματικές ροές UI. Δεν αγγίζει την παραγωγική βάση.
/// </summary>
public sealed class AppServerFixture : IAsyncLifetime
{
    private static readonly string SqlServer =
        Environment.GetEnvironmentVariable("INCIS_TEST_SQL_CONNECTION")
        ?? @"Server=.\SQLEXPRESS02;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly string _dbName = "INCIS_SaaP_PW_" + Guid.NewGuid().ToString("N")[..10];
    private Process? _proc;

    public string BaseUrl { get; private set; } = "";
    public string AdminEmail { get; } = "pw.admin@incis.local";
    public string AdminPassword { get; } = "PwT3st#2026!";

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public int ClientId { get; private set; }
    public int FormulaId { get; private set; }

    private DbContextOptions<INCISDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<INCISDbContext>().UseSqlServer(ConnectionString).Options;

    /// <summary>Seed βασικών δεδομένων (Client/Supplier/RawMaterial/Formula) μετά το startup.</summary>
    private async Task SeedAsync()
    {
        await using var ctx = new INCISDbContext(BuildOptions(), new HttpContextAccessor());

        var client = new Client { Name = "PW Client", Code = "PWC" };
        var supplier = new Supplier { Name = "PW Supplier" };
        ctx.Clients.Add(client);
        ctx.Suppliers.Add(supplier);
        await ctx.SaveChangesAsync();

        var rm = new RawMaterial { Code = "PW-RM", ProductName = "Glycerin", SupplierId = supplier.Id, Category = "Humectants", Price = 5m };
        ctx.RawMaterials.Add(rm);
        await ctx.SaveChangesAsync();

        var formula = new Formula { FormulaCode = "PW-0001-T01", ProductName = "PW Cream", ProductType = "Leave on", ClientId = client.Id };
        ctx.Formulas.Add(formula);
        await ctx.SaveChangesAsync();

        ctx.FormulaRawMaterials.Add(new FormulaRawMaterial { FormulaId = formula.Id, RawMaterialId = rm.Id, Percentage = 100m, Phase = "A", IsActive = true });
        ctx.INCIIngredients.Add(new INCIIngredient { FormulaId = formula.Id, INCIName = "AQUA", Function = "SOLVENT", Percentage = 100m });
        await ctx.SaveChangesAsync();

        ClientId = client.Id;
        FormulaId = formula.Id;
    }

    public string ConnectionString =>
        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(SqlServer) { InitialCatalog = _dbName }.ConnectionString;

    public async Task InitializeAsync()
    {
        var appDll = typeof(INCIS_SaaP.Controllers.HomeController).Assembly.Location;
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var psi = new ProcessStartInfo("dotnet", $"\"{appDll}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(appDll)!
        };
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["ASPNETCORE_URLS"] = BaseUrl;
        psi.Environment["DOTNET_ROLL_FORWARD"] = "Major";
        psi.Environment["ConnectionStrings__DefaultConnection"] = ConnectionString;
        psi.Environment["INCIS_DB_CONNECTION_STRING"] = ConnectionString;
        psi.Environment["DisableBackgroundServices"] = "true";
        psi.Environment["AdminSeed__Email"] = AdminEmail;
        psi.Environment["AdminSeed__Password"] = AdminPassword;
        psi.Environment["Jwt__Secret"] = "PlaywrightSecretKey_1234567890_MustBeLongEnough";
        _proc = Process.Start(psi)!;
        _ = _proc.StandardOutput.ReadToEndAsync();
        _ = _proc.StandardError.ReadToEndAsync();

        await WaitForHealthyAsync(TimeSpan.FromSeconds(120));
        await SeedAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        try { if (Browser != null) await Browser.CloseAsync(); } catch { }
        try { Playwright?.Dispose(); } catch { }

        try
        {
            if (_proc is { HasExited: false }) _proc.Kill(entireProcessTree: true);
        }
        catch { }

        try
        {
            await using var ctx = new INCISDbContext(BuildOptions(), new HttpContextAccessor());
            await ctx.Database.EnsureDeletedAsync();
        }
        catch { }
    }

    public async Task<IPage> NewPageAsync() => await Browser.NewPageAsync();

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private async Task WaitForHealthyAsync(TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                var res = await http.GetAsync($"{BaseUrl}/health");
                if (res.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Η εφαρμογή δεν ξεκίνησε εντός {timeout}.");
    }
}

[CollectionDefinition("playwright")]
public class PlaywrightCollection : ICollectionFixture<AppServerFixture> { }
