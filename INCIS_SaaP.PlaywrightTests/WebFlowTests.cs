using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Functional (browser) tests με Playwright σε πραγματικό Kestrel + απομονωμένη βάση.
/// Ελέγχουν πραγματικές ροές: login (επιτυχία/αποτυχία), πλοήγηση, δημιουργία INCI,
/// αναζήτηση και empty-state.
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class WebFlowTests
{
    private readonly AppServerFixture _fx;

    public WebFlowTests(AppServerFixture fx) => _fx = fx;

    private async Task<IPage> LoginAsync()
    {
        var page = await _fx.NewPageAsync();
        await page.GotoAsync($"{_fx.BaseUrl}/Account/Login");
        await page.FillAsync("#Email", _fx.AdminEmail);
        await page.FillAsync("#Password", _fx.AdminPassword);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    [Fact]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        var page = await LoginAsync();
        try
        {
            page.Url.Should().NotContain("/Account/Login", "η έγκυρη σύνδεση πρέπει να ανακατευθύνει");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShowsError()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/Account/Login");
            await page.FillAsync("#Email", _fx.AdminEmail);
            await page.FillAsync("#Password", "WrongPassword!123");
            await page.ClickAsync("button[type=submit]");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Account/Login", "η αποτυχημένη σύνδεση παραμένει στη σελίδα");
            (await page.Locator(".alert-danger").First.IsVisibleAsync())
                .Should().BeTrue("πρέπει να εμφανίζεται μήνυμα σφάλματος");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Inci_Create_Then_AppearsInList_And_IsSearchable()
    {
        var page = await LoginAsync();
        try
        {
            var name = "PW TEST INCI " + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

            await page.GotoAsync($"{_fx.BaseUrl}/INCIs");
            await page.ClickAsync("a:has-text('Νέο INCI')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("#Name", name);
            await page.FillAsync("#CASNumber", "1234-56-7");
            await page.FillAsync("#Function", "SOLVENT");
            await page.ClickAsync("button:has-text('Δημιουργία')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Επιτυχία: ανακατεύθυνση στη λίστα + εμφάνιση της εγγραφής
            page.Url.Should().Contain("/INCIs");
            (await page.Locator($"text={name}").CountAsync()).Should().BeGreaterThan(0, "η νέα εγγραφή πρέπει να εμφανίζεται");

            // Αναζήτηση: φιλτράρει στο συγκεκριμένο στοιχείο
            await page.FillAsync("input[name='searchString']", name);
            await page.ClickAsync("button:has-text('Αναζήτηση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            (await page.Locator("table tbody tr").CountAsync()).Should().Be(1, "η αναζήτηση πρέπει να επιστρέψει 1 εγγραφή");
            (await page.Locator($"text={name}").CountAsync()).Should().BeGreaterThan(0);
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Inci_Search_NoResults_ShowsEmptyState()
    {
        var page = await LoginAsync();
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/INCIs");
            await page.FillAsync("input[name='searchString']", "ZZZ-NO-SUCH-INCI-ZZZ");
            await page.ClickAsync("button:has-text('Αναζήτηση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            (await page.Locator("text=Δεν βρέθηκαν συστατικά INCI").CountAsync())
                .Should().BeGreaterThan(0, "πρέπει να εμφανίζεται κατάσταση κενού αποτελέσματος");
        }
        finally { await page.CloseAsync(); }
    }

    [Theory]
    [InlineData("/Clients")]
    [InlineData("/Suppliers")]
    [InlineData("/RawMaterials")]
    [InlineData("/Formulas")]
    [InlineData("/Reports")]
    public async Task AuthenticatedPages_Load_WithoutError(string url)
    {
        var page = await LoginAsync();
        try
        {
            var res = await page.GotoAsync($"{_fx.BaseUrl}{url}");
            res.Should().NotBeNull();
            res!.Status.Should().BeLessThan(500, $"το {url} δεν πρέπει να ρίχνει server error");
        }
        finally { await page.CloseAsync(); }
    }

    private async Task<string> CreateInciViaUiAsync(IPage page, string name)
    {
        await page.GotoAsync($"{_fx.BaseUrl}/INCIs");
        await page.ClickAsync("a:has-text('Νέο INCI')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.FillAsync("#Name", name);
        await page.FillAsync("#CASNumber", "9999-99-9");
        await page.ClickAsync("button:has-text('Δημιουργία')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return name;
    }

    [Fact]
    public async Task Inci_Edit_Flow_UpdatesRecord()
    {
        var page = await LoginAsync();
        try
        {
            var name = "PW EDIT " + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            await CreateInciViaUiAsync(page, name);

            // Άνοιγμα Edit από τη γραμμή της εγγραφής
            await page.FillAsync("input[name='searchString']", name);
            await page.ClickAsync("button:has-text('Αναζήτηση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.ClickAsync("a[href*='/INCIs/Edit/']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var newName = name + " REN";
            await page.FillAsync("#Name", newName);
            await page.ClickAsync("button:has-text('Αποθήκευση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            (await page.Locator($"text={newName}").CountAsync()).Should().BeGreaterThan(0, "το νέο όνομα πρέπει να εμφανίζεται μετά την επεξεργασία");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Inci_Delete_Flow_RemovesRecord()
    {
        var page = await LoginAsync();
        try
        {
            page.Dialog += async (_, d) => await d.AcceptAsync(); // data-confirm

            var name = "PW DEL " + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            await CreateInciViaUiAsync(page, name);

            await page.FillAsync("input[name='searchString']", name);
            await page.ClickAsync("button:has-text('Αναζήτηση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.ClickAsync("a[href*='/INCIs/Delete/']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.ClickAsync("button:has-text('Διαγραφή')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Αναζήτηση ξανά → empty state
            await page.FillAsync("input[name='searchString']", name);
            await page.ClickAsync("button:has-text('Αναζήτηση')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            (await page.Locator("text=Δεν βρέθηκαν συστατικά INCI").CountAsync())
                .Should().BeGreaterThan(0, "μετά τη διαγραφή η αναζήτηση δεν πρέπει να επιστρέφει την εγγραφή");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Anonymous_ProtectedPage_RedirectsToLogin()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/INCIs");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("Login", "χωρίς σύνδεση η προστατευμένη σελίδα πρέπει να ανακατευθύνει στο login");
        }
        finally { await page.CloseAsync(); }
    }
}
