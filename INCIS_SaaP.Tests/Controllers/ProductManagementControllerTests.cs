using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for the ProductManagement controller (Διαχείριση Προϊόντος).
/// </summary>
public class ProductManagementControllerTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly ProductManagementController _controller;

    public ProductManagementControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new ProductManagementController(_context)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private async Task<Formula> SeedFormula()
    {
        var formula = new Formula { FormulaCode = "TEST-0001-T01", ProductName = "Test Product" };
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();
        return formula;
    }

    [Fact]
    public async Task Index_ShouldReturnViewResult()
    {
        await SeedFormula();
        var result = await _controller.Index(null);
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Manage_ShouldReturnViewWithCategories()
    {
        var formula = await SeedFormula();

        var result = await _controller.Manage(formula.Id) as ViewResult;

        result.Should().NotBeNull();
        var model = result!.Model as ProductManageViewModel;
        model.Should().NotBeNull();
        model!.FormulaId.Should().Be(formula.Id);
        model.Categories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Manage_WithInvalidId_ShouldReturnNotFound()
    {
        var result = await _controller.Manage(9999);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AllergenCreate_Post_ShouldAddAllergen()
    {
        var formula = await SeedFormula();
        var allergen = new Allergen { FormulaId = formula.Id, AllergenName = "Limonene", IsDeclared = true };

        var result = await _controller.AllergenCreate(allergen);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Allergens.Should().ContainSingle(a => a.AllergenName == "Limonene" && a.FormulaId == formula.Id);
    }

    [Fact]
    public async Task AllergenDelete_ShouldRemoveAllergen()
    {
        var formula = await SeedFormula();
        var allergen = new Allergen { FormulaId = formula.Id, AllergenName = "Linalool" };
        _context.Allergens.Add(allergen);
        await _context.SaveChangesAsync();

        var result = await _controller.AllergenDelete(allergen.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Allergens.Any(a => a.Id == allergen.Id).Should().BeFalse();
    }

    [Fact]
    public async Task CompositionAdd_Post_ShouldLinkRawMaterialToFormula()
    {
        var formula = await SeedFormula();
        var rm = new RawMaterial { Code = "RM1", ProductName = "Water" };
        _context.RawMaterials.Add(rm);
        await _context.SaveChangesAsync();

        var line = new FormulaRawMaterial { FormulaId = formula.Id, RawMaterialId = rm.Id, Percentage = 80, Phase = "A" };
        var result = await _controller.CompositionAdd(line);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.FormulaRawMaterials.Should().ContainSingle(f =>
            f.FormulaId == formula.Id && f.RawMaterialId == rm.Id && f.IsActive);
    }

    [Fact]
    public async Task MasterInfoSave_ShouldUpdateFormulaHeaderFields()
    {
        var formula = await SeedFormula();
        var input = new Formula { Id = formula.Id, ManufacturingSite = "Site A", ManufacturingMethod = "Cold process" };

        var result = await _controller.MasterInfoSave(input);

        result.Should().BeOfType<RedirectToActionResult>();
        var updated = await _context.Formulas.FindAsync(formula.Id);
        updated!.ManufacturingSite.Should().Be("Site A");
        updated.ManufacturingMethod.Should().Be("Cold process");
    }

    [Fact]
    public async Task PriceSave_ShouldUpsertPriceInfo()
    {
        var formula = await SeedFormula();
        var input = new PriceInfo { FormulaId = formula.Id, UnitPrice = 12.5m, Currency = "EUR" };

        var result = await _controller.PriceSave(input);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.PriceInfos.Should().ContainSingle(p => p.FormulaId == formula.Id && p.UnitPrice == 12.5m);
    }

    [Fact]
    public async Task GenerateInci_ShouldComputeInciFromComposition()
    {
        var formula = await SeedFormula();
        var inci = new INCI { Name = "AQUA", CASNumber = "7732-18-5" };
        _context.INCIs.Add(inci);
        var rm = new RawMaterial { Code = "RM1", ProductName = "Water" };
        _context.RawMaterials.Add(rm);
        await _context.SaveChangesAsync();

        _context.Set<RawMaterialComposition>().Add(new RawMaterialComposition
        {
            RawMaterialId = rm.Id, INCIId = inci.Id, MinPercentage = 100, MaxPercentage = 100, IsImpurity = false
        });
        _context.FormulaRawMaterials.Add(new FormulaRawMaterial { FormulaId = formula.Id, RawMaterialId = rm.Id, Percentage = 90, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _controller.GenerateInci(formula.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.INCIIngredients.Should().ContainSingle(i => i.FormulaId == formula.Id && i.INCIName == "AQUA");
    }

    [Fact]
    public async Task CopyFrom_ShouldCopySelectedCategories()
    {
        var target = await SeedFormula();
        var source = new Formula { FormulaCode = "SRC-0001-T01", ProductName = "Source" };
        _context.Formulas.Add(source);
        await _context.SaveChangesAsync();
        _context.Allergens.Add(new Allergen { FormulaId = source.Id, AllergenName = "Citral" });
        await _context.SaveChangesAsync();

        var result = await _controller.CopyFrom(target.Id, source.Id, new[] { "allergens" });

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Allergens.Should().ContainSingle(a => a.FormulaId == target.Id && a.AllergenName == "Citral");
    }

    [Fact]
    public async Task ToxCreate_ShouldAutoComputeMoSAndSafety()
    {
        var formula = await SeedFormula();
        var tox = new ToxicologicalData { FormulaId = formula.Id, InciName = "X", NOAEL = 100, SED = 1 };

        var result = await _controller.ToxCreate(tox);

        result.Should().BeOfType<RedirectToActionResult>();
        var saved = _context.ToxicologicalData.Single(t => t.FormulaId == formula.Id);
        saved.MoS.Should().Be(100);
        saved.IsSafe.Should().BeTrue();
    }

    // ── Multi-tenant data isolation ──
    private static Data.INCISDbContext CreateContextForTenant(string dbName, string userId, int? tenantId, bool admin = false)
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Data.INCISDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
        };
        if (admin) claims.Add(new(System.Security.Claims.ClaimTypes.Role, INCIS_SaaP.Models.AppRoles.Admin));
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        if (tenantId.HasValue) httpContext.Items["TenantId"] = tenantId.Value;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);

        return new Data.INCISDbContext(options, accessor.Object);
    }

    [Fact]
    public async Task MultiTenantIsolation_TenantSeesOnlyOwnData()
    {
        var db = "isolation-" + Guid.NewGuid();

        // Tenant 1 (user A) δημιουργεί φόρμουλα
        using (var t1 = CreateContextForTenant(db, "userA", tenantId: 1))
        {
            t1.Formulas.Add(new Formula { FormulaCode = "T1-0001-T01", ProductName = "Tenant1 Product" });
            await t1.SaveChangesAsync();
        }

        // Tenant 2 δεν βλέπει τα δεδομένα του Tenant 1
        using (var t2 = CreateContextForTenant(db, "userB", tenantId: 2))
        {
            (await t2.Formulas.CountAsync()).Should().Be(0);
        }

        // Άλλος χρήστης ΙΔΙΟΥ tenant (1) βλέπει τα κοινά δεδομένα της εταιρείας
        using (var t1b = CreateContextForTenant(db, "userC", tenantId: 1))
        {
            var shared = await t1b.Formulas.ToListAsync();
            shared.Should().ContainSingle(f => f.FormulaCode == "T1-0001-T01");
            shared[0].TenantId.Should().Be(1);
        }

        // Admin βλέπει τα πάντα
        using (var ctxAdmin = CreateContextForTenant(db, "admin-1", tenantId: null, admin: true))
        {
            (await ctxAdmin.Formulas.CountAsync()).Should().Be(1);
        }
    }

    [Fact]
    public async Task MultiTenantIsolation_SubEntitiesProtectedFromIdor()
    {
        var db = "isolation-sub-" + Guid.NewGuid();
        int allergenId;

        // Tenant 1 δημιουργεί φόρμουλα + αλλεργιογόνο
        using (var t1 = CreateContextForTenant(db, "userA", tenantId: 1))
        {
            var f = new Formula { FormulaCode = "T1-SUB-T01", ProductName = "P", TenantId = 1 };
            t1.Formulas.Add(f);
            await t1.SaveChangesAsync();
            var a = new Allergen { FormulaId = f.Id, AllergenName = "Limonene" };
            t1.Allergens.Add(a);
            await t1.SaveChangesAsync();
            allergenId = a.Id;
        }

        // Tenant 2 ΔΕΝ μπορεί να δει/προσπελάσει το αλλεργιογόνο του Tenant 1 (IDOR)
        using (var t2 = CreateContextForTenant(db, "userB", tenantId: 2))
        {
            (await t2.Allergens.AnyAsync(a => a.Id == allergenId)).Should().BeFalse();
            (await t2.Allergens.FirstOrDefaultAsync(a => a.Id == allergenId)).Should().BeNull();
        }

        // Ίδιος tenant το βλέπει
        using (var t1b = CreateContextForTenant(db, "userC", tenantId: 1))
        {
            (await t1b.Allergens.AnyAsync(a => a.Id == allergenId)).Should().BeTrue();
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
