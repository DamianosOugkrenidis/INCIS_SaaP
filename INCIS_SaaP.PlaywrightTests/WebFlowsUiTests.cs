using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Πρόσθετα UI/UX scenarios (M-1 μετριασμός): dashboard/navigation, content assertions σε
/// Formulas, validation/error handling και responsive συμπεριφορά. Πραγματικός Kestrel +
/// απομονωμένη βάση (κοινό AppServerFixture).
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class WebFlowsUiTests
{
    private readonly AppServerFixture _fx;

    public WebFlowsUiTests(AppServerFixture fx) => _fx = fx;

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
    public async Task Dashboard_Navigation_ShowsMainSections_And_FormulasHeading()
    {
        var page = await LoginAsync();
        try
        {
            // Το authenticated navigation πρέπει να περιέχει σύνδεσμο προς Φόρμουλες.
            (await page.Locator("a[href*='Formulas']").CountAsync())
                .Should().BeGreaterThan(0, "το nav πρέπει να έχει σύνδεσμο Φόρμουλες για Admin");

            await page.ClickAsync("a[href*='Formulas']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Formulas");
            (await page.Locator("h1:has-text('Φόρμουλες')").CountAsync())
                .Should().BeGreaterThan(0, "η σελίδα Φόρμουλες πρέπει να εμφανίζει τον τίτλο h1");
            (await page.Locator("#formulasTable").CountAsync())
                .Should().BeGreaterThan(0, "ο πίνακας φορμουλών πρέπει να υπάρχει");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Formulas_List_ShowsSeededFormula_ContentAssertion()
    {
        var page = await LoginAsync();
        try
        {
            var res = await page.GotoAsync($"{_fx.BaseUrl}/Formulas");
            res.Should().NotBeNull();
            res!.Status.Should().BeLessThan(500);

            (await page.Locator("h1:has-text('Φόρμουλες')").CountAsync()).Should().Be(1);
            (await page.Locator("text=PW Cream").CountAsync())
                .Should().BeGreaterThan(0, "η seeded φόρμουλα πρέπει να εμφανίζεται στη λίστα");
            (await page.Locator("text=PW-0001-T01").CountAsync())
                .Should().BeGreaterThan(0, "ο κωδικός φόρμουλας πρέπει να εμφανίζεται");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Inci_Create_EmptyName_IsBlocked_ByValidation()
    {
        var page = await LoginAsync();
        try
        {
            await page.GotoAsync($"{_fx.BaseUrl}/INCIs/Create");
            // Υποβολή χωρίς συμπλήρωση υποχρεωτικού πεδίου (Name required).
            await page.ClickAsync("button:has-text('Δημιουργία')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/INCIs/Create", "η άκυρη υποβολή δεν πρέπει να προχωρήσει");
            (await page.Locator("h1:has-text('Νέο συστατικό INCI')").CountAsync())
                .Should().BeGreaterThan(0, "παραμένουμε στη φόρμα δημιουργίας");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task Dashboard_MobileViewport_NoHorizontalOverflow()
    {
        var page = await _fx.NewPageAsync();
        try
        {
            await page.SetViewportSizeAsync(390, 844); // iPhone 12-class
            await page.GotoAsync($"{_fx.BaseUrl}/Account/Login");
            await page.FillAsync("#Email", _fx.AdminEmail);
            await page.FillAsync("#Password", _fx.AdminPassword);
            await page.ClickAsync("button[type=submit]");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var overflow = await page.EvaluateAsync<int>(
                "Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");
            overflow.Should().BeLessThanOrEqualTo(4, "σε mobile viewport δεν πρέπει να υπάρχει οριζόντιο overflow");
        }
        finally { await page.CloseAsync(); }
    }
}
