using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Final operational check against a RUNNING INCIS server (real browser).
/// - Rotates admin + tenant passwords (old -> new) without disabling security.
/// - Verifies Provider Console lists all tenants, per-tenant data writes,
///   tenant isolation (view + no cross-tenant access), and role scoping.
/// Required env:
///   INCIS_LIVE_URL, INCIS_ADMIN_EMAIL, INCIS_ADMIN_PASSWORD, INCIS_ADMIN_NEW,
///   INCIS_TENANT_OLD, INCIS_NEW_AURORA, INCIS_NEW_VIOLETA, INCIS_NEW_HELIOS, INCIS_NEW_NIKEA
/// Optional: INCIS_FOREIGN_CLIENT_ID, INCIS_FOREIGN_OWNER
/// </summary>
public class LiveLocalServerTests
{
    private readonly ITestOutputHelper _o;
    public LiveLocalServerTests(ITestOutputHelper o) => _o = o;

    private static readonly (string Name, string Email, string NewEnv)[] TenantDefs =
    {
        ("Aurora Cosmetics", "aurora.admin@incis.local", "INCIS_NEW_AURORA"),
        ("Violeta Labs",     "violeta.admin@incis.local", "INCIS_NEW_VIOLETA"),
        ("Helios Skincare",  "helios.admin@incis.local", "INCIS_NEW_HELIOS"),
        ("Nikea Naturals",   "nikea.admin@incis.local", "INCIS_NEW_NIKEA"),
    };

    private static string Env(string k) => Environment.GetEnvironmentVariable(k) ?? "";

