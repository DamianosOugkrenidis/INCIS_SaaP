using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using Xunit;

namespace INCIS_SaaP.Tests.Integration;

/// <summary>
/// Integration tests σε πραγματικό SQL Server, απομονωμένη βάση (βλ. <see cref="SqlServerFixture"/>).
/// Καλύπτουν: migrations/σχήμα, εισαγωγή INCI, idempotency, αναζήτηση, και μη-παρέμβαση στα CosIng.
/// </summary>
[Trait("Category", "SqlIntegration")]
public class SqlServerIntegrationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fx;

    public SqlServerIntegrationTests(SqlServerFixture fx) => _fx = fx;

    // ------------------------------------------------------------------
    private static Stream MakeWorkbook(params string[][] rows)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("printing");
        ws.Cell(1, 1).Value = "INCI list";
        var headers = new[] { "a/n", "Name", "Origin", "Cas", "Einecs", "Tox. Profile", "Restriction", "Functions" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(3, i + 1).Value = headers[i];
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 4, c + 1).Value = rows[r][c];
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static string? FindAllIncisFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "ALL_INCIS_NEW_FILE.xls");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    // ------------------------------------------------------------------
    [Fact]
    public async Task Migrations_AreApplied_AndNewMigrationsPresent()
    {
        if (!_fx.Available) return;

        await using var ctx = _fx.CreateContext();
        var applied = (await ctx.Database.GetAppliedMigrationsAsync()).ToList();

        applied.Should().Contain("20261020132411_AddInciCatalogFields");
        applied.Should().Contain("20261020133742_EnlargeInciCatalogFields");
        applied.Should().Contain("20261020141235_AddInciFullName");
    }

    [Fact]
    public async Task Schema_IncisHasExpectedColumnTypes()
    {
        if (!_fx.Available) return;

        await using var ctx = _fx.CreateContext();
        var cols = await ctx.Database
            .SqlQueryRaw<string>(
                "SELECT c.name AS Value FROM sys.columns c WHERE c.object_id = OBJECT_ID('INCIs')")
            .ToListAsync();

        cols.Should().Contain(new[] { "Name", "CASNumber", "ECNumber", "Origin", "Restriction", "ToxicologicalProfile", "FullName", "DataSource", "ImportedAt" });
    }

    [Fact]
    public async Task Import_PersistsFields_AndPreservesFullName()
    {
        if (!_fx.Available) return;

        var longName = new string('Z', 600) + "-TAIL";
        await using var ctx = _fx.CreateContext();
        await SqlServerFixture.ResetAsync(ctx);
        var svc = new InciCatalogImportService(ctx, NullLogger<InciCatalogImportService>.Instance);

        using var wb = MakeWorkbook(
            new[] { "1", "SQLTEST AQUA", "None", "7732-18-5", "231-791-2", "tox text", "III/15a", "SOLVENT" },
            new[] { "2", longName, "None", "0", "-", "", "", "SOLVENT" });

        var result = await svc.ImportAsync(wb, "synthetic.xls");

        result.Success.Should().BeTrue();
        result.Inserted.Should().Be(2);

        var aqua = await ctx.INCIs.SingleAsync(i => i.Name == "SQLTEST AQUA");
        aqua.CASNumber.Should().Be("7732-18-5");
        aqua.ECNumber.Should().Be("231-791-2");
        aqua.Function.Should().Be("SOLVENT");
        aqua.Restriction.Should().Be("III/15a");
        aqua.DataSource.Should().Be(InciCatalogImportService.DataSourceName);

        var longRow = await ctx.INCIs.SingleAsync(i => i.FullName == longName);
        longRow.Name.Length.Should().Be(500);          // ευρετηριοποιήσιμο κλειδί
        longRow.FullName.Should().Be(longName);        // χωρίς απώλεια
        longRow.CASNumber.Should().BeNull();           // placeholder "0" -> null
        longRow.ECNumber.Should().BeNull();            // placeholder "-" -> null
    }

    [Fact]
    public async Task Import_IsIdempotent_OnSqlServer()
    {
        if (!_fx.Available) return;

        await using var ctx = _fx.CreateContext();

        await SqlServerFixture.ResetAsync(ctx);
        var svc = new InciCatalogImportService(ctx, NullLogger<InciCatalogImportService>.Instance);

        static Stream Wb() => MakeWorkbook(
            new[] { "1", "IDEMPOTENT X", "None", "1-1-1", "", "", "", "A" });

        using (var s1 = Wb()) { await svc.ImportAsync(s1, "a.xls"); }
        using (var s2 = Wb()) { var r2 = await svc.ImportAsync(s2, "b.xls"); r2.Inserted.Should().Be(0); r2.Updated.Should().Be(1); }

        (await ctx.INCIs.CountAsync(i => i.Name == "IDEMPOTENT X")).Should().Be(1);
    }

    [Fact]
    public async Task Search_ByName_FullName_Cas_Ec_WorksOnSqlServer()
    {
        if (!_fx.Available) return;

        var prefix = new string('Q', 500);
        var longName = prefix + "SQLTAILMARKER";

        await using var ctx = _fx.CreateContext();

        await SqlServerFixture.ResetAsync(ctx);
        var svc = new InciCatalogImportService(ctx, NullLogger<InciCatalogImportService>.Instance);
        using var wb = MakeWorkbook(
            new[] { "1", "SEARCHABLE ALOE", "None", "9999-88-7", "222-333-4", "", "", "A" },
            new[] { "2", longName, "None", "0", "", "", "", "B" });
        await svc.ImportAsync(wb, "s.xls");

        // Name search (case-insensitive στο SQL collation)
        (await ctx.INCIs.CountAsync(i => i.Name.Contains("ALOE"))).Should().Be(1);
        // CAS / EC
        (await ctx.INCIs.CountAsync(i => i.CASNumber!.Contains("9999-88-7"))).Should().Be(1);
        (await ctx.INCIs.CountAsync(i => i.ECNumber!.Contains("222-333-4"))).Should().Be(1);
        // FullName tail (πέραν των 500 χαρ.)
        (await ctx.INCIs.CountAsync(i => i.FullName!.Contains("SQLTAILMARKER"))).Should().Be(1);
        // Το κολοβωμένο κλειδί ΔΕΝ περιέχει το tail
        (await ctx.INCIs.CountAsync(i => i.Name.Contains("SQLTAILMARKER"))).Should().Be(0);
    }

    [Fact]
    public async Task InciImport_DoesNotAffect_CosIng()
    {
        if (!_fx.Available) return;

        await using var ctx = _fx.CreateContext();

        await SqlServerFixture.ResetAsync(ctx);
        ctx.CosIngIngredients.Add(new CosIngIngredient { InciName = "AQUA", CASNumber = "7732-18-5", Function = "SOLVENT", DataSource = "github_csv_2016" });
        ctx.CosIngIngredients.Add(new CosIngIngredient { InciName = "GLYCERIN", CASNumber = "56-81-5", Function = "HUMECTANT", DataSource = "github_csv_2016" });
        ctx.CosIngEntries.Add(new CosIngEntry { InciName = "AQUA", Annex = "II", EntryNumber = "1", DataSource = "cosingchecker_api_2026" });
        await ctx.SaveChangesAsync();

        var svc = new InciCatalogImportService(ctx, NullLogger<InciCatalogImportService>.Instance);
        using var wb = MakeWorkbook(new[] { "1", "COSING REGRESSION X", "None", "1", "", "", "", "A" });
        await svc.ImportAsync(wb, "x.xls");

        (await ctx.CosIngIngredients.CountAsync()).Should().Be(2);
        (await ctx.CosIngEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RealAllIncisFile_FullImport_IsIdempotent_AndLossless()
    {
        if (!_fx.Available) return;

        var file = FindAllIncisFile();
        if (file == null) return; // soft-skip αν λείπει το αρχείο-πηγή

        await using var ctx = _fx.CreateContext();

        await SqlServerFixture.ResetAsync(ctx);
        var svc = new InciCatalogImportService(ctx, NullLogger<InciCatalogImportService>.Instance);

        var r1 = await svc.ImportFromFileAsync(file);
        r1.Success.Should().BeTrue();
        r1.TotalDataRows.Should().BeGreaterThan(19000);
        r1.Inserted.Should().BeGreaterThan(19000);
        r1.ErrorRows.Should().Be(0);

        var total = await ctx.INCIs.CountAsync();
        var distinctFull = await ctx.INCIs.Select(i => i.FullName).Distinct().CountAsync();
        total.Should().Be(distinctFull);                     // χωρίς διπλοεγγραφές
        (await ctx.INCIs.AnyAsync(i => i.Name != i.FullName)).Should().BeTrue("υπάρχουν σύνθετα ονόματα >500 χαρ.");

        var r2 = await svc.ImportFromFileAsync(file);
        r2.Inserted.Should().Be(0);
        r2.Updated.Should().Be(total);

        (await ctx.INCIs.CountAsync()).Should().Be(total);   // σταθερό
    }

    private static readonly string[] IntegrityTables =
    {
        "CosIngIngredients", "CosIngEntries", "Clients", "Suppliers", "RawMaterials", "Formulas",
        "Restrictions", "AspNetUsers", "AspNetRoles"
    };

    // Fingerprint ανά πίνακα. Για AspNetUsers/Roles ΧΡΗΣΙΜΟΠΟΙΟΥΝΤΑΙ ΜΟΝΟ μη-ευαίσθητες στήλες
    // (ποτέ PasswordHash/SecurityStamp) ώστε να μην εκτίθενται κωδικοί/tokens.
    private static readonly Dictionary<string, string> FingerprintColumns = new()
    {
        { "AspNetUsers", "Id, UserName, Email, EmailConfirmed, AccessFailedCount" },
        { "AspNetRoles", "Id, Name, NormalizedName" }
    };

    private static async Task<Dictionary<string, string>> SnapshotAsync(
        INCIS_SaaP.Data.INCISDbContext ctx, IEnumerable<string> tables)
    {
        var dict = new Dictionary<string, string>();
        foreach (var t in tables)
        {
            var cols = FingerprintColumns.TryGetValue(t, out var c) ? c : "*";
#pragma warning disable EF1002 // internal constants (table/column names), not user input
            var v = await ctx.Database
                .SqlQueryRaw<string>($"SELECT CONCAT(COUNT(*), '|', ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM({cols})), 0)) AS Value FROM [{t}]")
                .ToListAsync();
#pragma warning restore EF1002
            dict[t] = v.Single();
        }
        return dict;
    }

    /// <summary>
    /// Ακεραιότητα δεδομένων: μετά την ΠΛΗΡΗ εισαγωγή του ALL_INCIS_NEW_FILE, οι πίνακες
    /// CosIng/Annex/Clients/Suppliers/RawMaterials/Formulas πρέπει να είναι ΑΚΡΙΒΩΣ ίδιοι
    /// (πλήθος + fingerprint περιεχομένου) πριν και μετά.
    /// </summary>
    [Fact]
    public async Task DataIntegrity_FullImport_DoesNotChange_PreexistingEntities()
    {
        if (!_fx.Available) return;
        var file = FindAllIncisFile();
        if (file == null) return;

        await using var ctx = _fx.CreateContext();
        await SqlServerFixture.ResetAsync(ctx);

        // Seed προϋπαρχόντων δεδομένων σε όλα τα υποσυστήματα
        var client = new INCIS_SaaP.Models.Client { Name = "Integrity Client", Code = "IC-1" };
        var supplier = new INCIS_SaaP.Models.Supplier { Name = "Integrity Supplier" };
        ctx.Clients.Add(client);
        ctx.Suppliers.Add(supplier);
        ctx.CosIngIngredients.Add(new INCIS_SaaP.Models.CosIngIngredient { InciName = "AQUA", CASNumber = "7732-18-5", Function = "SOLVENT", DataSource = "github_csv_2016" });
        ctx.CosIngIngredients.Add(new INCIS_SaaP.Models.CosIngIngredient { InciName = "GLYCERIN", CASNumber = "56-81-5", Function = "HUMECTANT", DataSource = "github_csv_2016" });
        ctx.CosIngEntries.Add(new INCIS_SaaP.Models.CosIngEntry { InciName = "PROHIBITED X", Annex = "II", EntryNumber = "1", DataSource = "cosingchecker_api_2026" });
        ctx.CosIngEntries.Add(new INCIS_SaaP.Models.CosIngEntry { InciName = "RESTRICTED Y", Annex = "III", EntryNumber = "2", DataSource = "cosingchecker_api_2026" });
        await ctx.SaveChangesAsync();

        ctx.RawMaterials.Add(new INCIS_SaaP.Models.RawMaterial { Code = "IRM-1", ProductName = "Glycerin", SupplierId = supplier.Id, Category = "Humectants", Price = 5m });
        ctx.Formulas.Add(new INCIS_SaaP.Models.Formula { FormulaCode = "INT-0001-T01", ProductName = "Integrity Cream", ClientId = client.Id });
        await ctx.SaveChangesAsync();

        // Restrictions (FK -> INCI)
        var seedInci = new INCIS_SaaP.Models.INCI { Name = "INTEGRITY SEED INCI", IsActive = true };
        ctx.INCIs.Add(seedInci);
        await ctx.SaveChangesAsync();
        ctx.Restrictions.Add(new INCIS_SaaP.Models.Restriction
        {
            INCIId = seedInci.Id,
            Regulation = "1223/2009",
            Description = "seed restriction",
            MaxPercentage = 1m,
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        // Identity rows με ΣΥΝΘΕΤΙΚΑ (μη-ευαίσθητα) δεδομένα — χωρίς πραγματικούς κωδικούς/tokens.
        await ctx.Database.ExecuteSqlRawAsync(
            "IF NOT EXISTS (SELECT 1 FROM AspNetRoles) " +
            "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-test-1', N'TestRole', N'TESTROLE', N'cs');" +
            "IF NOT EXISTS (SELECT 1 FROM AspNetUsers) " +
            "INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, CreatedAt, IsActive, MustChangePassword, IsSelfRegistered, IsApproved) " +
            "VALUES (N'user-test-1', N'integrity@test.local', N'INTEGRITY@TEST.LOCAL', N'integrity@test.local', N'INTEGRITY@TEST.LOCAL', 1, N'<synthetic-no-secret>', N'ss', N'cs', 0, 0, 1, 0, SYSUTCDATETIME(), 1, 0, 0, 1);");

        var before = await SnapshotAsync(ctx, IntegrityTables);

        var svc = new InciCatalogImportService(ctx, NullLogger<InciCatalogImportService>.Instance);
        var result = await svc.ImportFromFileAsync(file);
        result.Success.Should().BeTrue();

        var after = await SnapshotAsync(ctx, IntegrityTables);

        foreach (var t in IntegrityTables)
            after[t].Should().Be(before[t], $"ο πίνακας {t} δεν πρέπει να μεταβληθεί από το INCI import");

        (await ctx.INCIs.CountAsync()).Should().BeGreaterThan(19000);
    }
}
