using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Σενάριο ταυτόχρονης χρήσης από πολλούς χρήστες (tenant admins) σε πραγματικό Kestrel.
/// Η ροή έγκρισης χρηστών (approve) καλύπτεται in-process (TenantApprovalTests) και η
/// εξουσιοδότηση/ρόλοι στο AccessPolicyTests/EndpointAuthorizationTests.
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class ThreeUserPilotTests
{
    private readonly AppServerFixture _fx;

    public ThreeUserPilotTests(AppServerFixture fx) => _fx = fx;

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

    private async Task<IPage> LoginPageAsync(string email, string password = Password)
    {
        var page = await _fx.NewPageAsync();
        await page.GotoAsync($"{_fx.BaseUrl}/Account/Login");
        await page.FillAsync("#Email", email);
        await page.FillAsync("#Password", password);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    [Fact]
    public async Task ThreeTenantAdmins_ConcurrentlyCreateInci_AllSucceed()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var admins = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var org = $"CONC-{i}-{s}";
            var em = $"conc{i}.{s}@pilot.local";
            await RegisterAsync($"Concurrent {i}", em, org);
            admins.Add(em);
        }

        var pages = new List<IPage>();
        foreach (var a in admins) pages.Add(await LoginPageAsync(a));
        try
        {
            var n1 = "CONC A " + s;
            var n2 = "CONC B " + s;
            var n3 = "CONC C " + s;

            async Task CreateAsync(IPage page, string name)
            {
                await page.GotoAsync($"{_fx.BaseUrl}/INCIs/Create");
                await page.FillAsync("#Name", name);
                await page.FillAsync("#CASNumber", "1234-56-7");
                await page.ClickAsync("button:has-text('Δημιουργία')");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            await Task.WhenAll(CreateAsync(pages[0], n1), CreateAsync(pages[1], n2), CreateAsync(pages[2], n3));

            foreach (var (page, name) in new[] { (pages[0], n1), (pages[1], n2), (pages[2], n3) })
            {
                await page.GotoAsync($"{_fx.BaseUrl}/INCIs");
                await page.FillAsync("input[name='searchString']", name);
                await page.ClickAsync("button:has-text('Αναζήτηση')");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                (await page.Locator($"text={name}").CountAsync())
                    .Should().BeGreaterThan(0, $"το {name} πρέπει να αποθηκεύτηκε χωρίς σφάλμα concurrency");
            }
        }
        finally
        {
            foreach (var p in pages) await p.CloseAsync();
        }
    }
}
