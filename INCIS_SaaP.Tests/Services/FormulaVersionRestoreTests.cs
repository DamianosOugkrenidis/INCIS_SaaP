using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// S-3 (advanced): πραγματικό version comparison και restore (core composition) —
/// όχι no-op. Επαληθεύει scalar fields, raw materials και INCI ingredients.
/// </summary>
public class FormulaVersionRestoreTests
{
    [Fact]
    public async Task RestoreVersion_ReappliesScalarsAndComposition()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaVersionService(context, NullLogger<FormulaVersionService>.Instance);

        var supplier = new Supplier { Name = "S1" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        var rm = new RawMaterial { Code = "RM-RST", ProductName = "Mat", SupplierId = supplier.Id, Price = 1m, Category = "A" };
        context.RawMaterials.Add(rm);
        await context.SaveChangesAsync();

        var formula = new Formula { FormulaCode = "RST-0001", ProductName = "Original", ProductType = "Leave on" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        context.FormulaRawMaterials.Add(new FormulaRawMaterial { FormulaId = formula.Id, RawMaterialId = rm.Id, Percentage = 50m, Phase = "A", IsActive = true });
        context.INCIIngredients.Add(new INCIIngredient { FormulaId = formula.Id, INCIName = "AQUA", Function = "SOLVENT", Percentage = 10m });
        await context.SaveChangesAsync();

        var v1 = await service.CreateVersionAsync(formula.Id, "init", "Major", "r", "u");

        // Mutate formula + composition
        var f = await context.Formulas.FindAsync(formula.Id);
        f!.ProductName = "Modified";
        var rmRow = await context.FormulaRawMaterials.SingleAsync(x => x.FormulaId == formula.Id);
        rmRow.Percentage = 80m;
        context.INCIIngredients.RemoveRange(context.INCIIngredients.Where(x => x.FormulaId == formula.Id));
        context.INCIIngredients.Add(new INCIIngredient { FormulaId = formula.Id, INCIName = "GLYCERIN", Percentage = 5m });
        await context.SaveChangesAsync();

        var v2 = await service.CreateVersionAsync(formula.Id, "mod", "Minor", "r", "u");

        // Compare
        var cmp = await service.CompareVersionsAsync(v1.Id, v2.Id, "u");
        cmp.DifferencesJson.Should().Contain("ProductName").And.Contain("RawMaterials").And.Contain("INCIIngredients");
        cmp.Summary.Should().Contain("difference");

        // Restore v1
        (await service.RestoreVersionAsync(formula.Id, v1.Id, "u")).Should().BeTrue();

        (await context.Formulas.FindAsync(formula.Id))!.ProductName.Should().Be("Original");

        var rms = await context.FormulaRawMaterials.Where(x => x.FormulaId == formula.Id).ToListAsync();
        rms.Should().ContainSingle();
        rms[0].Percentage.Should().Be(50m);

        var incis = await context.INCIIngredients.Where(x => x.FormulaId == formula.Id).ToListAsync();
        incis.Should().ContainSingle(i => i.INCIName == "AQUA");
        incis.Should().NotContain(i => i.INCIName == "GLYCERIN");

        var versions = await service.GetVersionHistoryAsync(formula.Id);
        versions.Should().HaveCount(3);
        versions.Single(v => v.IsCurrentVersion).ChangeType.Should().Be("Restoration");
    }
}
