using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// End-to-end tests proving the (previously non-functional) Formula Approval Workflow
/// now works after adding the entities to the EF model.
/// </summary>
public class FormulaApprovalServiceTests
{
    private static (FormulaApprovalService service, Data.INCISDbContext ctx) Create()
    {
        var ctx = TestDbContextFactory.Create();
        return (new FormulaApprovalService(ctx, NullLogger<FormulaApprovalService>.Instance), ctx);
    }

    private static async Task<Formula> SeedFormula(Data.INCISDbContext ctx, string code)
    {
        var f = new Formula { FormulaCode = code, ProductName = "P" };
        ctx.Formulas.Add(f);
        await ctx.SaveChangesAsync();
        return f;
    }

    [Fact]
    public async Task Submit_CreatesApproval_WithDefaultThreeSteps()
    {
        var (service, ctx) = Create();
        var formula = await SeedFormula(ctx, "APR-0001");

        var approval = await service.SubmitForApprovalAsync(
            formula.Id, "Review", "desc", "Standard", "High", null, "submitter");

        approval.Id.Should().BeGreaterThan(0);
        approval.ApprovalRequestCode.Should().StartWith("APR-");
        approval.Status.Should().Be("Pending");
        approval.TotalSteps.Should().Be(3);

        var pending = await service.GetPendingApprovalsAsync();
        pending.Should().ContainSingle(a => a.Id == approval.Id);
        pending.Single().ApprovalSteps.Should().HaveCount(3);
    }

    [Fact]
    public async Task Submit_UnknownFormula_Throws()
    {
        var (service, _) = Create();
        var act = () => service.SubmitForApprovalAsync(9999, "t", null, "Standard", "Low", null, "u");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ApproveAllSteps_CompletesApproval()
    {
        var (service, ctx) = Create();
        var formula = await SeedFormula(ctx, "APR-0002");
        var approval = await service.SubmitForApprovalAsync(formula.Id, "t", null, "Standard", "Medium", null, "submitter");

        var steps = (await service.GetPendingApprovalsAsync()).Single().ApprovalSteps.OrderBy(s => s.StepNumber).ToList();

        (await service.ApproveStepAsync(approval.Id, steps[0].Id, "ok1", "rd")).Should().BeTrue();
        (await service.ApproveStepAsync(approval.Id, steps[1].Id, "ok2", "ra")).Should().BeTrue();
        (await service.ApproveStepAsync(approval.Id, steps[2].Id, "ok3", "prod")).Should().BeTrue();

        var final = await ctx.Set<FormulaApproval>().FindAsync(approval.Id);
        final!.Status.Should().Be("Approved");
        final.CompletedAt.Should().NotBeNull();
        final.FinalApprover.Should().Be("prod");
    }

    [Fact]
    public async Task RejectStep_RejectsWholeApproval()
    {
        var (service, ctx) = Create();
        var formula = await SeedFormula(ctx, "APR-0003");
        var approval = await service.SubmitForApprovalAsync(formula.Id, "t", null, "Standard", "Low", null, "submitter");
        var firstStep = (await service.GetPendingApprovalsAsync()).Single().ApprovalSteps.OrderBy(s => s.StepNumber).First();

        (await service.RejectStepAsync(approval.Id, firstStep.Id, "not compliant", "ra")).Should().BeTrue();

        var final = await ctx.Set<FormulaApproval>().FindAsync(approval.Id);
        final!.Status.Should().Be("Rejected");
        final.FinalComments.Should().Be("not compliant");
    }

    [Fact]
    public async Task AddComment_PersistsComment()
    {
        var (service, ctx) = Create();
        var formula = await SeedFormula(ctx, "APR-0004");
        var approval = await service.SubmitForApprovalAsync(formula.Id, "t", null, "Standard", "Low", null, "submitter");

        var comment = await service.AddCommentAsync(approval.Id, null, "Looks good", "Comment", null, "reviewer");

        comment.Id.Should().BeGreaterThan(0);
        ctx.Set<ApprovalComment>().Should().ContainSingle(c => c.FormulaApprovalId == approval.Id);
    }

    [Fact]
    public async Task Cancel_BySubmitter_Succeeds_ByOther_Fails()
    {
        var (service, ctx) = Create();
        var formula = await SeedFormula(ctx, "APR-0005");
        var approval = await service.SubmitForApprovalAsync(formula.Id, "t", null, "Standard", "Low", null, "owner");

        (await service.CancelApprovalAsync(approval.Id, "changed mind", "someone-else")).Should().BeFalse();
        (await service.CancelApprovalAsync(approval.Id, "changed mind", "owner")).Should().BeTrue();

        var final = await ctx.Set<FormulaApproval>().FindAsync(approval.Id);
        final!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task GetApprovalStatistics_ReturnsCounts()
    {
        var (service, ctx) = Create();
        var formula = await SeedFormula(ctx, "APR-0006");
        await service.SubmitForApprovalAsync(formula.Id, "t", null, "Standard", "Low", null, "u");

        var stats = await service.GetApprovalStatisticsAsync();

        stats.Should().ContainKey("Total");
        ((int)stats["Total"]).Should().BeGreaterThanOrEqualTo(1);
        stats.Should().ContainKey("Pending");
    }
}
