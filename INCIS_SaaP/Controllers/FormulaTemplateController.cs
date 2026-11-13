// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class FormulaTemplateController : Controller
{
    private readonly FormulaTemplateService _templateService;

    public FormulaTemplateController(FormulaTemplateService templateService)
    {
        _templateService = templateService;
    }

    public async Task<IActionResult> Index(int? categoryId, string? productType)
    {
        var templates = await _templateService.GetTemplatesAsync(categoryId, productType, true);
        ViewBag.Categories = await _templateService.GetCategoryTreeAsync();
        return View(templates);
    }

    public async Task<IActionResult> TopRated()
    {
        var templates = await _templateService.GetTopRatedTemplatesAsync(20);
        return View(templates);
    }

    public async Task<IActionResult> MostUsed()
    {
        var templates = await _templateService.GetMostUsedTemplatesAsync(20);
        return View(templates);
    }

    [Authorize(Roles = "Admin,SuperUser")]
    public IActionResult Create()
    {
        return View();
    }

    public async Task<IActionResult> Apply(int templateId)
    {
        await Task.CompletedTask;
        ViewBag.TemplateId = templateId;
        return View();
    }
}
