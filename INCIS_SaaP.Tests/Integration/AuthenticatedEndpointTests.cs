using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Integration;

/// <summary>
/// Authenticated integration tests — εκτελούν το σώμα των [Authorize] controllers
/// (MVC + API) μέσω του TestAuthHandler, για πραγματική κάλυψη controllers/services/views.
/// </summary>
[Trait("Category", "Integration")]
public class AuthenticatedEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthenticatedEndpointTests(CustomWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AuthWorks_ProtectedListPage_Returns200NotRedirect()
    {
        var res = await _client.GetAsync("/Clients");
        ((int)res.StatusCode).Should().Be(200, "ο TestAuthHandler πρέπει να αυθεντικοποιεί το αίτημα");
    }

    // ---------------- MVC GET pages ----------------
    [Theory]
    [InlineData("/Home/Index")]
    [InlineData("/Clients")]
    [InlineData("/Clients/Create")]
    [InlineData("/Clients/Details/1")]
    [InlineData("/Clients/Edit/1")]
    [InlineData("/Suppliers")]
    [InlineData("/Suppliers/Create")]
    [InlineData("/Suppliers/Details/1")]
    [InlineData("/INCIs")]
    [InlineData("/INCIs/Create")]
    [InlineData("/INCIs/Details/1")]
    [InlineData("/RawMaterials")]
    [InlineData("/RawMaterials/Create")]
    [InlineData("/RawMaterials/Details/1")]
    [InlineData("/Formulas")]
    [InlineData("/Formulas/Create")]
    [InlineData("/Formulas/Details/1")]
    [InlineData("/Restrictions")]
    [InlineData("/Restrictions/Create")]
    [InlineData("/Reports")]
    [InlineData("/Tools")]
    [InlineData("/Help")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/Account/ForgotPassword")]
    [InlineData("/Account/AccessDenied")]
    [InlineData("/Account/ChangePassword")]
    [InlineData("/Account/LoginTwoFactor")]
    [InlineData("/Account/ResetPassword?email=a@b.gr&token=x")]
    [InlineData("/Import")]
    [InlineData("/Import/Template?type=Clients")]
    [InlineData("/Import/Template?type=Suppliers")]
    [InlineData("/Import/Template?type=INCIs")]
    [InlineData("/Import/Template?type=RawMaterials")]
    [InlineData("/Import/Template?type=Formulas")]
    [InlineData("/ProductManagement/Safety?formulaId=1")]
    [InlineData("/ProductManagement/AllergenCreate?formulaId=1")]
    [InlineData("/ProductManagement/HeavyMetalCreate?formulaId=1")]
    [InlineData("/ProductManagement/ClaimCreate?formulaId=1")]
    [InlineData("/FormulaDocuments/Index/1")]
    [InlineData("/FormulaDocuments/FormulaPrice/1")]
    [InlineData("/FormulaDocuments/MasterFormula/1")]
    [InlineData("/FormulaDocuments/InciList/1")]
    [InlineData("/FormulaDocuments/NaturalFormula/1")]
    [InlineData("/FormulaDocuments/Allergens/1")]
    [InlineData("/FormulaDocuments/HeavyMetals/1")]
    [InlineData("/FormulaDocuments/QuantitativeInci/1")]
    [InlineData("/FormulaDocuments/MarketingInfo/1")]
    [InlineData("/FormulaDocuments/RegulatedIngredients/1")]
    [InlineData("/FormulaDocuments/FormulaClaims/1")]
    [InlineData("/FormulaDocuments/ToxicologicalProfile/1")]
    [InlineData("/FormulaDocuments/QualitativeQuantitative/1")]
    public async Task MvcGetPage_DoesNotServerError(string url)
    {
        var res = await _client.GetAsync(url);
        ((int)res.StatusCode).Should().BeLessThan(500, $"το {url} δεν πρέπει να ρίχνει server error");
    }

    // ---------------- API GET endpoints ----------------
    [Theory]
    [InlineData("/api/FormulaAnalysis/1/analyze")]
    [InlineData("/api/FormulaAnalysis/1/allergens")]
    [InlineData("/api/FormulaAnalysis/1/prices")]
    [InlineData("/api/FormulaAnalysis/1/heavymetals")]
    [InlineData("/api/FormulaAnalysis/allergens/eu-list")]
    [InlineData("/api/FormulaAnalysis/heavymetals/limits")]
    [InlineData("/api/FormulaAnalysis/1/predict-success")]
    [InlineData("/api/FormulaAnalysis/ml/model-info")]
    [InlineData("/api/cosing/functions")]
    [InlineData("/api/v1/subscription/plans")]
    [InlineData("/api/v1/Api/health")]
    public async Task ApiGetEndpoint_DoesNotServerError(string url)
    {
        var res = await _client.GetAsync(url);
        ((int)res.StatusCode).Should().BeLessThan(500, $"το {url} δεν πρέπει να ρίχνει server error");
    }

    [Fact]
    public async Task ApiPredictSuccess_ReturnsProbabilityJson()
    {
        var res = await _client.GetAsync("/api/FormulaAnalysis/1/predict-success");
        ((int)res.StatusCode).Should().Be(200);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("successProbability");
    }

    // ---------------- Broad API GET coverage (controllers + services) ----------------
    [Theory]
    // ApiController (api/v1/Api)
    [InlineData("/api/v1/Api/me")]
    [InlineData("/api/v1/Api/formulas")]
    [InlineData("/api/v1/Api/formulas/1")]
    [InlineData("/api/v1/Api/formulas/search?query=test")]
    [InlineData("/api/v1/Api/formulas/1/ingredients")]
    [InlineData("/api/v1/Api/formulas/1/allergens")]
    [InlineData("/api/v1/Api/formulas/1/safety")]
    [InlineData("/api/v1/Api/certifications")]
    // ApiCosIng
    [InlineData("/api/cosing/ingredients/search?q=aqua")]
    [InlineData("/api/cosing/ingredients/1")]
    [InlineData("/api/cosing/ingredients/by-function?function=Solvent")]
    [InlineData("/api/cosing/check/prohibited?inciName=aqua")]
    [InlineData("/api/cosing/check/restricted?inciName=aqua")]
    [InlineData("/api/cosing/check/full?inciName=aqua")]
    [InlineData("/api/cosing/annex/II")]
    [InlineData("/api/cosing/stats")]
    // ApiExtended (api/v1)
    [InlineData("/api/v1/clients")]
    [InlineData("/api/v1/clients/1")]
    [InlineData("/api/v1/suppliers")]
    [InlineData("/api/v1/suppliers/1")]
    [InlineData("/api/v1/incis")]
    [InlineData("/api/v1/incis/1")]
    [InlineData("/api/v1/rawmaterials")]
    [InlineData("/api/v1/rawmaterials/1")]
    [InlineData("/api/v1/rawmaterials/1/samples")]
    [InlineData("/api/v1/restrictions")]
    [InlineData("/api/v1/formulas/1/natural-calculation")]
    [InlineData("/api/v1/formulas/1/water-calculation")]
    // ApiFeatureFlags
    [InlineData("/api/v1/feature-flags")]
    [InlineData("/api/v1/feature-flags/details")]
    // ApiFormulaApproval
    [InlineData("/api/formula-approvals/my-approvals")]
    [InlineData("/api/formula-approvals/pending")]
    [InlineData("/api/formula-approvals/overdue")]
    [InlineData("/api/formula-approvals/statistics")]
    // ApiFormulaClone
    [InlineData("/api/formulas/1/clone/generate-code")]
    // ApiFormulaDocuments (api/v1/formulas/1/documents)
    [InlineData("/api/v1/formulas/1/documents")]
    [InlineData("/api/v1/formulas/1/documents/formula-price")]
    [InlineData("/api/v1/formulas/1/documents/natural-formula")]
    [InlineData("/api/v1/formulas/1/documents/heavy-metals")]
    [InlineData("/api/v1/formulas/1/documents/regulated-ingredients")]
    [InlineData("/api/v1/formulas/1/documents/quantitative-inci")]
    [InlineData("/api/v1/formulas/1/documents/marketing-info")]
    [InlineData("/api/v1/formulas/1/documents/allergens")]
    [InlineData("/api/v1/formulas/1/documents/claims")]
    [InlineData("/api/v1/formulas/1/documents/toxicological-profile")]
    [InlineData("/api/v1/formulas/1/documents/master-formula")]
    [InlineData("/api/v1/formulas/1/documents/inci-list")]
    [InlineData("/api/v1/formulas/1/documents/qualitative-quantitative")]
    // ApiFormulaTemplate
    [InlineData("/api/formula-templates")]
    [InlineData("/api/formula-templates/top-rated")]
    [InlineData("/api/formula-templates/most-used")]
    [InlineData("/api/formula-templates/categories")]
    // ApiFormulaVersion
    [InlineData("/api/formulas/1/versions")]
    // ApiProduction
    [InlineData("/api/v1/production/batches")]
    [InlineData("/api/v1/production/statistics")]
    [InlineData("/api/v1/production/formulas/1/batches")]
    // ApiProductManagement (api/v1/formulas/1)
    [InlineData("/api/v1/formulas/1/inci")]
    [InlineData("/api/v1/formulas/1/allergens")]
    [InlineData("/api/v1/formulas/1/heavy-metals")]
    [InlineData("/api/v1/formulas/1/claims")]
    [InlineData("/api/v1/formulas/1/toxicology")]
    [InlineData("/api/v1/formulas/1/marketing")]
    [InlineData("/api/v1/formulas/1/price")]
    [InlineData("/api/v1/formulas/1/natural")]
    [InlineData("/api/v1/formulas/1/safety")]
    // ApiReports
    [InlineData("/api/v1/reports/all-clients")]
    [InlineData("/api/v1/reports/all-suppliers")]
    [InlineData("/api/v1/reports/all-incis")]
    [InlineData("/api/v1/reports/all-raw-materials")]
    [InlineData("/api/v1/reports/used-raw-materials")]
    [InlineData("/api/v1/reports/unused-raw-materials")]
    [InlineData("/api/v1/reports/expired-samples")]
    // ApiSubscription
    [InlineData("/api/v1/subscription/status")]
    [InlineData("/api/v1/subscription/invoices")]
    // ApiUtility
    [InlineData("/api/v1/utility/product-categories")]
    [InlineData("/api/v1/utility/inci-search?q=aqua")]
    [InlineData("/api/v1/utility/statistics")]
    [InlineData("/api/v1/utility/info")]
    // ApiWebhooks
    [InlineData("/api/v1/webhooks")]
    // ApiDocuments
    [InlineData("/api/v1/documents")]
    // FormulaGrouping
    [InlineData("/api/FormulaGrouping/group-by-formula-type")]
    [InlineData("/api/FormulaGrouping/group-by-project-id")]
    [InlineData("/api/FormulaGrouping/group-by-created-date/Month")]
    public async Task BroadApiGet_DoesNotServerError(string url)
    {
        var res = await _client.GetAsync(url);
        ((int)res.StatusCode).Should().BeLessThan(500, $"το {url} δεν πρέπει να ρίχνει server error");
    }
}
