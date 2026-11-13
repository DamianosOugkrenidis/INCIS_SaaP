using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace INCIS_SaaP.PlaywrightTests;

/// <summary>
/// Εκτεταμένες browser ροές: ProductManagement, FormulaDocuments και ΠΡΑΓΜΑΤΙΚΕΣ λήψεις
/// αρχείων (xlsx από Reports, pdf από FormulaDocuments) με έλεγχο signature/μεγέθους.
/// </summary>
[Collection("playwright")]
[Trait("Category", "Playwright")]
public class WebFlowsExtendedTests
{
    private readonly AppServerFixture _fx;

    public WebFlowsExtendedTests(AppServerFixture fx) => _fx = fx;

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

    [Theory]
    [InlineData("Safety")]
    [InlineData("Allergens")]
    [InlineData("Claims")]
    [InlineData("HeavyMetals")]
    [InlineData("Toxicology")]
    [InlineData("Marketing")]
    [InlineData("Price")]
    [InlineData("Natural")]
    [InlineData("Inci")]
    public async Task ProductManagement_Pages_Load(string page)
    {
        var browser = await LoginAsync();
        try
        {
            var res = await browser.GotoAsync($"{_fx.BaseUrl}/ProductManagement/{page}?formulaId={_fx.FormulaId}");
            res.Should().NotBeNull();
            res!.Status.Should().BeLessThan(500, $"το ProductManagement/{page} δεν πρέπει να ρίχνει server error");
        }
        finally { await browser.CloseAsync(); }
    }

    [Fact]
    public async Task ProductManagement_Safety_RendersContent()
    {
        var page = await LoginAsync();
        try
        {
            var res = await page.GotoAsync($"{_fx.BaseUrl}/ProductManagement/Safety?formulaId={_fx.FormulaId}");
            res!.Status.Should().BeLessThan(400);

            var html = await page.ContentAsync();
            html.Length.Should().BeGreaterThan(1000, "η σελίδα πρέπει να αποδίδει πραγματικό περιεχόμενο");
        }
        finally { await page.CloseAsync(); }
    }

    [Theory]
    [InlineData("Index")]
    [InlineData("FormulaPrice")]
    [InlineData("HeavyMetals")]
    [InlineData("InciList")]
    [InlineData("NaturalFormula")]
    [InlineData("MasterFormula")]
    public async Task FormulaDocuments_Pages_Load(string action)
    {
        var page = await LoginAsync();
        try
        {
            var res = await page.GotoAsync($"{_fx.BaseUrl}/FormulaDocuments/{action}/{_fx.FormulaId}");
            res.Should().NotBeNull();
            res!.Status.Should().BeLessThan(500, $"το FormulaDocuments/{action} δεν πρέπει να ρίχνει server error");
        }
        finally { await page.CloseAsync(); }
    }

    private static async Task<(string Name, byte[] Bytes)> DownloadAsync(IPage page, string url)
    {
        // Όταν η πλοήγηση προκαλεί download, το GotoAsync ρίχνει ERR_ABORTED — αναμενόμενο.
        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            try
            {
                await page.GotoAsync(url);
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("ERR_ABORTED"))
            {
                // Το download ξεκίνησε· αγνοούμε το abort της πλοήγησης.
            }
        });

        var path = await download.PathAsync();
        var bytes = await File.ReadAllBytesAsync(path);
        return (download.SuggestedFilename, bytes);
    }

    [Theory]
    [InlineData("/Reports/ExportClients", ".xlsx")]
    [InlineData("/Reports/ExportINCIs", ".xlsx")]
    [InlineData("/Reports/ExportFormulas", ".xlsx")]
    public async Task ReportExports_ReturnValidXlsx(string url, string ext)
    {
        var page = await LoginAsync();
        try
        {
            var (name, bytes) = await DownloadAsync(page, $"{_fx.BaseUrl}{url}");

            name.ToLowerInvariant().Should().EndWith(ext);
            bytes.Length.Should().BeGreaterThan(0);
            // xlsx = ZIP container -> "PK"
            bytes[0].Should().Be(0x50);
            bytes[1].Should().Be(0x4B);
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task FormulaDocumentPdf_ReturnsValidPdf()
    {
        var page = await LoginAsync();
        try
        {
            var (name, bytes) = await DownloadAsync(page, $"{_fx.BaseUrl}/FormulaDocuments/Pdf/{_fx.FormulaId}");

            name.ToLowerInvariant().Should().EndWith(".pdf");
            bytes.Length.Should().BeGreaterThan(0);
            System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        }
        finally { await page.CloseAsync(); }
    }
}