    [Fact]
    public async Task Live_FinalOperationalCheck()
    {
        var baseUrl = Env("INCIS_LIVE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl)) { _o.WriteLine("SKIP: INCIS_LIVE_URL not set"); return; }

        var adminEmail = Env("INCIS_ADMIN_EMAIL");
        var adminOld = Env("INCIS_ADMIN_PASSWORD");
        var adminNew = Env("INCIS_ADMIN_NEW");
        var tenantOld = Env("INCIS_TENANT_OLD");
        var foreignId = Env("INCIS_FOREIGN_CLIENT_ID");
        var foreignOwner = Env("INCIS_FOREIGN_OWNER");
        var results = new List<string>();

        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var ctxOpts = new BrowserNewContextOptions { IgnoreHTTPSErrors = true, BaseURL = baseUrl };

        // ── Admin: login + rotate password ──
        var adminCtx = await browser.NewContextAsync(ctxOpts);
        var admin = await adminCtx.NewPageAsync();
        var used = await LoginAny(admin, baseUrl, adminEmail, adminOld, adminNew);
        Assert.NotNull(used);
        if (used == adminOld) { await ChangePasswordAsync(admin, baseUrl, adminOld, adminNew); results.Add($"ADMIN_PASSWORD_ROTATED email={adminEmail}"); }
        else { results.Add($"ADMIN_PASSWORD_ALREADY_ROTATED email={adminEmail}"); }
        Assert.Contains("/Home", admin.Url);

        await admin.GotoAsync($"{baseUrl}/Tenants");
        await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var tenantsHtml = await admin.ContentAsync();
        foreach (var t in TenantDefs)
        {
            Assert.Contains(t.Name, tenantsHtml);
        }
        results.Add("PROVIDER_CONSOLE_OK all_4_tenants_visible");
        await admin.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\Users\daoug\INCIS\admin-console.png" });

        // ── Per tenant: rotate password, write data, isolation & permissions ──
        foreach (var t in TenantDefs)
        {
            var newPwd = Env(t.NewEnv);
            Assert.False(string.IsNullOrWhiteSpace(newPwd), $"Missing new password env {t.NewEnv}");

            var ctx = await browser.NewContextAsync(ctxOpts);
            var page = await ctx.NewPageAsync();

            var usedTenant = await LoginAny(page, baseUrl, t.Email, tenantOld, newPwd);
            Assert.NotNull(usedTenant);
            if (usedTenant == tenantOld) { await ChangePasswordAsync(page, baseUrl, tenantOld, newPwd); results.Add($"TENANT_PASSWORD_ROTATED email={t.Email}"); }
            else { results.Add($"TENANT_PASSWORD_ALREADY_ROTATED email={t.Email}"); }
            Assert.Contains("/Home", page.Url);

            // write data (2nd client)
            var cname = t.Name + " Client 2";
            await page.GotoAsync($"{baseUrl}/Clients/Create");
            await page.FillAsync("input[name='Name']", cname);
            await page.FillAsync("input[name='Code']", "C2-" + t.Name.Split(' ')[0].ToUpperInvariant());
            await page.Locator("form:has(input[name='Code']) button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.Contains("/Clients", page.Url);

            var own = await page.ContentAsync();
            Assert.Contains(cname, own);
            Assert.Contains(t.Name + " Client", own); // original client (tenant-scoped)
            foreach (var other in TenantDefs.Where(x => x.Name != t.Name))
            {
                Assert.DoesNotContain(other.Name + " Client", own);
            }
            results.Add($"TENANT_DATA_WRITE_OK name='{t.Name}' + isolation_view_ok");

            // cross-tenant: must NOT view another tenant's client (404), unless it's the owner
            if (!string.IsNullOrEmpty(foreignId) && t.Name != foreignOwner)
            {
                var r1 = await page.GotoAsync($"{baseUrl}/Clients/Details/{foreignId}");
                Assert.Equal(404, r1!.Status);
                var r2 = await page.GotoAsync($"{baseUrl}/Clients/Edit/{foreignId}");
                Assert.Equal(404, r2!.Status);
                results.Add($"CROSS_TENANT_DENIED name='{t.Name}' foreignClientId={foreignId} -> 404 (details+edit)");
            }

            // TenantAdmin cannot open provider console
            await page.GotoAsync($"{baseUrl}/Tenants");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var denied = page.Url.Contains("AccessDenied") || (await page.ContentAsync()).Contains("AccessDenied");
            Assert.True(denied, "TenantAdmin must be denied /Tenants");
            results.Add($"PROVIDER_CONSOLE_DENIED name='{t.Name}'");

            // /Users scoped to own tenant
            await page.GotoAsync($"{baseUrl}/Users");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var uHtml = await page.ContentAsync();
            foreach (var other in TenantDefs.Where(x => x.Email != t.Email)) { Assert.DoesNotContain(other.Email, uHtml); }
            results.Add($"USERS_SCOPED_OK name='{t.Name}'");

            await page.ScreenshotAsync(new PageScreenshotOptions { Path = $@"C:\Users\daoug\INCIS\tenant-{t.Name.Split(' ')[0].ToLowerInvariant()}.png" });
            await ctx.CloseAsync();
        }

        await File.WriteAllLinesAsync(@"C:\Users\daoug\INCIS\live-final-results.txt", results);
        foreach (var r in results) _o.WriteLine(r);
        await adminCtx.CloseAsync();
    }

    private static async Task<string?> LoginAny(IPage page, string baseUrl, string email, string first, string second)
    {
        foreach (var p in new[] { first, second })
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            await page.GotoAsync($"{baseUrl}/Account/Login");
            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='Password']", p);
            await page.Locator("form:has(input[name='Password']) button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            if (!page.Url.Contains("/Account/Login")) return p;
        }
        return null;
    }

    private static async Task ChangePasswordAsync(IPage page, string baseUrl, string oldPwd, string newPwd)
    {
        await page.GotoAsync($"{baseUrl}/Account/ChangePassword");
        await page.FillAsync("input[name='OldPassword']", oldPwd);
        await page.FillAsync("input[name='NewPassword']", newPwd);
        await page.FillAsync("input[name='ConfirmPassword']", newPwd);
        await page.Locator("form:has(input[name='NewPassword']) button[type='submit']").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
