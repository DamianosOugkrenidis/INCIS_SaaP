using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using Xunit;

namespace INCIS_SaaP.Tests.Integration;

/// <summary>
/// Fixture για integration tests σε πραγματικό SQL Server, σε ΑΠΟΜΟΝΩΜΕΝΗ βάση (fresh per run).
/// Δεν αγγίζει ποτέ την παραγωγική βάση: δημιουργεί/διαγράφει δική της βάση με μοναδικό όνομα.
/// Αν δεν υπάρχει διαθέσιμος SQL Server, τα tests κάνουν soft-skip (Available=false).
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public bool Available { get; private set; }
    public string? SkipReason { get; private set; }
    public string DatabaseName { get; private set; } = string.Empty;
    public string ConnectionString { get; private set; } = string.Empty;
    public DbContextOptions<INCISDbContext> Options { get; private set; } = null!;

    private static string ServerConnection =>
        Environment.GetEnvironmentVariable("INCIS_TEST_SQL_CONNECTION")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? "Server=.\\SQLEXPRESS02;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    public INCISDbContext CreateContext() => new(Options, new HttpContextAccessor());

    /// <summary>Καθαρίζει τους πίνακες που αγγίζουν τα tests, για ανεξαρτησία μεταξύ τους.</summary>
    public static Task ResetAsync(INCISDbContext ctx) =>
        ctx.Database.ExecuteSqlRawAsync(
            "DELETE FROM FormulaRawMaterials; DELETE FROM RawMaterials; DELETE FROM Formulas; " +
            "DELETE FROM INCIs; DELETE FROM CosIngIngredients; DELETE FROM CosIngEntries;");

    public async Task InitializeAsync()
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(ServerConnection);
            if (string.IsNullOrWhiteSpace(builder.DataSource)) throw new InvalidOperationException("no datasource");

            using (var conn = new SqlConnection(builder.ConnectionString))
            {
                await conn.OpenAsync();
            }

            DatabaseName = "INCIS_SaaP_Tests_" + Guid.NewGuid().ToString("N")[..12];
            var b2 = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = DatabaseName };
            ConnectionString = b2.ConnectionString;

            Options = new DbContextOptionsBuilder<INCISDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            await using var ctx = CreateContext();
            await ctx.Database.MigrateAsync();   // δημιουργεί τη βάση + εφαρμόζει migrations
            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason = $"SQL Server μη διαθέσιμος: {ex.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (!Available) return;
        try
        {
            await using var ctx = CreateContext();
            await ctx.Database.EnsureDeletedAsync();
        }
        catch { /* best-effort cleanup */ }
    }
}
