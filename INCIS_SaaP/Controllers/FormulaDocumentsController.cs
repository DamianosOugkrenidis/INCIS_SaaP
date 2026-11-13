// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class FormulaDocumentsController : Controller
{
    private readonly FormulaDocumentService _documentService;
    private readonly ExportService _exportService;

    public FormulaDocumentsController(FormulaDocumentService documentService, ExportService exportService)
    {
        _documentService = documentService;
        _exportService = exportService;
    }

    // Document Hub
    public async Task<IActionResult> Index(int id)
    {
        var model = await _documentService.GetDocumentsIndex(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // PDF export του πλήρους ντοσιέ φόρμουλας
    public async Task<IActionResult> Pdf(int id)
    {
        var model = await _documentService.GetDocumentsIndex(id);
        if (model == null) return NotFound();

        var bytes = await _exportService.ExportFormulaToPdfAsync(id);
        var fileName = $"Formula_{model.FormulaCode}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    // FORM1 - Formula Price
    public async Task<IActionResult> FormulaPrice(int id)
    {
        var model = await _documentService.GetFormulaPriceDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM2 - Natural Formula
    public async Task<IActionResult> NaturalFormula(int id)
    {
        var model = await _documentService.GetNaturalFormulaDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM3 - Heavy Metals
    public async Task<IActionResult> HeavyMetals(int id)
    {
        var model = await _documentService.GetHeavyMetalsDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM4 - Regulated Ingredients
    public async Task<IActionResult> RegulatedIngredients(int id)
    {
        var model = await _documentService.GetRegulatedIngredientsDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM5 - Quantitative INCI
    public async Task<IActionResult> QuantitativeInci(int id)
    {
        var model = await _documentService.GetQuantitativeInciDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM6 - Marketing Info
    public async Task<IActionResult> MarketingInfo(int id)
    {
        var model = await _documentService.GetMarketingInfoDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM8 - Allergens
    public async Task<IActionResult> Allergens(int id)
    {
        var model = await _documentService.GetAllergensDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM10 - Formula Claims
    public async Task<IActionResult> FormulaClaims(int id)
    {
        var model = await _documentService.GetFormulaClaimsDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM11 - Toxicological Profile
    public async Task<IActionResult> ToxicologicalProfile(int id)
    {
        var model = await _documentService.GetToxicologicalProfileDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM12 - Master Formula
    public async Task<IActionResult> MasterFormula(int id)
    {
        var model = await _documentService.GetMasterFormulaDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM13 - INCI List
    public async Task<IActionResult> InciList(int id)
    {
        var model = await _documentService.GetInciListDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // FORM14 - Qualitative & Quantitative
    public async Task<IActionResult> QualitativeQuantitative(int id)
    {
        var model = await _documentService.GetQualitativeQuantitativeDocument(id);
        if (model == null) return NotFound();
        return View(model);
    }
}
