// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για κλωνοποίηση φορμούλων
/// </summary>
public class FormulaCloneService
{
    private readonly INCISDbContext _context;
    private readonly ILogger<FormulaCloneService> _logger;

    public FormulaCloneService(INCISDbContext context, ILogger<FormulaCloneService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Κλωνοποίηση φόρμουλας με νέο κωδικό
    /// </summary>
    public async Task<Formula> CloneFormulaAsync(int sourceFormulaId, string newFormulaCode, string userId)
    {
        var sourceFormula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials).ThenInclude(frm => frm.RawMaterial)
            .Include(f => f.INCIIngredients)
            .Include(f => f.SafetyData)
            .Include(f => f.PriceInfo)
            .Include(f => f.Allergens)
            .Include(f => f.HeavyMetals)
            .Include(f => f.MarketingInfo)
            .Include(f => f.NaturalCertification)
            .Include(f => f.Claims)
            .FirstOrDefaultAsync(f => f.Id == sourceFormulaId);

        if (sourceFormula == null)
            throw new ArgumentException($"Source formula with ID {sourceFormulaId} not found");

        // Δημιουργία νέας φόρμουλας
        var clonedFormula = new Formula
        {
            FormulaCode = newFormulaCode,
            ProductName = sourceFormula.ProductName + " (Clone)",
            ProductType = sourceFormula.ProductType,
            ManufacturingSite = sourceFormula.ManufacturingSite,
            ManufacturingMethod = sourceFormula.ManufacturingMethod,
            PPIdCode = sourceFormula.PPIdCode,
            PackagingInfo = sourceFormula.PackagingInfo,
            PhysicochemicalProperties = sourceFormula.PhysicochemicalProperties,
            MicrobiologicalProperties = sourceFormula.MicrobiologicalProperties,
            Comments = $"Cloned from {sourceFormula.FormulaCode}",
            ClientId = sourceFormula.ClientId,
            TestNumber = sourceFormula.TestNumber,
            IsExternalDevelopment = sourceFormula.IsExternalDevelopment,
            IsWIP = true, // Νέα φόρμουλα πάντα ξεκινάει ως WIP
            CreationDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        _context.Formulas.Add(clonedFormula);
        await _context.SaveChangesAsync();

        // Clone raw materials
        foreach (var sourceRawMaterial in sourceFormula.FormulaRawMaterials.Where(frm => frm.IsActive))
        {
            var clonedRawMaterial = new FormulaRawMaterial
            {
                FormulaId = clonedFormula.Id,
                RawMaterialId = sourceRawMaterial.RawMaterialId,
                Percentage = sourceRawMaterial.Percentage,
                Phase = sourceRawMaterial.Phase,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<FormulaRawMaterial>().Add(clonedRawMaterial);
        }

        // Clone INCI ingredients
        foreach (var sourceINCI in sourceFormula.INCIIngredients)
        {
            var clonedINCI = new INCIIngredient
            {
                FormulaId = clonedFormula.Id,
                INCIName = sourceINCI.INCIName,
                Function = sourceINCI.Function,
                Percentage = sourceINCI.Percentage,
                CASNumber = sourceINCI.CASNumber,
                ECNumber = sourceINCI.ECNumber,
                Origin = sourceINCI.Origin
            };
            _context.Set<INCIIngredient>().Add(clonedINCI);
        }

        // Clone safety data
        if (sourceFormula.SafetyData != null)
        {
            var clonedSafetyData = new SafetyData
            {
                FormulaId = clonedFormula.Id,
                IrritationLevel = sourceFormula.SafetyData.IrritationLevel,
                SkinCompatibility = sourceFormula.SafetyData.SkinCompatibility,
                EyeIrritation = sourceFormula.SafetyData.EyeIrritation,
                IsSafeForPregnancy = sourceFormula.SafetyData.IsSafeForPregnancy,
                Phototoxicity = sourceFormula.SafetyData.Phototoxicity,
                Sensitization = sourceFormula.SafetyData.Sensitization,
                Warnings = sourceFormula.SafetyData.Warnings,
                Precautions = sourceFormula.SafetyData.Precautions
            };
            _context.Set<SafetyData>().Add(clonedSafetyData);
        }

        // Clone marketing info
        if (sourceFormula.MarketingInfo != null)
        {
            var clonedMarketingInfo = new MarketingInfo
            {
                FormulaId = clonedFormula.Id,
                ProductName = sourceFormula.MarketingInfo.ProductName,
                ProductDescription = sourceFormula.MarketingInfo.ProductDescription,
                TargetAudience = sourceFormula.MarketingInfo.TargetAudience,
                AgeRange = sourceFormula.MarketingInfo.AgeRange,
                GenderTarget = sourceFormula.MarketingInfo.GenderTarget,
                KeyBenefits = sourceFormula.MarketingInfo.KeyBenefits,
                UsageInstructions = sourceFormula.MarketingInfo.UsageInstructions,
                PackagingType = sourceFormula.MarketingInfo.PackagingType,
                DistributionChannel = sourceFormula.MarketingInfo.DistributionChannel
            };
            _context.Set<MarketingInfo>().Add(clonedMarketingInfo);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Cloned formula {sourceFormulaId} to {clonedFormula.Id} with code {newFormulaCode} by user {userId}");

        return clonedFormula;
    }

    /// <summary>
    /// Κλωνοποίηση φόρμουλας σε διαφορετικό πελάτη
    /// </summary>
    public async Task<Formula> CloneFormulaToClientAsync(int sourceFormulaId, string newFormulaCode, 
        int newClientId, string userId)
    {
        var clonedFormula = await CloneFormulaAsync(sourceFormulaId, newFormulaCode, userId);
        
        // Ενημέρωση client
        clonedFormula.ClientId = newClientId;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Cloned formula {sourceFormulaId} to client {newClientId}");

        return clonedFormula;
    }

    /// <summary>
    /// Κλωνοποίηση φόρμουλας με τροποποιήσεις
    /// </summary>
    public async Task<Formula> CloneFormulaWithModificationsAsync(int sourceFormulaId, string newFormulaCode,
        Dictionary<string, object> modifications, string userId)
    {
        var clonedFormula = await CloneFormulaAsync(sourceFormulaId, newFormulaCode, userId);

        // Εφαρμογή τροποποιήσεων
        if (modifications.ContainsKey("ProductName"))
            clonedFormula.ProductName = modifications["ProductName"]?.ToString();

        if (modifications.ContainsKey("ProductType"))
            clonedFormula.ProductType = modifications["ProductType"]?.ToString();

        if (modifications.ContainsKey("ManufacturingSite"))
            clonedFormula.ManufacturingSite = modifications["ManufacturingSite"]?.ToString();

        if (modifications.ContainsKey("ClientId") && modifications["ClientId"] != null)
            clonedFormula.ClientId = Convert.ToInt32(modifications["ClientId"]);

        clonedFormula.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Cloned formula {sourceFormulaId} with modifications to {clonedFormula.Id}");

        return clonedFormula;
    }

    /// <summary>
    /// Bulk κλωνοποίηση πολλαπλών φορμούλων
    /// </summary>
    public async Task<List<Formula>> CloneMultipleFormulasAsync(List<int> formulaIds, string userId)
    {
        var clonedFormulas = new List<Formula>();

        foreach (var formulaId in formulaIds)
        {
            try
            {
                var sourceFormula = await _context.Formulas.FindAsync(formulaId);
                if (sourceFormula == null)
                    continue;

                var newCode = $"{sourceFormula.FormulaCode}-CLONE-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var clonedFormula = await CloneFormulaAsync(formulaId, newCode, userId);
                clonedFormulas.Add(clonedFormula);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cloning formula {formulaId}: {ex.Message}");
            }
        }

        _logger.LogInformation($"Cloned {clonedFormulas.Count} out of {formulaIds.Count} formulas by user {userId}");

        return clonedFormulas;
    }

    /// <summary>
    /// Ελέγχει αν ο κωδικός φόρμουλας υπάρχει ήδη
    /// </summary>
    public async Task<bool> CheckFormulaCodeExistsAsync(string formulaCode)
    {
        return await _context.Formulas.AnyAsync(f => f.FormulaCode == formulaCode);
    }

    /// <summary>
    /// Δημιουργία unique formula code
    /// </summary>
    public async Task<string> GenerateUniqueFormulaCodeAsync(string baseCode)
    {
        var newCode = baseCode;
        var counter = 1;

        while (await CheckFormulaCodeExistsAsync(newCode))
        {
            newCode = $"{baseCode}-{counter}";
            counter++;
        }

        return newCode;
    }
}
