using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// S-1 regression: ροή έγκρισης/απενεργοποίησης χρήστη μέσω πραγματικού browser POST.
/// (Η προηγούμενη αποτυχία ήταν synthetic-click quirk· η φόρμα είναι σωστή —
///  επιβεβαιώνεται με πραγματικό `form.submit()` που στέλνει cookies + antiforgery token.)
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class UsersApprovalTests
{
    private readonly AppServerFixture _fx;

    public UsersApprovalTests(AppServerFixture fx) => _fx = fx;

    private const string Password = "Pilot#2026!Aa";

    private async Task RegisterAsync(string fullName, string email, string org)
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/Account/Register");
            await page.FillAsync("#FullName", fullName);
            await page.FillAsync("#Email", email);
            await page.FillAsync("#OrganizationName", org);
            await page.FillAsync("#Password", Password);
            await page.FillAsync("#ConfirmPassword", Password);
            await page.ClickAsync("button:has-text('Δημιουργία Λογαριασμού')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await page.CloseAsync(); }
    }

    private async Task<IPage> LoginAsync(string email)
    {
        var page = await _fx.NewPageAsync();
        await page.GotoAsync($"{_fx.BaseUrl}/Account/Login");
        await page.FillAsync("#Email", email);
        await page.FillAsync("#Password", Password);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    private async Task<(string admin, string member)> ProvisionPendingAsync()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var org = "APPR-" + s;
        var admin = $"tadmin.{s}@appr.local";
        var member = $"member.{s}@appr.local";
        await RegisterAsync("Appr Admin", admin, org);
        await RegisterAsync("Appr Member", member, org);
        return (admin, member);
    }

    private async Task GoUsersAndSubmitAsync(IPage page, string buttonText)
    {
        await page.GotoAsync($"{_fx.BaseUrl}/Users");
        var form = page.Locator($"form:has(button:has-text('{buttonText}'))").First;
        (await form.CountAsync()).Should().BeGreaterThan(0, $"πρέπει να υπάρχει φόρμα με κουμπί '{buttonText}' στο /Users");
        var wait = page.WaitForResponseAsync(r => r.Request.Method == "POST");
        await form.EvaluateAsync("f => f.submit()");
        await wait;
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<bool> TryLoginAsync(string email)
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/Account/Login");
            await page.FillAsync("#Email", email);
            await page.FillAsync("#Password", Password);
            await page.ClickAsync("button[type=submit]");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            return !page.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase);
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ApprovalFlow_ApproveFormIsCorrect_And_MemberCanLogin()
    {
        var (admin, member) = await ProvisionPendingAsync();

        var page = await LoginAsync(admin);
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/Users");
            var form = page.Locator("form:has(button:has-text('Έγκριση'))").First;
            (await form.CountAsync()).Should().BeGreaterThan(0, "πρέπει να υπάρχει φόρμα έγκρισης για τον pending χρήστη");
            var action = await form.GetAttributeAsync("action");
            action.Should().Contain("/Users/Approve");
            (await form.Locator("input[name='__RequestVerificationToken']").CountAsync()).Should().Be(1);

            await GoUsersAndSubmitAsync(page, "Έγκριση");
        }
        finally { await page.CloseAsync(); }

        (await TryLoginAsync(member)).Should().BeTrue("μετά την έγκριση ο χρήστης πρέπει να συνδέεται");
    }

    [Fact]
    public async Task ApprovedMember_CannotAccess_UserManagement()
    {
        var (admin, member) = await ProvisionPendingAsync();

        var adminPage = await LoginAsync(admin);
        try { await GoUsersAndSubmitAsync(adminPage, "Έγκριση"); }
        finally { await adminPage.CloseAsync(); }

        var memberPage = await LoginAsync(member);
        try
        {
            var res = await memberPage.GotoAsync($"{_fx.BaseUrl}/Users");
            var status = res?.Status ?? 0;
            (status == 403 || memberPage.Url.Contains("AccessDenied", StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue($"ο απλός χρήστης δεν πρέπει να έχει πρόσβαση στη Διαχείριση Χρηστών (status={status}, url={memberPage.Url})");
        }
        finally { await memberPage.CloseAsync(); }
    }

    [Fact]
    public async Task DeactivatedMember_CannotLogin()
    {
        var (admin, member) = await ProvisionPendingAsync();

        var adminPage = await LoginAsync(admin);
        try
        {
            await GoUsersAndSubmitAsync(adminPage, "Έγκριση"); // pending → approved
            (await TryLoginAsync(member)).Should().BeTrue("μετά την έγκριση συνδέεται");

            await GoUsersAndSubmitAsync(adminPage, "Απενεργοποίηση"); // reject/disable
        }
        finally { await adminPage.CloseAsync(); }

        (await TryLoginAsync(member)).Should().BeFalse("ο απενεργοποιημένος χρήστης δεν πρέπει να συνδέεται");
    }

    // ---------------- S-1: REAL browser click (no form.submit workaround) ----------------
    private async Task RealClickAsync(IPage page, string testId, string expectedAction)
    {
        await page.GotoAsync($"{_fx.BaseUrl}/Users");
        var btn = page.Locator($"[data-testid='{testId}']").First;
        await btn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await btn.ScrollIntoViewIfNeededAsync();

        var posts = new List<string>();
        void OnRequest(object? _, IRequest req)
        {
            if (string.Equals(req.Method, "POST", StringComparison.OrdinalIgnoreCase)) posts.Add(req.Url);
        }
        page.Request += OnRequest;

        await btn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        page.Request -= OnRequest;

        var matching = posts.Where(p => p.Contains($"/Users/{expectedAction}", StringComparison.OrdinalIgnoreCase)).ToList();
        matching.Should().NotBeEmpty($"το πραγματικό click πρέπει να κάνει POST στο {expectedAction}. posts=[{string.Join(", ", posts)}]");
    }

    private INCIS_SaaP.Data.INCISDbContext Db()
    {
        var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<INCIS_SaaP.Data.INCISDbContext>()
            .UseSqlServer(_fx.ConnectionString).Options;
        return new INCIS_SaaP.Data.INCISDbContext(opts, new Microsoft.AspNetCore.Http.HttpContextAccessor());
    }

    [Fact]
    public async Task RealClick_Approve_PostsAndMemberCanLogin()
    {
        var (admin, member) = await ProvisionPendingAsync();

        var adminPage = await LoginAsync(admin);
        try { await RealClickAsync(adminPage, "approve-user", "Approve"); }
        finally { await adminPage.CloseAsync(); }

        await using (var db = Db())
        {
            var u = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .SingleAsync(db.Users, x => x.Email == member);
            u.IsApproved.Should().BeTrue("το Approve πρέπει να αποθηκεύσει IsApproved=true");
            u.IsActive.Should().BeTrue("το Approve πρέπει να αποθηκεύσει IsActive=true");
        }

        (await TryLoginAsync(member)).Should().BeTrue("το πραγματικό click έγκρισης πρέπει να ενεργοποιεί τον χρήστη");
    }

    [Fact]
    public async Task RealClick_Deactivate_BlocksLogin()
    {
        var (admin, member) = await ProvisionPendingAsync();

        var adminPage = await LoginAsync(admin);
        try
        {
            await RealClickAsync(adminPage, "approve-user", "Approve");
            (await TryLoginAsync(member)).Should().BeTrue();

            await RealClickAsync(adminPage, "toggle-user", "ToggleActive");
        }
        finally { await adminPage.CloseAsync(); }

        (await TryLoginAsync(member)).Should().BeFalse("μετά το πραγματικό click απενεργοποίησης ο χρήστης δεν συνδέεται");
    }

    [Fact]
    public async Task PendingUser_CannotLogin_UntilApproved()
    {
        var (_, member) = await ProvisionPendingAsync();
        (await TryLoginAsync(member)).Should().BeFalse("ο pending χρήστης δεν πρέπει να συνδέεται πριν την έγκριση");
    }

    [Fact]
    public async Task TenantAdmin_CannotSee_OtherTenantPendingUsers()
    {
        var s1 = Guid.NewGuid().ToString("N")[..8];
        var s2 = Guid.NewGuid().ToString("N")[..8];
        var adminA = $"tadmin.a{s1}@appr.local";
        var memberA = $"member.a{s1}@appr.local";
        var adminB = $"tadmin.b{s2}@appr.local";
        var memberB = $"member.b{s2}@appr.local";

        await RegisterAsync("Admin A", adminA, "ORG-A-" + s1);
        await RegisterAsync("Member A", memberA, "ORG-A-" + s1);
        await RegisterAsync("Admin B", adminB, "ORG-B-" + s2);
        await RegisterAsync("Member B", memberB, "ORG-B-" + s2);

        var page = await LoginAsync(adminA);
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/Users");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var html = await page.ContentAsync();

            html.Should().NotContain(memberB, "ο TenantAdmin A δεν πρέπει να βλέπει pending χρήστες άλλου tenant");
            html.Should().Contain(memberA, "ο TenantAdmin A πρέπει να βλέπει τους δικούς του χρήστες");
        }
        finally { await page.CloseAsync(); }
    }
}
