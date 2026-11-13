using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Commercial operations E2E: user limits (MaxUsers), provider SetLimits, subscription status
/// and audit logging — πάνω σε πραγματικούς tenants μέσω browser.
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class CommercialOperationsTests
{
    private readonly AppServerFixture _fx;

    public CommercialOperationsTests(AppServerFixture fx) => _fx = fx;

    private const string NewPass = "Commercial#2026!Aa";
    private const string UserPass = "Member#2026!Aa";

    private async Task<IPage> LoginAsync(string email, string password)
    {
        var page = await _fx.NewPageAsync();
        await page.GotoAsync($"{_fx.BaseUrl}/Account/Login");
        await page.FillAsync("#Email", email);
        await page.FillAsync("#Password", password);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    private async Task<(string email, string temp)> CreateTenantAsync(string name, string email, int maxUsers)
    {
        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            await admin.FillAsync("input[name='name']", name);
            await admin.FillAsync("input[name='email']", email);
            await admin.FillAsync("input[name='maxUsers']", maxUsers.ToString());
            await admin.ClickAsync("button:has-text('Δημιουργία πελάτη')");
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var cred = await admin.Locator("code").First.InnerTextAsync();
            var parts = cred.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return (parts[0], parts[1]);
        }
        finally { await admin.CloseAsync(); }
    }

    private async Task<IPage> TenantAdminAsync(string email, string temp)
    {
        var page = await LoginAsync(email, temp);
        if (page.Url.Contains("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase))
        {
            await page.FillAsync("#OldPassword", temp);
            await page.FillAsync("#NewPassword", NewPass);
            await page.FillAsync("#ConfirmPassword", NewPass);
            await page.ClickAsync("button:has-text('Αποθήκευση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.CloseAsync();
            page = await LoginAsync(email, NewPass);
        }
        return page;
    }

    private async Task<(bool ok, string html)> RegisterMemberAsync(string org, string fullName, string email)
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/Account/Register");
            await page.FillAsync("#FullName", fullName);
            await page.FillAsync("#Email", email);
            await page.FillAsync("#OrganizationName", org);
            await page.FillAsync("#Password", UserPass);
            await page.FillAsync("#ConfirmPassword", UserPass);
            await page.ClickAsync("button:has-text('Δημιουργία Λογαριασμού')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            return (!page.Url.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase), await page.ContentAsync());
        }
        finally { await page.CloseAsync(); }
    }

    private INCIS_SaaP.Data.INCISDbContext Db()
    {
        var opts = new DbContextOptionsBuilder<INCIS_SaaP.Data.INCISDbContext>()
            .UseSqlServer(_fx.ConnectionString).Options;
        return new INCIS_SaaP.Data.INCISDbContext(opts, new Microsoft.AspNetCore.Http.HttpContextAccessor());
    }

    [Fact]
    public async Task MaxUsersLimit_BlocksAdditionalRegistration()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var name = "LIMIT-" + s;
        var adminEmail = $"tadmin.limit.{s}@saas.local";

        var (email, temp) = await CreateTenantAsync(name, adminEmail, maxUsers: 2);
        var adminPage = await TenantAdminAsync(email, temp);
        await adminPage.CloseAsync();

        // 2nd user (admin=1, member=2) -> allowed
        var (ok1, _) = await RegisterMemberAsync(name, "Member One", $"m1.{s}@saas.local");
        ok1.Should().BeTrue("ο 2ος χρήστης επιτρέπεται (όριο=2)");

        // 3rd user -> blocked by MaxUsers
        var (ok2, html) = await RegisterMemberAsync(name, "Member Two", $"m2.{s}@saas.local");
        ok2.Should().BeFalse("ο 3ος χρήστης πρέπει να αποκλείεται λόγω ορίου");
        html.Should().Contain("όριο χρηστών", "πρέπει να εμφανίζεται σαφές μήνυμα ορίου");
    }

    [Fact]
    public async Task SetLimits_FromProviderConsole_TakesEffect()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var name = "SETLIM-" + s;
        var adminEmail = $"tadmin.setlim.{s}@saas.local";

        var (email, temp) = await CreateTenantAsync(name, adminEmail, maxUsers: 5);
        var page = await TenantAdminAsync(email, temp);
        await page.CloseAsync();

        (await RegisterMemberAsync(name, "Member A", $"a.{s}@saas.local")).ok.Should().BeTrue();

        // Provider sets limit to 1 -> next registration must fail
        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            var row = admin.Locator($"tr:has-text('{name}')");
            await row.Locator("input[name='maxUsers']").FillAsync("1");
            await row.Locator("button:has-text('Όριο')").ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await admin.CloseAsync(); }

        var (ok, html) = await RegisterMemberAsync(name, "Member B", $"b.{s}@saas.local");
        ok.Should().BeFalse("με όριο=1 κάθε επιπλέον εγγραφή αποκλείεται");
        html.Should().Contain("όριο χρηστών");
    }

    [Fact]
    public async Task AuditLog_RecordsTenantAdminActions()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var name = "AUDIT-" + s;
        var adminEmail = $"tadmin.audit.{s}@saas.local";
        var clientName = "AUDIT-CLIENT-" + s;

        var (email, temp) = await CreateTenantAsync(name, adminEmail, maxUsers: 5);
        var page = await TenantAdminAsync(email, temp);
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/Clients/Create");
            await page.FillAsync("#Name", clientName);
            await page.ClickAsync("button:has-text('Δημιουργία')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await page.CloseAsync(); }

        await using var db = Db();
        var recent = await db.AuditLogs.OrderByDescending(l => l.Timestamp).Take(50).ToListAsync();
        var clientLogs = recent
            .Where(l => l.EntityName.Contains("Client", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(l.Action, "Created", StringComparison.OrdinalIgnoreCase))
            .ToList();

        clientLogs.Should().NotBeEmpty(
            $"η δημιουργία Client πρέπει να καταγράφεται. recent=[{string.Join("; ", recent.Select(l => $"{l.EntityName}/{l.Action}"))}]");

        clientLogs.Any(l => (l.NewValues ?? "").Contains(clientName))
            .Should().BeTrue("το audit πρέπει να περιέχει τα νέα δεδομένα του Client");

        clientLogs.Any(l => string.Equals(l.UserEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("το audit πρέπει να περιλαμβάνει τον δράστη (TenantAdmin)");
    }

    [Fact]
    public async Task Provider_CanCreatePlan_AndAssignToTenant()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var planName = "PLAN-" + s;
        var tenantName = "PLANTEN-" + s;
        var email = $"tadmin.plan.{s}@saas.local";

        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            var planForm = admin.Locator("form[action*='CreatePlan']").First;
            await planForm.Locator("input[name='name']").FillAsync(planName);
            await planForm.Locator("input[name='maxUsers']").FillAsync("8");
            await planForm.Locator("input[name='monthlyPrice']").FillAsync("49");
            await planForm.Locator("input[name='annualPrice']").FillAsync("490");
            await planForm.Locator("input[name='hasApiAccess']").CheckAsync();
            await planForm.Locator("button:has-text('Πακέτο')").ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);

            (await admin.Locator($"text={planName}").CountAsync()).Should().BeGreaterThan(0, "το πακέτο πρέπει να εμφανίζεται");

            var tenantForm = admin.Locator("form[action*='/Tenants/Create']").First;
            await tenantForm.Locator("input[name='name']").FillAsync(tenantName);
            await tenantForm.Locator("input[name='email']").FillAsync(email);
            await tenantForm.Locator("select[name='planId']").SelectOptionAsync(new SelectOptionValue { Label = planName });
            await tenantForm.Locator("button:has-text('Δημιουργία πελάτη')").ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await admin.CloseAsync(); }

        await using var db = Db();
        var tenant = await db.Tenants.Include(t => t.SubscriptionPlan).SingleAsync(t => t.Name == tenantName);
        tenant.SubscriptionPlanId.Should().NotBeNull();
        tenant.SubscriptionPlan!.Name.Should().Be(planName);
        tenant.MaxUsers.Should().Be(8, "το MaxUsers του πακέτου εφαρμόζεται");
    }

    [Fact]
    public async Task Renew_ExtendsSubscriptionExpiry()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var name = "RENEW-" + s;
        var email = $"tadmin.renew.{s}@saas.local";

        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            var tenantForm = admin.Locator("form[action*='/Tenants/Create']").First;
            await tenantForm.Locator("input[name='name']").FillAsync(name);
            await tenantForm.Locator("input[name='email']").FillAsync(email);
            await tenantForm.Locator("input[name='trialDays']").FillAsync("1");
            // activate unchecked -> trial 1 day
            await tenantForm.Locator("button:has-text('Δημιουργία πελάτη')").ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var row = admin.Locator($"tr:has-text('{name}')");
            await row.Locator("button[name='months'][value='12']").ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await admin.CloseAsync(); }

        await using var db = Db();
        var tenant = await db.Tenants.SingleAsync(t => t.Name == name);
        tenant.SubscriptionStatus.Should().Be("Active");
        tenant.SubscriptionExpiresAt.Should().NotBeNull();
        tenant.SubscriptionExpiresAt!.Value.Should().BeAfter(DateTime.UtcNow.AddMonths(11), "η ανανέωση 12 μηνών πρέπει να επεκτείνει τη λήξη");
    }
}
