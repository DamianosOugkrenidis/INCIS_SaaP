// Author: Damianos Pythagoras Ougkrenidis
//
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για διαχείριση formula templates
/// </summary>
public class FormulaTemplateService
{
    private readonly INCISDbContext _context;
    private readonly ILogger<FormulaTemplateService> _logger;

    public FormulaTemplateService(INCISDbContext context, ILogger<FormulaTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Δημιουργία template από υπάρχουσα φόρμουλα
    /// </summary>
    public async Task<FormulaTemplate> CreateTemplateFromFormulaAsync(int formulaId, string templateName, 
        string templateCode, int? categoryId, string userId)
    {
        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials).ThenInclude(frm => frm.RawMaterial)
            .Include(f => f.INCIIngredients)
            .Include(f => f.SafetyData)
            .Include(f => f.MarketingInfo)
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null)
            throw new ArgumentException($"Formula with ID {formulaId} not found");

        // Serialize φόρμουλα σε template data
        var templateData = SerializeFormulaAsTemplate(formula);

        var template = new FormulaTemplate
        {
            TemplateName = templateName,
            TemplateCode = templateCode,
            CategoryId = categoryId,
            ProductType = formula.ProductType,
            TemplateData = templateData,
            TemplateType = "Complete",
            TargetUse = formula.ProductType,
            IsPublic = true,
            IsSystemTemplate = false,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<FormulaTemplate>().Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created template '{templateName}' from formula {formulaId} by user {userId}");

        return template;
    }

    /// <summary>
    /// Εφαρμογή template σε νέα φόρμουλα
    /// </summary>
    public async Task<Formula> ApplyTemplateAsync(int templateId, string newFormulaCode, int? clientId, string userId)
    {
        var template = await _context.Set<FormulaTemplate>()
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
            throw new ArgumentException($"Template with ID {templateId} not found");

        // Deserialize template data
        var templateData = JsonSerializer.Deserialize<Dictionary<string, object>>(template.TemplateData);

        // Δημιούργησε νέα φόρμουλα από το template
        var formula = new Formula
        {
            FormulaCode = newFormulaCode,
            ProductName = template.TemplateName,
            ProductType = template.ProductType,
            ClientId = clientId,
            CreationDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            IsWIP = true
        };

        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        // Καταγραφή χρήσης template
        var usage = new FormulaTemplateUsage
        {
            TemplateId = templateId,
            FormulaId = formula.Id,
            UsedAt = DateTime.UtcNow,
            UsedBy = userId,
            ModificationPercentage = 0
        };

        _context.Set<FormulaTemplateUsage>().Add(usage);

        // Ενημέρωση statistics
        template.UsageCount++;
        template.LastUsed = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Applied template {templateId} to create formula {formula.Id}");

        return formula;
    }

    /// <summary>
    /// Λήψη όλων των templates
    /// </summary>
    public async Task<List<FormulaTemplate>> GetTemplatesAsync(int? categoryId = null, string? productType = null, 
        bool? isPublic = null)
    {
        var query = _context.Set<FormulaTemplate>()
            .Include(t => t.Category)
            .Where(t => t.IsActive);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (!string.IsNullOrEmpty(productType))
            query = query.Where(t => t.ProductType == productType);

        if (isPublic.HasValue)
            query = query.Where(t => t.IsPublic == isPublic.Value);

        return await query
            .OrderByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.Rating)
            .ToListAsync();
    }

    /// <summary>
    /// Αναζήτηση templates
    /// </summary>
    public async Task<List<FormulaTemplate>> SearchTemplatesAsync(string searchTerm)
    {
        return await _context.Set<FormulaTemplate>()
            .Include(t => t.Category)
            .Where(t => t.IsActive && 
                   (t.TemplateName.Contains(searchTerm) || 
                    t.TemplateCode.Contains(searchTerm) ||
                    (t.Description != null && t.Description.Contains(searchTerm)) ||
                    (t.Tags != null && t.Tags.Contains(searchTerm))))
            .OrderByDescending(t => t.UsageCount)
            .ToListAsync();
    }

    /// <summary>
    /// Βαθμολόγηση template
    /// </summary>
    public async Task<bool> RateTemplateAsync(int templateId, int rating, string userId)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        var template = await _context.Set<FormulaTemplate>().FindAsync(templateId);
        if (template == null)
            return false;

        // Υπολογισμός νέου μέσου όρου (απλοποιημένος)
        decimal newRating = ((template.Rating ?? 0) * template.RatingCount + rating) / (template.RatingCount + 1);
        
        template.Rating = newRating;
        template.RatingCount++;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {userId} rated template {templateId} with {rating} stars");

        return true;
    }

    /// <summary>
    /// Δημιουργία κατηγορίας template
    /// </summary>
    public async Task<TemplateCategory> CreateCategoryAsync(string name, string? description, 
        int? parentCategoryId, string userId)
    {
        var category = new TemplateCategory
        {
            Name = name,
            Description = description,
            ParentCategoryId = parentCategoryId,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            DisplayOrder = 0
        };

        _context.Set<TemplateCategory>().Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created template category '{name}' by user {userId}");

        return category;
    }

    /// <summary>
    /// Λήψη δέντρου κατηγοριών
    /// </summary>
    public async Task<List<TemplateCategory>> GetCategoryTreeAsync()
    {
        return await _context.Set<TemplateCategory>()
            .Include(c => c.SubCategories)
            .Include(c => c.Templates)
            .Where(c => c.IsActive && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Λήψη top rated templates
    /// </summary>
    public async Task<List<FormulaTemplate>> GetTopRatedTemplatesAsync(int count = 10)
    {
        return await _context.Set<FormulaTemplate>()
            .Include(t => t.Category)
            .Where(t => t.IsActive && t.IsPublic && t.Rating.HasValue)
            .OrderByDescending(t => t.Rating)
            .ThenByDescending(t => t.RatingCount)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Λήψη most used templates
    /// </summary>
    public async Task<List<FormulaTemplate>> GetMostUsedTemplatesAsync(int count = 10)
    {
        return await _context.Set<FormulaTemplate>()
            .Include(t => t.Category)
            .Where(t => t.IsActive && t.IsPublic)
            .OrderByDescending(t => t.UsageCount)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Διαγραφή template
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int templateId, string userId)
    {
        var template = await _context.Set<FormulaTemplate>().FindAsync(templateId);
        if (template == null)
            return false;

        // Soft delete
        template.IsActive = false;
        template.LastModified = DateTime.UtcNow;
        template.LastModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Deleted template {templateId} by user {userId}");

        return true;
    }

    /// <summary>
    /// Serialize φόρμουλα για template
    /// </summary>
    private string SerializeFormulaAsTemplate(Formula formula)
    {
        var templateObj = new
        {
            FormulaCode = formula.FormulaCode,
            ProductName = formula.ProductName,
            ProductType = formula.ProductType,
            ManufacturingMethod = formula.ManufacturingMethod,
            RawMaterials = formula.FormulaRawMaterials.Select(frm => new
            {
                RawMaterialId = frm.RawMaterialId,
                RawMaterialCode = frm.RawMaterial?.Code,
                RawMaterialName = frm.RawMaterial?.ProductName,
                Percentage = frm.Percentage,
                Phase = frm.Phase
            }).ToList(),
            INCIIngredients = formula.INCIIngredients.Select(ii => new
            {
                INCIName = ii.INCIName,
                Function = ii.Function,
                Percentage = ii.Percentage
            }).ToList(),
            SafetyData = formula.SafetyData,
            MarketingInfo = formula.MarketingInfo
        };

        return JsonSerializer.Serialize(templateObj, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}
