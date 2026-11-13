using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// End-to-end tests proving the (previously non-functional) Formula Version Control
/// and Formula Template features now work after adding the entities to the EF model.
/// </summary>
public class FormulaVersioningTemplateTests
{
    // ---------------- Formula Version Control ----------------

    [Fact]
    public async Task CreateVersion_Then_History_Works()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaVersionService(context, NullLogger<FormulaVersionService>.Instance);

        var formula = new Formula { FormulaCode = "VER-0001", ProductName = "Cream" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var v1 = await service.CreateVersionAsync(formula.Id, "Initial", "Major", "First draft", "user1");
        var v2 = await service.CreateVersionAsync(formula.Id, "Tweaked", "Minor", "Adjusted %", "user1");

        v1.VersionNumber.Should().Be(1);
        v2.VersionNumber.Should().Be(2);
        v1.Checksum.Should().NotBeNullOrEmpty();

        var history = await service.GetVersionHistoryAsync(formula.Id);
        history.Should().HaveCount(2);
        history.First().VersionNumber.Should().Be(2); // ordered desc

        // Only the latest is current
        history.Single(v => v.IsCurrentVersion).VersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task VerifyVersionIntegrity_DetectsTampering()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaVersionService(context, NullLogger<FormulaVersionService>.Instance);

        var formula = new Formula { FormulaCode = "VER-0002" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var version = await service.CreateVersionAsync(formula.Id, "d", "Major", "r", "u");

        service.VerifyVersionIntegrity(version).Should().BeTrue();

        version.VersionData += "tampered";
        service.VerifyVersionIntegrity(version).Should().BeFalse();
    }

    [Fact]
    public async Task ApproveAndReject_Version_UpdatesStatus()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaVersionService(context, NullLogger<FormulaVersionService>.Instance);

        var formula = new Formula { FormulaCode = "VER-0003" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var version = await service.CreateVersionAsync(formula.Id, "d", "Major", "r", "u");

        (await service.ApproveVersionAsync(version.Id, "approver")).Should().BeTrue();
        (await service.GetVersionAsync(version.Id))!.ApprovalStatus.Should().Be("Approved");

        (await service.RejectVersionAsync(version.Id, "rejector", "not good")).Should().BeTrue();
        var rejected = await service.GetVersionAsync(version.Id);
        rejected!.ApprovalStatus.Should().Be("Rejected");
        rejected.Notes.Should().Contain("not good");
    }

    [Fact]
    public async Task CompareVersions_ProducesComparison()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaVersionService(context, NullLogger<FormulaVersionService>.Instance);

        var formula = new Formula { FormulaCode = "VER-0004" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var v1 = await service.CreateVersionAsync(formula.Id, "d1", "Major", "r", "u");
        var v2 = await service.CreateVersionAsync(formula.Id, "d2", "Minor", "r", "u");

        var comparison = await service.CompareVersionsAsync(v1.Id, v2.Id, "u");

        comparison.Should().NotBeNull();
        comparison.FormulaId.Should().Be(formula.Id);
        comparison.Summary.Should().Contain("v1").And.Contain("v2");
    }

    [Fact]
    public async Task CreateVersion_UnknownFormula_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaVersionService(context, NullLogger<FormulaVersionService>.Instance);

        var act = () => service.CreateVersionAsync(9999, "d", "Major", "r", "u");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------- Formula Templates ----------------

    [Fact]
    public async Task CreateTemplateFromFormula_Then_Apply_Works()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaTemplateService(context, NullLogger<FormulaTemplateService>.Instance);

        var formula = new Formula { FormulaCode = "TPL-0001", ProductName = "Base Cream", ProductType = "Cream" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var template = await service.CreateTemplateFromFormulaAsync(formula.Id, "Cream Base", "TMPL-1", null, "user1");
        template.Id.Should().BeGreaterThan(0);
        template.IsActive.Should().BeTrue();

        var newFormula = await service.ApplyTemplateAsync(template.Id, "TPL-NEW-01", null, "user1");
        newFormula.Id.Should().BeGreaterThan(0);
        newFormula.FormulaCode.Should().Be("TPL-NEW-01");

        // Usage statistics updated
        var updated = (await service.GetTemplatesAsync()).Single(t => t.Id == template.Id);
        updated.UsageCount.Should().Be(1);
    }

    [Fact]
    public async Task RateTemplate_ValidAndInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaTemplateService(context, NullLogger<FormulaTemplateService>.Instance);

        var formula = new Formula { FormulaCode = "TPL-0002", ProductType = "Serum" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();
        var template = await service.CreateTemplateFromFormulaAsync(formula.Id, "Serum", "TMPL-2", null, "u");

        (await service.RateTemplateAsync(template.Id, 4, "u")).Should().BeTrue();

        var act = () => service.RateTemplateAsync(template.Id, 6, "u");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Categories_And_Search_Work()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaTemplateService(context, NullLogger<FormulaTemplateService>.Instance);

        var category = await service.CreateCategoryAsync("Skincare", "Skin products", null, "u");
        category.Id.Should().BeGreaterThan(0);

        var formula = new Formula { FormulaCode = "TPL-0003", ProductType = "Lotion" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();
        await service.CreateTemplateFromFormulaAsync(formula.Id, "Nourishing Lotion", "TMPL-3", category.Id, "u");

        var found = await service.SearchTemplatesAsync("Lotion");
        found.Should().ContainSingle();

        var tree = await service.GetCategoryTreeAsync();
        tree.Should().Contain(c => c.Name == "Skincare");
    }

    [Fact]
    public async Task DeleteTemplate_SoftDeletes()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FormulaTemplateService(context, NullLogger<FormulaTemplateService>.Instance);

        var formula = new Formula { FormulaCode = "TPL-0004" };
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();
        var template = await service.CreateTemplateFromFormulaAsync(formula.Id, "Temp", "TMPL-4", null, "u");

        (await service.DeleteTemplateAsync(template.Id, "u")).Should().BeTrue();

        // Soft-deleted => no longer listed
        (await service.GetTemplatesAsync()).Should().NotContain(t => t.Id == template.Id);
    }
}
