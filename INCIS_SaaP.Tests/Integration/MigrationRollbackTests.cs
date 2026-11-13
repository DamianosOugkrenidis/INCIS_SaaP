using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.Tests.Integration;

/// <summary>
/// Επαληθεύει ότι το migration-DOWN είναι πλέον ΑΣΦΑΛΕΣ (μη-καταστροφικό) ακόμη και με
/// δεδομένα Name > 200 χαρ. (boundary 200 / 201 / 500 / 1903), χωρίς SQL error 2628 και
/// χωρίς απώλεια/περικοπή των αποθηκευμένων ονομάτων.
/// </summary>
[Trait("Category", "SqlIntegration")]
public class MigrationRollbackTests : IClassFixture<SqlServerFixture>
{
    private const string PreviousMigration = "20260702214036_AddFormulaApprovalWorkflow";

    private readonly SqlServerFixture _fx;

    public MigrationRollbackTests(SqlServerFixture fx) => _fx = fx;

    [Fact]
    public async Task Down_And_Up_Succeeds_With_Names_Over_200_Without_Truncation()
    {
        if (!_fx.Available) return;

        await using var ctx = _fx.CreateContext();
        await SqlServerFixture.ResetAsync(ctx);

        var names = new[]
        {
            new string('A', 200),   // boundary (χωράει στο παλιό όριο)
            new string('B', 201),   // οριακά πάνω από 200
            new string('C', 500)    // μέγιστο τρέχον (Name nvarchar(500))
        };

        // Το πλήρες (1903 χαρ.) ζει στο FullName (nvarchar(max)) — δεν επηρεάζει το Name.
        var longFullName = new string('D', 1903) + "TAIL";

        ctx.INCIs.Add(new INCI { Name = names[0], FullName = names[0], IsActive = true });
        ctx.INCIs.Add(new INCI { Name = names[1], FullName = names[1], IsActive = true });
        ctx.INCIs.Add(new INCI { Name = names[2], FullName = longFullName, IsActive = true });
        await ctx.SaveChangesAsync();

        var migrator = ctx.GetInfrastructure().GetRequiredService<IMigrator>();

        // ---- DOWN στη προηγούμενη migration (πρέπει να πετύχει χωρίς 2628) ----
        await migrator.MigrateAsync(PreviousMigration);

        var afterDown = await ctx.Database
            .SqlQueryRaw<string>("SELECT CONCAT(COUNT(*), '|', MAX(LEN(Name))) AS Value FROM INCIs")
            .SingleAsync();
        afterDown.Should().Be("3|500", "όλα τα ονόματα διατηρούνται χωρίς truncation");

        var migrationsAfterDown = (await ctx.Database.GetAppliedMigrationsAsync()).Count();
        migrationsAfterDown.Should().Be(13);

        // ---- UP ξανά στην τελευταία ----
        await migrator.MigrateAsync();

        var afterUp = await ctx.Database
            .SqlQueryRaw<string>("SELECT CONCAT(COUNT(*), '|', MAX(LEN(Name))) AS Value FROM INCIs")
            .SingleAsync();
        afterUp.Should().Be("3|500");

        var migrationsAfterUp = (await ctx.Database.GetAppliedMigrationsAsync()).Count();
        migrationsAfterUp.Should().Be(ctx.Database.GetMigrations().Count(), "όλα τα migrations εφαρμόζονται μετά το UP");
    }

    private static async Task<HashSet<string>> ColumnsAsync(INCIS_SaaP.Data.INCISDbContext ctx) =>
        (await ctx.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sys.columns WHERE object_id = OBJECT_ID('INCIs')").ToListAsync())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Πλήρης χαρακτηρισμός του DOWN: ποιες στήλες αφαιρούνται (additive), ποιες διατηρούνται
    /// και ότι τα surviving δεδομένα μένουν ΑΜΕΤΑΒΛΗΤΑ. Τα additive πεδία (FullName κ.λπ.)
    /// χάνονται με το migration-down -> για πλήρη διατήρηση απαιτείται restore.
    /// </summary>
    [Fact]
    public async Task Down_RemovesAdditiveFields_ButPreservesSurvivingData()
    {
        if (!_fx.Available) return;

        await using var ctx = _fx.CreateContext();
        await SqlServerFixture.ResetAsync(ctx);

        var longFull = new string('Z', 1903);

        ctx.INCIs.Add(new INCI
        {
            Name = new string('N', 500),
            CASNumber = new string('1', 200),
            ECNumber = new string('2', 200),
            Origin = "Synthetic",
            Restriction = "III/226",
            ToxicologicalProfile = "tox profile value",
            FullName = longFull,
            DataSource = "ALL_INCIS_NEW_FILE",
            ImportedAt = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc),
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var survivingFingerprintBefore = await ctx.Database.SqlQueryRaw<string>(
            "SELECT CONVERT(varchar(40), ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(Name, CASNumber, ECNumber)),0)) AS Value FROM INCIs")
            .SingleAsync();

        var migrator = ctx.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration); // DOWN

        var colsAfterDown = await ColumnsAsync(ctx);
        colsAfterDown.Should().Contain(new[] { "Name", "CASNumber", "ECNumber" });
        colsAfterDown.Should().NotContain(new[]
        {
            "FullName", "Origin", "Restriction", "ToxicologicalProfile", "DataSource", "ImportedAt"
        }, "το DOWN αφαιρεί τα additive πεδία (feature rollback)");

        var survivingFingerprintAfter = await ctx.Database.SqlQueryRaw<string>(
            "SELECT CONVERT(varchar(40), ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(Name, CASNumber, ECNumber)),0)) AS Value FROM INCIs")
            .SingleAsync();
        survivingFingerprintAfter.Should().Be(survivingFingerprintBefore,
            "τα surviving πεδία δεν πρέπει να αλλάξουν/περικοπούν");

        // ---- UP: επιστρέφουν οι στήλες· τα additive δεδομένα είναι πλέον NULL (χάθηκαν με το down) ----
        await migrator.MigrateAsync();
        var colsAfterUp = await ColumnsAsync(ctx);
        colsAfterUp.Should().Contain(new[]
        {
            "FullName", "Origin", "Restriction", "ToxicologicalProfile", "DataSource", "ImportedAt"
        });

        var survivingFingerprintAfterUp = await ctx.Database.SqlQueryRaw<string>(
            "SELECT CONVERT(varchar(40), ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(Name, CASNumber, ECNumber)),0)) AS Value FROM INCIs")
            .SingleAsync();
        survivingFingerprintAfterUp.Should().Be(survivingFingerprintBefore);

        var fullNameNotNull = await ctx.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM INCIs WHERE FullName IS NOT NULL").SingleAsync();
        fullNameNotNull.Should().Be(0, "ΤΕΚΜΗΡΙΩΣΗ: το migration-down χάνει τα additive πεδία· πλήρης διατήρηση = restore-based");
    }
}
