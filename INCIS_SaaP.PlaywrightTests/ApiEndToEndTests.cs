using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Πραγματικά HTTP API tests εναντίον του running Kestrel (ίδιο cookie session με browser):
/// Documents upload/download/list/delete, Subscription status (current tenant), Webhooks GET.
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class ApiEndToEndTests
{
    private readonly AppServerFixture _fx;

    public ApiEndToEndTests(AppServerFixture fx) => _fx = fx;

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

    private async Task<IPage> CreateTenantAdminPageAsync(string tenantName, string adminEmail)
    {
        var admin = await LoginAsync(_fx.AdminEmail, _fx.AdminPassword);
        string email, temp;
        try
        {
            await admin.GotoAsync($"{_fx.BaseUrl}/Tenants");
            await admin.FillAsync("input[name='name']", tenantName);
            await admin.FillAsync("input[name='email']", adminEmail);
            await admin.ClickAsync("button:has-text('Δημιουργία πελάτη')");
            await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var cred = await admin.Locator("code").First.InnerTextAsync();
            var parts = cred.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            email = parts[0]; temp = parts[1];
        }
        finally { await admin.CloseAsync(); }

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

    [Fact]
    public async Task Documents_Upload_Download_List_Delete_OverRealHttp()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var page = await CreateTenantAdminPageAsync("API-DOC-" + s, $"tadmin.apidoc.{s}@saas.local");
        try
        {
            var request = page.APIRequest;
            var pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\nE2E document content\n%%EOF");

            var form = request.CreateFormData();
            form.Set("documentType", "TDS");
            form.Set("entityType", "RawMaterial");
            form.Set("entityId", "1");
            form.Set("description", "E2E");
            form.Set("file", new FilePayload { Name = "e2e.pdf", MimeType = "application/pdf", Buffer = pdf });

            var upload = await request.PostAsync($"{_fx.BaseUrl}/api/v1/documents/upload", new APIRequestContextOptions
            {
                Multipart = form
            });
            upload.Ok.Should().BeTrue($"upload should succeed (status {(int)upload.Status})");

            using var doc = JsonDocument.Parse(await upload.TextAsync());
            var id = doc.RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var download = await request.GetAsync($"{_fx.BaseUrl}/api/v1/documents/{id}/download");
            download.Ok.Should().BeTrue();
            var bytes = await download.BodyAsync();
            bytes.Should().Equal(pdf, "το downloaded αρχείο πρέπει να είναι ίδιο με το uploaded");

            var list = await request.GetAsync($"{_fx.BaseUrl}/api/v1/documents");
            list.Ok.Should().BeTrue();
            (await list.TextAsync()).Should().Contain("e2e.pdf");

            var del = await request.DeleteAsync($"{_fx.BaseUrl}/api/v1/documents/{id}");
            del.Ok.Should().BeTrue();

            var after = await request.GetAsync($"{_fx.BaseUrl}/api/v1/documents/{id}/download");
            after.Status.Should().Be(404, "μετά τη διαγραφή το document δεν πρέπει να είναι προσβάσιμο");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task SubscriptionStatus_ReturnsCurrentTenant()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var tenantName = "API-SUB-" + s;
        var page = await CreateTenantAdminPageAsync(tenantName, $"tadmin.apisub.{s}@saas.local");
        try
        {
            var res = await page.APIRequest.GetAsync($"{_fx.BaseUrl}/api/v1/subscription/status");
            res.Ok.Should().BeTrue();
            (await res.TextAsync()).Should().Contain(tenantName, "πρέπει να επιστρέφεται ο τρέχων tenant");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Webhooks_GetAll_WorksForTenantAdmin()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var page = await CreateTenantAdminPageAsync("API-WH-" + s, $"tadmin.apiwh.{s}@saas.local");
        try
        {
            var res = await page.APIRequest.GetAsync($"{_fx.BaseUrl}/api/v1/webhooks");
            res.Ok.Should().BeTrue("ο TenantAdmin πρέπει να βλέπει τα webhooks του tenant του");
        }
        finally { await page.CloseAsync(); }
    }
}
