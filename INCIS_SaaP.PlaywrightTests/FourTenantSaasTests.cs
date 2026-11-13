using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Πλήρες σενάριο 4 ανεξάρτητων tenants (εμπορικό SaaS): ο Provider Admin δημιουργεί
/// 4 πελάτες + TenantAdmin ο καθένας, κάθε tenant παράγει δικά του δεδομένα, επαληθεύεται
/// πλήρης απομόνωση (κανείς δεν βλέπει δεδομένα άλλου) και ότι η αναστολή ενός tenant
/// μπλοκάρει τη σύνδεση των χρηστών του.
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class FourTenantSaasTests
{
    private readonly AppServerFixture _fx;

    public FourTenantSaasTests(AppServerFixture fx) => _fx = fx;

    private const string NewPass = "Commercial#2026!Aa";

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

    private async Task<(string email, string temp)> CreateTenantAsync(string tenantName, string adminEmail)
    {
        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            await admin.FillAsync("input[name='name']", tenantName);
            await admin.FillAsync("input[name='email']", adminEmail);
            await admin.ClickAsync("button:has-text('Δημιουργία πελάτη')");
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var cred = await admin.Locator("code").First.InnerTextAsync();
            var parts = cred.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            parts.Should().HaveCountGreaterThanOrEqualTo(2, "πρέπει να εμφανιστούν τα διαπιστευτήρια του TenantAdmin");
            return (parts[0], parts[1]);
        }
        finally { await admin.CloseAsync(); }
    }

    private async Task<IPage> LoginTenantAdmin_AndSetPasswordAsync(string email, string temp)
    {
        var page = await LoginAsync(email, temp);
        if (page.Url.Contains("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase))
        {
            await page.FillAsync("#OldPassword", temp);
            await page.FillAsync("#NewPassword", NewPass);
            await page.FillAsync("#ConfirmPassword", NewPass);
            await page.ClickAsync("button:has-text('Αποθήκευση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Η αλλαγή κωδικού αλλάζει το security stamp -> το τρέχον cookie ακυρώνεται.
            // Απαιτείται νέα σύνδεση με τον νέο κωδικό.
            await page.CloseAsync();
            page = await LoginAsync(email, NewPass);
        }
        return page;
    }

    private async Task CreateClientAsync(IPage page, string name)
    {
        await page.GotoAsync($"{_fx.BaseUrl}/Clients/Create");
        await page.FillAsync("#Name", name);
        await page.ClickAsync("button:has-text('Δημιουργία')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Fact]
    public async Task FourIndependentTenants_AreIsolated_AndSuspensionBlocksLogin()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var names = Enumerable.Range(1, 4).Select(i => $"T{i}-TENANT-{s}").ToArray();
        var emails = Enumerable.Range(1, 4).Select(i => $"tadmin{i}.{s}@saas.local").ToArray();
        var clients = Enumerable.Range(1, 4).Select(i => $"T{i}-CLIENT-{s}").ToArray();

        // 1) Δημιουργία 4 tenants μέσω Provider Admin console
        var creds = new List<(string email, string temp)>();
        for (int i = 0; i < 4; i++)
            creds.Add(await CreateTenantAsync(names[i], emails[i]));

        // 2) Κάθε TenantAdmin συνδέεται, αλλάζει κωδικό, δημιουργεί δικό του Client
        var pages = new List<IPage>();
        for (int i = 0; i < 4; i++)
        {
            var p = await LoginTenantAdmin_AndSetPasswordAsync(creds[i].email, creds[i].temp);
            pages.Add(p);
            await CreateClientAsync(p, clients[i]);
        }

        try
        {
            // 3) Πλήρης απομόνωση: ο tenant i βλέπει ΜΟΝΟ το δικό του client
            for (int i = 0; i < 4; i++)
            {
                await pages[i].GotoAsync($"{_fx.BaseUrl}/Clients");
                (await pages[i].Locator($"text={clients[i]}").CountAsync())
                    .Should().BeGreaterThan(0, $"ο tenant {i + 1} πρέπει να βλέπει το client του");

                for (int j = 0; j < 4; j++)
                {
                    if (i == j) continue;
                    (await pages[i].Locator($"text={clients[j]}").CountAsync())
                        .Should().Be(0, $"ο tenant {i + 1} ΔΕΝ πρέπει να βλέπει δεδομένα του tenant {j + 1}");
                }
            }
        }
        finally
        {
            foreach (var p in pages) await p.CloseAsync();
        }

        // 4) Αναστολή του tenant 4 από τον Provider Admin → μπλοκάρεται η σύνδεση
        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            var row = admin.Locator($"tr:has-text('{names[3]}')");
            await row.Locator("button:has-text('Αναστολή')").ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await admin.CloseAsync(); }

        var blocked = await LoginAsync(creds[3].email, NewPass);
        try
        {
            blocked.Url.Should().Contain("/Account/Login",
                "η αναστολή του tenant πρέπει να εμποδίζει τη σύνδεση των χρηστών του");
        }
        finally { await blocked.CloseAsync(); }

        // Οι υπόλοιποι tenants συνεχίζουν να συνδέονται κανονικά
        var stillOk = await LoginAsync(creds[0].email, NewPass);
        try
        {
            stillOk.Url.Should().NotContain("/Account/Login", "οι ενεργοί tenants δεν επηρεάζονται");
        }
        finally { await stillOk.CloseAsync(); }
    }

    private async Task<bool> CanLoginAsync(string email, string password)
    {
        var pg = await LoginAsync(email, password);
        var ok = !pg.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase);
        await pg.CloseAsync();
        return ok;
    }

    [Fact]
    public async Task Suspension_Then_Reactivation_RestoresLogin()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var name = "REACT-" + s;
        var email = $"tadmin.react.{s}@saas.local";

        var (e, temp) = await CreateTenantAsync(name, email);
        var p = await LoginTenantAdmin_AndSetPasswordAsync(e, temp);
        await p.CloseAsync();

        (await CanLoginAsync(e, NewPass)).Should().BeTrue("baseline: ενεργός tenant μπορεί να συνδεθεί");

        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            await admin.Locator($"tr:has-text('{name}')").Locator("button:has-text('Αναστολή')").First.ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await admin.CloseAsync(); }

        (await CanLoginAsync(e, NewPass)).Should().BeFalse("μετά την αναστολή η σύνδεση μπλοκάρεται");

        admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            await admin.Locator($"tr:has-text('{name}')").Locator("button:has-text('Ενεργοποίηση')").First.ClickAsync();
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally { await admin.CloseAsync(); }

        (await CanLoginAsync(e, NewPass)).Should().BeTrue("μετά την επανενεργοποίηση η σύνδεση αποκαθίσταται");
    }
}
