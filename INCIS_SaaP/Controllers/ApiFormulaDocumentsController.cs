// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API endpoints για δημιουργία εγγράφων φόρμουλας (FORM1-FORM14)
/// EU Cosmetics Regulation 1223/2009 compliance documents
/// </summary>
[Route("api/v1/formulas/{formulaId}/documents")]
[ApiController]
[Authorize]
public class ApiFormulaDocumentsController : ControllerBase
{
    private readonly FormulaDocumentService _documentService;

    public ApiFormulaDocumentsController(FormulaDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>
    /// Λίστα διαθέσιμων εγγράφων για μια φόρμουλα
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDocumentsIndex(int formulaId)
    {
        var result = await _documentService.GetDocumentsIndex(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM1 - Formula Price Document
    /// </summary>
    [HttpGet("formula-price")]
    public async Task<IActionResult> GetFormulaPrice(int formulaId)
    {
        var result = await _documentService.GetFormulaPriceDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM2 - Natural Formula Document
    /// </summary>
    [HttpGet("natural-formula")]
    public async Task<IActionResult> GetNaturalFormula(int formulaId)
    {
        var result = await _documentService.GetNaturalFormulaDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM3 - Heavy Metals &amp; Impurities Document
    /// </summary>
    [HttpGet("heavy-metals")]
    public async Task<IActionResult> GetHeavyMetals(int formulaId)
    {
        var result = await _documentService.GetHeavyMetalsDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM4 - Identification of Regulated Ingredients
    /// </summary>
    [HttpGet("regulated-ingredients")]
    public async Task<IActionResult> GetRegulatedIngredients(int formulaId)
    {
        var result = await _documentService.GetRegulatedIngredientsDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM5 - Quantitative INCI Composition
    /// </summary>
    [HttpGet("quantitative-inci")]
    public async Task<IActionResult> GetQuantitativeInci(int formulaId)
    {
        var result = await _documentService.GetQuantitativeInciDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM6 - Marketing Info
    /// </summary>
    [HttpGet("marketing-info")]
    public async Task<IActionResult> GetMarketingInfo(int formulaId)
    {
        var result = await _documentService.GetMarketingInfoDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM8 - Allergens Composition
    /// </summary>
    [HttpGet("allergens")]
    public async Task<IActionResult> GetAllergens(int formulaId)
    {
        var result = await _documentService.GetAllergensDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM10 - Formula Claims (Vegan, GMO-Free, etc.)
    /// </summary>
    [HttpGet("claims")]
    public async Task<IActionResult> GetFormulaClaims(int formulaId)
    {
        var result = await _documentService.GetFormulaClaimsDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM11 - Summarized Toxicological Profile
    /// </summary>
    [HttpGet("toxicological-profile")]
    public async Task<IActionResult> GetToxicologicalProfile(int formulaId)
    {
        var result = await _documentService.GetToxicologicalProfileDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM12 - Master Formula
    /// </summary>
    [HttpGet("master-formula")]
    public async Task<IActionResult> GetMasterFormula(int formulaId)
    {
        var result = await _documentService.GetMasterFormulaDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM13 - INCI List (for product labeling)
    /// </summary>
    [HttpGet("inci-list")]
    public async Task<IActionResult> GetInciList(int formulaId)
    {
        var result = await _documentService.GetInciListDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }

    /// <summary>
    /// FORM14 - Qualitative &amp; Quantitative Composition
    /// </summary>
    [HttpGet("qualitative-quantitative")]
    public async Task<IActionResult> GetQualitativeQuantitative(int formulaId)
    {
        var result = await _documentService.GetQualitativeQuantitativeDocument(formulaId);
        if (result == null) return NotFound(new { message = "Formula not found" });
        return Ok(result);
    }
}
