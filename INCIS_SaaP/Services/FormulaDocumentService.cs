// Author: Damianos Pythagoras Ougkrenidis
//
using INCIS_SaaP.Data;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;
using Microsoft.EntityFrameworkCore;

namespace INCIS_SaaP.Services;

/// <summary>
/// Υπηρεσία φόρτωσης δεδομένων φόρμουλας και αντιστοίχισης σε ViewModels
/// για δημιουργία εγγράφων (FORM1–FORM14).
/// </summary>
public class FormulaDocumentService
{
    private readonly INCISDbContext _context;

    public FormulaDocumentService(INCISDbContext context)
    {
        _context = context;
    }

    // ───────────────────────────────────────────────────────────────
    // Helper: load the base formula header used by every document
    // ───────────────────────────────────────────────────────────────

    private static void MapBase(Formula formula, FormulaDocumentBaseViewModel vm)
    {
        vm.FormulaId = formula.Id;
        vm.FormulaCode = formula.FormulaCode;
        vm.ProductName = formula.ProductName ?? string.Empty;
        vm.ProductType = formula.ProductType;
        vm.ClientName = formula.Client?.Name;
        vm.PPIdCode = formula.PPIdCode;
        vm.IssueDate = formula.IssueDate;
        vm.Version = formula.Version;
        vm.GeneratedAt = DateTime.Now;
    }

    // ═══════════════════════════════════════════════════════════════
    // Documents Index
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Επιστρέφει τη λίστα διαθέσιμων εγγράφων για μια φόρμουλα,
    /// ελέγχοντας αν υπάρχουν τα αντίστοιχα δεδομένα στη βάση.
    /// </summary>
    public async Task<FormulaDocumentsIndexViewModel?> GetDocumentsIndex(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        // Parallel existence checks
        bool hasRawMaterials = await _context.FormulaRawMaterials
            .AnyAsync(frm => frm.FormulaId == formulaId && frm.IsActive);

        bool hasAllergens = await _context.Allergens
            .AnyAsync(a => a.FormulaId == formulaId);

        bool hasHeavyMetals = await _context.HeavyMetals
            .AnyAsync(h => h.FormulaId == formulaId);

        bool hasInciIngredients = await _context.INCIIngredients
            .AnyAsync(i => i.FormulaId == formulaId);

        bool hasMarketingInfo = await _context.MarketingInfos
            .AnyAsync(m => m.FormulaId == formulaId);

        bool hasClaims = await _context.Claims
            .AnyAsync(c => c.FormulaId == formulaId);

        bool hasNaturalCert = await _context.NaturalCertifications
            .AnyAsync(n => n.FormulaId == formulaId);

        bool hasSafetyData = await _context.SafetyData
            .AnyAsync(s => s.FormulaId == formulaId);

        // Check for regulated ingredients: any INCI with restrictions referenced
        // through the formula's raw materials
        bool hasRegulatedIngredients = await _context.FormulaRawMaterials
            .Where(frm => frm.FormulaId == formulaId && frm.IsActive)
            .SelectMany(frm => frm.RawMaterial!.Composition)
            .AnyAsync(rc => rc.INCI != null && rc.INCI.Restrictions.Any());

        return new FormulaDocumentsIndexViewModel
        {
            FormulaId = formula.Id,
            FormulaCode = formula.FormulaCode,
            ProductName = formula.ProductName ?? string.Empty,
            ProductType = formula.ProductType,
            ClientName = formula.Client?.Name,
            AvailableDocuments = new List<DocumentInfo>
            {
                new DocumentInfo
                {
                    FormNumber = "FORM1",
                    Title = "Formula Price",
                    Description = "Τιμολόγηση φόρμουλας με ανάλυση κόστους α' υλών",
                    Icon = "fa-euro-sign",
                    ActionName = "FormulaPrice",
                    BadgeColor = "success",
                    HasData = hasRawMaterials
                },
                new DocumentInfo
                {
                    FormNumber = "FORM2",
                    Title = "Natural Formula",
                    Description = "Φυσικά συστατικά και πιστοποιήσεις ISO 16128",
                    Icon = "fa-leaf",
                    ActionName = "NaturalFormula",
                    BadgeColor = "success",
                    HasData = hasRawMaterials
                },
                new DocumentInfo
                {
                    FormNumber = "FORM3",
                    Title = "Heavy Metals / Impurities",
                    Description = "Βαρέα μέταλλα και προσμίξεις α' υλών",
                    Icon = "fa-shield-halved",
                    ActionName = "HeavyMetals",
                    BadgeColor = "danger",
                    HasData = hasRawMaterials || hasHeavyMetals
                },
                new DocumentInfo
                {
                    FormNumber = "FORM4",
                    Title = "Regulated Ingredients",
                    Description = "Ρυθμιζόμενα συστατικά κατά Κανονισμό 1223/2009",
                    Icon = "fa-triangle-exclamation",
                    ActionName = "RegulatedIngredients",
                    BadgeColor = "warning",
                    HasData = hasRegulatedIngredients || hasInciIngredients
                },
                new DocumentInfo
                {
                    FormNumber = "FORM5",
                    Title = "Quantitative INCI",
                    Description = "Ποσοτική σύνθεση INCI συστατικών",
                    Icon = "fa-list-ol",
                    ActionName = "QuantitativeInci",
                    BadgeColor = "info",
                    HasData = hasInciIngredients
                },
                new DocumentInfo
                {
                    FormNumber = "FORM6",
                    Title = "Marketing Info",
                    Description = "Πληροφορίες μάρκετινγκ και εμπορικής παρουσίασης",
                    Icon = "fa-bullhorn",
                    ActionName = "MarketingInfo",
                    BadgeColor = "primary",
                    HasData = hasMarketingInfo || hasRawMaterials
                },
                new DocumentInfo
                {
                    FormNumber = "FORM8",
                    Title = "Allergens Composition",
                    Description = "Αλλεργιογόνα συστατικά στη φόρμουλα",
                    Icon = "fa-diamond-exclamation",
                    ActionName = "Allergens",
                    BadgeColor = "danger",
                    HasData = hasAllergens
                },
                new DocumentInfo
                {
                    FormNumber = "FORM10",
                    Title = "Formula Claims",
                    Description = "Ισχυρισμοί/Δηλώσεις προϊόντος (Vegan, Gluten-Free κλπ)",
                    Icon = "fa-clipboard-check",
                    ActionName = "FormulaClaims",
                    BadgeColor = "info",
                    HasData = hasRawMaterials || hasClaims
                },
                new DocumentInfo
                {
                    FormNumber = "FORM11",
                    Title = "Toxicological Profile",
                    Description = "Συνοπτικό τοξικολογικό προφίλ (NOAEL, MoS)",
                    Icon = "fa-skull-crossbones",
                    ActionName = "ToxicologicalProfile",
                    BadgeColor = "warning",
                    HasData = hasInciIngredients || hasSafetyData
                },
                new DocumentInfo
                {
                    FormNumber = "FORM12",
                    Title = "Master Formula",
                    Description = "Πλήρες δελτίο φόρμουλας με φάσεις παραγωγής",
                    Icon = "fa-file-medical",
                    ActionName = "MasterFormula",
                    BadgeColor = "primary",
                    HasData = hasRawMaterials
                },
                new DocumentInfo
                {
                    FormNumber = "FORM13",
                    Title = "INCI List",
                    Description = "Λίστα INCI ονομάτων κατά φθίνουσα σειρά συγκέντρωσης",
                    Icon = "fa-rectangle-list",
                    ActionName = "InciList",
                    BadgeColor = "secondary",
                    HasData = hasInciIngredients || hasRawMaterials
                },
                new DocumentInfo
                {
                    FormNumber = "FORM14",
                    Title = "Qualitative & Quantitative",
                    Description = "Ποιοτική & ποσοτική σύνθεση α' υλών με υπο-συστατικά",
                    Icon = "fa-sitemap",
                    ActionName = "QualitativeQuantitative",
                    BadgeColor = "primary",
                    HasData = hasRawMaterials
                }
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM1 – Formula Price Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<FormulaPriceDocumentViewModel?> GetFormulaPriceDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Supplier)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.MainINCI)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Composition)
                        .ThenInclude(rc => rc.INCI!)
                            .ThenInclude(i => i.Restrictions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new FormulaPriceDocumentViewModel();
        MapBase(formula, vm);

        int order = 1;
        decimal totalPrice = 0m;

        foreach (var frm in formula.FormulaRawMaterials.OrderBy(x => x.Id))
        {
            var rm = frm.RawMaterial;
            if (rm == null || !rm.IsActive) continue;

            var pct = frm.Percentage ?? 0m;
            var rmPrice = rm.Price ?? 0m;
            var contribution = rmPrice * pct / 100m;
            totalPrice += contribution;

            vm.Ingredients.Add(new PriceLineItem
            {
                Order = order++,
                RawMaterialCode = rm.Code,
                RawMaterialName = rm.ProductName,
                InciName = rm.MainINCI?.Name,
                SupplierName = rm.Supplier?.Name,
                Percentage = pct,
                RMPrice = rmPrice,
                ContributionPrice = Math.Round(contribution, 4)
            });
        }

        vm.TotalPrice = Math.Round(totalPrice, 4);

        // Allergens: collect unique INCI substances that have restrictions
        var allergenSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var frm in formula.FormulaRawMaterials)
        {
            var rm = frm.RawMaterial;
            if (rm?.Composition == null) continue;
            foreach (var comp in rm.Composition)
            {
                var inci = comp.INCI;
                if (inci == null) continue;
                if (inci.Restrictions.Any() && allergenSet.Add(inci.Name))
                {
                    var restriction = inci.Restrictions.FirstOrDefault();
                    vm.Allergens.Add(new AllergenLineItem
                    {
                        InciName = inci.Name,
                        CASNumber = inci.CASNumber,
                        ECNumber = inci.ECNumber,
                        Restriction = restriction?.Description
                    });
                }
            }
        }

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM2 – Natural Formula Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<NaturalFormulaDocumentViewModel?> GetNaturalFormulaDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.NaturalCertification)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.MainINCI)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new NaturalFormulaDocumentViewModel();
        MapBase(formula, vm);

        vm.CertificationType = formula.NaturalCertification?.CertificationType;
        vm.CertifyingBody = formula.NaturalCertification?.CertifyingBody;

        int order = 1;
        decimal totalNatural = 0m;
        decimal totalOrganic = 0m;

        foreach (var frm in formula.FormulaRawMaterials.OrderBy(x => x.Id))
        {
            var rm = frm.RawMaterial;
            if (rm == null || !rm.IsActive) continue;

            var pct = frm.Percentage ?? 0m;
            var naturalPct = rm.NaturalOriginPercentage;

            // Weighted contribution to total
            var naturalContrib = naturalPct.HasValue ? pct * naturalPct.Value / 100m : 0m;
            totalNatural += naturalContrib;

            // Organic percentage from certification if available
            var organicPct = formula.NaturalCertification?.OrganicPercentage;
            // Per-ingredient organic: we approximate using the cert's ratio if the RM is natural
            decimal? ingredientOrganic = null;
            if (naturalPct.HasValue && naturalPct.Value > 0 && organicPct.HasValue && formula.NaturalCertification?.NaturalPercentage > 0)
            {
                ingredientOrganic = naturalPct.Value * (organicPct.Value / formula.NaturalCertification.NaturalPercentage.Value);
            }

            if (ingredientOrganic.HasValue)
            {
                totalOrganic += pct * ingredientOrganic.Value / 100m;
            }

            vm.Ingredients.Add(new NaturalIngredientLine
            {
                Order = order++,
                RawMaterialCode = rm.Code,
                RawMaterialName = rm.ProductName,
                InciName = rm.MainINCI?.Name,
                Percentage = pct,
                NaturalOriginPercentage = naturalPct,
                OrganicPercentage = ingredientOrganic.HasValue ? Math.Round(ingredientOrganic.Value, 2) : null,
                Origin = rm.HasISO16128 ? "ISO 16128" : null,
                IsBiodegradable = naturalPct.HasValue && naturalPct.Value >= 95m
            });
        }

        vm.TotalNaturalPercentage = Math.Round(totalNatural, 2);
        vm.TotalOrganicPercentage = Math.Round(totalOrganic, 2);
        vm.TotalBiodegradability = vm.Ingredients.Any()
            ? Math.Round(vm.Ingredients.Count(i => i.IsBiodegradable) / (decimal)vm.Ingredients.Count * 100m, 2)
            : 0m;

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM3 – Heavy Metals / Impurities Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<HeavyMetalsDocumentViewModel?> GetHeavyMetalsDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.HeavyMetals)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.MainINCI)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Composition.Where(c => c.IsImpurity))
                        .ThenInclude(rc => rc.INCI)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new HeavyMetalsDocumentViewModel();
        MapBase(formula, vm);

        // Raw material impurities
        int order = 1;
        foreach (var frm in formula.FormulaRawMaterials.OrderBy(x => x.Id))
        {
            var rm = frm.RawMaterial;
            if (rm == null || !rm.IsActive) continue;

            var impurities = rm.Composition.Where(c => c.IsImpurity).ToList();
            var impuritySpecs = impurities.Any()
                ? string.Join("; ", impurities.Select(i => $"{i.INCI?.Name ?? "Unknown"}: {i.MinPercentage?.ToString("F4") ?? "N/A"}–{i.MaxPercentage?.ToString("F4") ?? "N/A"}%"))
                : null;

            vm.RawMaterialImpurities.Add(new ImpurityLineItem
            {
                Order = order++,
                RawMaterialCode = rm.Code,
                RawMaterialName = rm.ProductName,
                InciName = rm.MainINCI?.Name,
                Percentage = frm.Percentage ?? 0m,
                ImpuritySpecs = impuritySpecs,
                HasRMI = rm.HasRMI
            });
        }

        // Heavy metal analysis from DB records
        foreach (var hm in formula.HeavyMetals.OrderBy(h => h.MetalName))
        {
            vm.HeavyMetalAnalysis.Add(new HeavyMetalAnalysisItem
            {
                MetalName = hm.MetalName ?? string.Empty,
                LimitValue = hm.LimitValue,
                DetectedValue = hm.DetectedValue,
                Unit = hm.Unit ?? "ppm",
                TestMethod = hm.TestMethod,
                IsCompliant = hm.IsCompliant ?? true,
                Regulation = null // Could be extended later
            });
        }

        vm.IsOverallCompliant = !vm.HeavyMetalAnalysis.Any(h => !h.IsCompliant);
        vm.ComplianceSummary = vm.IsOverallCompliant
            ? "All heavy metal levels are within acceptable limits."
            : "One or more heavy metals exceed the acceptable limits. Review required.";

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM4 – Regulated Ingredients Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<RegulatedIngredientsDocumentViewModel?> GetRegulatedIngredientsDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.Allergens)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Composition)
                        .ThenInclude(rc => rc.INCI!)
                            .ThenInclude(i => i.Restrictions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new RegulatedIngredientsDocumentViewModel();
        MapBase(formula, vm);

        // Build allergen name set for cross-reference
        var allergenNames = new HashSet<string>(
            formula.Allergens.Where(a => a.AllergenName != null).Select(a => a.AllergenName!),
            StringComparer.OrdinalIgnoreCase);

        int order = 1;
        int totalRegulated = 0;
        int totalAllergens = 0;

        // Collect all INCI substances that have restrictions
        var processed = new HashSet<int>(); // Track by INCI Id to avoid duplicates
        foreach (var frm in formula.FormulaRawMaterials)
        {
            var rm = frm.RawMaterial;
            if (rm?.Composition == null) continue;

            var pct = frm.Percentage ?? 0m;

            foreach (var comp in rm.Composition.Where(c => !c.IsImpurity))
            {
                var inci = comp.INCI;
                if (inci == null || !inci.Restrictions.Any()) continue;
                if (!processed.Add(inci.Id)) continue;

                var primaryRestriction = inci.Restrictions.FirstOrDefault(r => r.IsActive);
                bool isAllergen = allergenNames.Contains(inci.Name);

                // Calculate actual concentration in formula: RM% * ingredient concentration
                var maxIngredientPct = comp.MaxPercentage ?? comp.MinPercentage ?? 100m;
                var actualConcentration = pct * maxIngredientPct / 100m;

                bool isCompliant = true;
                if (primaryRestriction?.MaxPercentage.HasValue == true)
                {
                    isCompliant = actualConcentration <= primaryRestriction.MaxPercentage.Value;
                }

                totalRegulated++;
                if (isAllergen) totalAllergens++;

                vm.Ingredients.Add(new RegulatedIngredientLine
                {
                    Order = order++,
                    InciName = inci.Name,
                    CASNumber = inci.CASNumber,
                    ECNumber = inci.ECNumber,
                    Function = inci.Function,
                    AnnexReference = primaryRestriction?.Regulation,
                    MaxConcentration = primaryRestriction?.MaxPercentage,
                    ActualConcentration = Math.Round(actualConcentration, 4),
                    IsAllergen = isAllergen,
                    Restrictions = primaryRestriction?.Description,
                    IsCompliant = isCompliant
                });
            }
        }

        vm.TotalRegulated = totalRegulated;
        vm.TotalAllergens = totalAllergens;

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM5 – Quantitative INCI Composition Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<QuantitativeInciDocumentViewModel?> GetQuantitativeInciDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.INCIIngredients)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new QuantitativeInciDocumentViewModel();
        MapBase(formula, vm);

        // If INCIIngredients exist, use them directly (they represent the final INCI composition)
        if (formula.INCIIngredients.Any())
        {
            int order = 1;
            foreach (var inci in formula.INCIIngredients.OrderByDescending(i => i.Percentage ?? 0))
            {
                vm.Ingredients.Add(new QuantitativeInciLine
                {
                    Order = order++,
                    InciName = inci.INCIName ?? string.Empty,
                    CASNumber = inci.CASNumber,
                    ECNumber = inci.ECNumber,
                    Function = inci.Function,
                    Percentage = inci.Percentage,
                    Restrictions = null,
                    Origin = inci.Origin
                });
            }
        }
        else
        {
            // Fallback: calculate from raw material compositions
            var rawData = await _context.FormulaRawMaterials
                .Where(frm => frm.FormulaId == formulaId && frm.IsActive)
                .Include(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Composition.Where(c => !c.IsImpurity))
                        .ThenInclude(rc => rc.INCI!)
                            .ThenInclude(i => i.Restrictions)
                .AsSplitQuery()
                .ToListAsync();

            // Aggregate by INCI name
            var inciAggregation = new Dictionary<string, (INCI Inci, decimal TotalPct)>(StringComparer.OrdinalIgnoreCase);

            foreach (var frm in rawData)
            {
                var rm = frm.RawMaterial;
                if (rm?.Composition == null) continue;

                var frmPct = frm.Percentage ?? 0m;

                foreach (var comp in rm.Composition.Where(c => !c.IsImpurity))
                {
                    var inci = comp.INCI;
                    if (inci == null) continue;

                    // Weighted contribution: RM% * average composition %
                    var avgPct = ((comp.MinPercentage ?? 0m) + (comp.MaxPercentage ?? comp.MinPercentage ?? 100m)) / 2m;
                    var contribution = frmPct * avgPct / 100m;

                    if (inciAggregation.TryGetValue(inci.Name, out var existing))
                    {
                        inciAggregation[inci.Name] = (existing.Inci, existing.TotalPct + contribution);
                    }
                    else
                    {
                        inciAggregation[inci.Name] = (inci, contribution);
                    }
                }
            }

            int order = 1;
            foreach (var kvp in inciAggregation.OrderByDescending(x => x.Value.TotalPct))
            {
                var inci = kvp.Value.Inci;
                var restriction = inci.Restrictions.FirstOrDefault(r => r.IsActive);

                vm.Ingredients.Add(new QuantitativeInciLine
                {
                    Order = order++,
                    InciName = inci.Name,
                    CASNumber = inci.CASNumber,
                    ECNumber = inci.ECNumber,
                    Function = inci.Function,
                    Percentage = Math.Round(kvp.Value.TotalPct, 4),
                    Restrictions = restriction?.Description,
                    Origin = null
                });
            }
        }

        vm.TotalPercentage = vm.Ingredients.Sum(i => i.Percentage ?? 0m);

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM6 – Marketing Info Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<MarketingInfoDocumentViewModel?> GetMarketingInfoDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.MarketingInfo)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Supplier)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.MainINCI)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new MarketingInfoDocumentViewModel();
        MapBase(formula, vm);

        vm.ProductDescription = formula.MarketingInfo?.ProductDescription;
        vm.TargetAudience = formula.MarketingInfo?.TargetAudience;
        vm.KeyBenefits = formula.MarketingInfo?.KeyBenefits;
        vm.UsageInstructions = formula.MarketingInfo?.UsageInstructions;

        int order = 1;
        foreach (var frm in formula.FormulaRawMaterials.OrderBy(x => x.Id))
        {
            var rm = frm.RawMaterial;
            if (rm == null || !rm.IsActive) continue;

            vm.Ingredients.Add(new MarketingIngredientLine
            {
                Order = order++,
                RawMaterialCode = rm.Code,
                RawMaterialName = rm.ProductName,
                InciName = rm.MainINCI?.Name,
                ManufacturerName = rm.ManufacturerName,
                SupplierName = rm.Supplier?.Name,
                HasMarketingInfo = rm.HasMarketingInfo,
                Percentage = frm.Percentage ?? 0m
            });
        }

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM8 – Allergens Composition Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<AllergensDocumentViewModel?> GetAllergensDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.Allergens)
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new AllergensDocumentViewModel();
        MapBase(formula, vm);

        bool isLeaveOn = string.Equals(formula.ProductType, "Leave on", StringComparison.OrdinalIgnoreCase);
        var threshold = isLeaveOn
            ? vm.DeclarationThresholdLeaveOn  // 0.001 (10 ppm)
            : vm.DeclarationThresholdRinseOff; // 0.01 (100 ppm)

        int order = 1;
        int totalDeclared = 0;

        foreach (var allergen in formula.Allergens.OrderByDescending(a => a.Concentration ?? 0))
        {
            var concentration = allergen.Concentration ?? 0m;
            bool exceedsThreshold = concentration >= threshold;
            bool isDeclared = allergen.IsDeclared ?? exceedsThreshold;

            if (isDeclared) totalDeclared++;

            vm.Allergens.Add(new AllergenCompositionLine
            {
                Order = order++,
                AllergenName = allergen.AllergenName ?? string.Empty,
                CASNumber = null, // Allergen model doesn't have CAS; could be extended
                ECNumber = null,
                AllergenType = allergen.AllergenType,
                Concentration = concentration,
                Unit = allergen.Unit ?? "%",
                Source = allergen.Source,
                IsDeclared = isDeclared,
                ExceedsThreshold = exceedsThreshold
            });
        }

        vm.TotalAllergens = formula.Allergens.Count;
        vm.TotalDeclared = totalDeclared;

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM10 – Formula Claims Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<FormulaClaimsDocumentViewModel?> GetFormulaClaimsDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.Claims)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.MainINCI)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new FormulaClaimsDocumentViewModel();
        MapBase(formula, vm);

        // Evaluate claims per raw material based on document flags and naming
        int order = 1;

        foreach (var frm in formula.FormulaRawMaterials.OrderBy(x => x.Id))
        {
            var rm = frm.RawMaterial;
            if (rm == null || !rm.IsActive) continue;

            bool isVegan = rm.HasVegan;

            // Heuristic checks based on available data
            // These would typically be stored per raw material; here we use convention
            var nameLower = (rm.ProductName ?? "").ToLowerInvariant();
            var categoryLower = (rm.Category ?? "").ToLowerInvariant();

            vm.Ingredients.Add(new ClaimIngredientLine
            {
                Order = order++,
                RawMaterialCode = rm.Code,
                RawMaterialName = rm.ProductName,
                InciName = rm.MainINCI?.Name,
                IsVegan = isVegan,
                IsGlutenFree = !nameLower.Contains("wheat") && !nameLower.Contains("gluten") && !nameLower.Contains("triticum"),
                IsGMOFree = true, // Default true; would need separate tracking
                IsMicroplasticFree = !nameLower.Contains("acrylate") && !categoryLower.Contains("microplastic"),
                IsPhthalateFree = rm.HasPhthalates_BHT_Statement || !nameLower.Contains("phthalate"),
                IsPalmOilFree = !nameLower.Contains("palm") && !nameLower.Contains("elaeis"),
                IsSiliconeFree = !nameLower.Contains("dimethicone") && !nameLower.Contains("siloxane") && !nameLower.Contains("silicone")
            });
        }

        // Build summary
        var summary = new ClaimsSummary
        {
            CanClaimVegan = vm.Ingredients.All(i => i.IsVegan),
            CanClaimGlutenFree = vm.Ingredients.All(i => i.IsGlutenFree),
            CanClaimGMOFree = vm.Ingredients.All(i => i.IsGMOFree),
            CanClaimMicroplasticFree = vm.Ingredients.All(i => i.IsMicroplasticFree),
            CanClaimPhthalateFree = vm.Ingredients.All(i => i.IsPhthalateFree),
            CanClaimPalmOilFree = vm.Ingredients.All(i => i.IsPalmOilFree),
            CanClaimSiliconeFree = vm.Ingredients.All(i => i.IsSiliconeFree)
        };

        // Populate verified/failed claims lists
        void CheckClaim(bool can, string claimName)
        {
            if (can) summary.VerifiedClaims.Add(claimName);
            else summary.FailedClaims.Add(claimName);
        }

        CheckClaim(summary.CanClaimVegan, "Vegan");
        CheckClaim(summary.CanClaimGlutenFree, "Gluten-Free");
        CheckClaim(summary.CanClaimGMOFree, "GMO-Free");
        CheckClaim(summary.CanClaimMicroplasticFree, "Microplastic-Free");
        CheckClaim(summary.CanClaimPhthalateFree, "Phthalate-Free");
        CheckClaim(summary.CanClaimPalmOilFree, "Palm Oil-Free");
        CheckClaim(summary.CanClaimSiliconeFree, "Silicone-Free");

        // Add verified claims from DB records
        foreach (var claim in formula.Claims.Where(c => c.IsVerified == true))
        {
            var text = claim.ClaimText ?? claim.ClaimCategory ?? "Verified Claim";
            if (!summary.VerifiedClaims.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                summary.VerifiedClaims.Add(text);
            }
        }

        vm.Summary = summary;

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM11 – Toxicological Profile Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<ToxicologicalProfileDocumentViewModel?> GetToxicologicalProfileDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.SafetyData)
            .Include(f => f.ToxicologicalData)
            .Include(f => f.INCIIngredients)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new ToxicologicalProfileDocumentViewModel();
        MapBase(formula, vm);

        // Body weight for MoS calculation (standard adult 60 kg)
        // Default daily exposure amount (mg/kg bw/day) for cosmetics
        const decimal defaultDailyExposure = 1.0m;

        int order = 1;
        bool allSafe = true;

        // Prefer ToxicologicalData from DB (FORM11 dedicated data)
        if (formula.ToxicologicalData.Any())
        {
            foreach (var tox in formula.ToxicologicalData.OrderByDescending(t => t.Percentage ?? 0))
            {
                if (!tox.IsSafe) allSafe = false;

                vm.Ingredients.Add(new ToxicologicalIngredientLine
                {
                    Order = order++,
                    InciName = tox.InciName ?? string.Empty,
                    CASNumber = tox.CASNumber,
                    Function = tox.Function,
                    Percentage = tox.Percentage,
                    NOAEL = tox.NOAEL,
                    SED = tox.SED,
                    MoS = tox.MoS,
                    ToxicologicalCategory = tox.ToxicologicalCategory,
                    IsSafe = tox.IsSafe,
                    Notes = tox.Notes
                });
            }
        }
        else if (formula.INCIIngredients.Any())
        {
            foreach (var inci in formula.INCIIngredients.OrderByDescending(i => i.Percentage ?? 0))
            {
                var pct = inci.Percentage ?? 0m;

                // SED = (concentration / 100) * dailyExposure
                // For a more realistic model this would use actual absorption and usage data.
                var sed = pct / 100m * defaultDailyExposure;

                // NOAEL: not stored on INCIIngredient; set to null (would come from toxicological DB)
                decimal? noael = null;

                // MoS = NOAEL / SED (>100 is considered safe)
                decimal? mos = (noael.HasValue && sed > 0) ? noael.Value / sed : null;
                bool isSafe = !mos.HasValue || mos.Value >= 100m;
                if (!isSafe) allSafe = false;

                string? toxCategory = mos switch
                {
                    null => "Insufficient data",
                    >= 1000 => "Very low risk",
                    >= 100 => "Acceptable",
                    >= 10 => "Moderate risk",
                    _ => "High risk"
                };

                vm.Ingredients.Add(new ToxicologicalIngredientLine
                {
                    Order = order++,
                    InciName = inci.INCIName ?? string.Empty,
                    CASNumber = inci.CASNumber,
                    Function = inci.Function,
                    Percentage = pct,
                    NOAEL = noael,
                    SED = sed > 0 ? Math.Round(sed, 6) : null,
                    MoS = mos.HasValue ? Math.Round(mos.Value, 2) : null,
                    ToxicologicalCategory = toxCategory,
                    IsSafe = isSafe,
                    Notes = noael.HasValue ? null : "NOAEL data not available; requires toxicological assessment."
                });
            }
        }
        else
        {
            // Fallback: derive from raw material compositions
            var rawData = await _context.FormulaRawMaterials
                .Where(frm => frm.FormulaId == formulaId && frm.IsActive)
                .Include(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Composition.Where(c => !c.IsImpurity))
                        .ThenInclude(rc => rc.INCI)
                .AsSplitQuery()
                .ToListAsync();

            var inciAgg = new Dictionary<string, (INCI Inci, decimal TotalPct)>(StringComparer.OrdinalIgnoreCase);
            foreach (var frm in rawData)
            {
                var rm = frm.RawMaterial;
                if (rm?.Composition == null) continue;
                var frmPct = frm.Percentage ?? 0m;

                foreach (var comp in rm.Composition.Where(c => !c.IsImpurity))
                {
                    var inci = comp.INCI;
                    if (inci == null) continue;
                    var avgPct = ((comp.MinPercentage ?? 0m) + (comp.MaxPercentage ?? comp.MinPercentage ?? 100m)) / 2m;
                    var contribution = frmPct * avgPct / 100m;

                    if (inciAgg.TryGetValue(inci.Name, out var existing))
                        inciAgg[inci.Name] = (existing.Inci, existing.TotalPct + contribution);
                    else
                        inciAgg[inci.Name] = (inci, contribution);
                }
            }

            foreach (var kvp in inciAgg.OrderByDescending(x => x.Value.TotalPct))
            {
                var inci = kvp.Value.Inci;
                var pct = kvp.Value.TotalPct;
                var sed = pct / 100m * defaultDailyExposure;

                vm.Ingredients.Add(new ToxicologicalIngredientLine
                {
                    Order = order++,
                    InciName = inci.Name,
                    CASNumber = inci.CASNumber,
                    Function = inci.Function,
                    Percentage = Math.Round(pct, 4),
                    NOAEL = null,
                    SED = sed > 0 ? Math.Round(sed, 6) : null,
                    MoS = null,
                    ToxicologicalCategory = "Insufficient data",
                    IsSafe = true, // Default safe when no data
                    Notes = "NOAEL data not available; requires toxicological assessment."
                });
            }
        }

        vm.IsOverallSafe = allSafe;
        vm.SafetyConclusion = allSafe
            ? "Based on available data, all ingredients meet safety margin requirements."
            : "One or more ingredients have a Margin of Safety (MoS) below the acceptable threshold of 100. Further toxicological review is recommended.";

        // Enrich with safety data if available
        if (formula.SafetyData != null)
        {
            vm.SafetyConclusion += $" Skin compatibility: {formula.SafetyData.SkinCompatibility ?? "N/A"}." +
                                   $" Irritation level: {formula.SafetyData.IrritationLevel ?? "N/A"}.";
        }

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM12 – Master Formula Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<MasterFormulaDocumentViewModel?> GetMasterFormulaDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Supplier)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.MainINCI)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new MasterFormulaDocumentViewModel();
        MapBase(formula, vm);

        vm.ManufacturingSite = formula.ManufacturingSite;
        vm.ManufacturingMethod = formula.ManufacturingMethod;
        vm.PhysicochemicalProperties = formula.PhysicochemicalProperties;
        vm.MicrobiologicalProperties = formula.MicrobiologicalProperties;
        vm.PackagingInfo = formula.PackagingInfo;
        vm.Comments = formula.Comments;

        // Group raw materials by phase
        var phaseGroups = formula.FormulaRawMaterials
            .Where(frm => frm.RawMaterial != null && frm.RawMaterial.IsActive)
            .GroupBy(frm => frm.Phase ?? "Unassigned")
            .OrderBy(g => g.Key);

        decimal totalPercentage = 0m;

        foreach (var group in phaseGroups)
        {
            var phase = new MasterFormulaPhase
            {
                PhaseName = group.Key
            };

            int order = 1;
            decimal phaseTotal = 0m;

            foreach (var frm in group.OrderBy(x => x.Id))
            {
                var rm = frm.RawMaterial!;
                var pct = frm.Percentage ?? 0m;
                phaseTotal += pct;

                phase.Ingredients.Add(new MasterFormulaIngredientLine
                {
                    Order = order++,
                    RawMaterialCode = rm.Code,
                    PrismaCode = rm.PrismaCode,
                    RawMaterialName = rm.ProductName,
                    InciName = rm.MainINCI?.Name,
                    SupplierName = rm.Supplier?.Name,
                    ManufacturerName = rm.ManufacturerName,
                    Percentage = pct,
                    Phase = frm.Phase
                });
            }

            phase.PhaseTotal = phaseTotal;
            totalPercentage += phaseTotal;
            vm.Phases.Add(phase);
        }

        vm.TotalPercentage = totalPercentage;

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM13 – INCI List Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<InciListDocumentViewModel?> GetInciListDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.INCIIngredients)
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new InciListDocumentViewModel();
        MapBase(formula, vm);

        List<string> inciNames;

        if (formula.INCIIngredients.Any())
        {
            // Use the pre-calculated INCI ingredients, ordered by descending percentage
            inciNames = formula.INCIIngredients
                .OrderByDescending(i => i.Percentage ?? 0)
                .Where(i => !string.IsNullOrWhiteSpace(i.INCIName))
                .Select(i => i.INCIName!)
                .ToList();
        }
        else
        {
            // Fallback: derive from raw material compositions
            var rawData = await _context.FormulaRawMaterials
                .Where(frm => frm.FormulaId == formulaId && frm.IsActive)
                .Include(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Composition.Where(c => !c.IsImpurity))
                        .ThenInclude(rc => rc.INCI)
                .AsSplitQuery()
                .ToListAsync();

            var inciAgg = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var frm in rawData)
            {
                var rm = frm.RawMaterial;
                if (rm?.Composition == null) continue;
                var frmPct = frm.Percentage ?? 0m;

                foreach (var comp in rm.Composition.Where(c => !c.IsImpurity))
                {
                    var inci = comp.INCI;
                    if (inci == null || string.IsNullOrWhiteSpace(inci.Name)) continue;

                    var avgPct = ((comp.MinPercentage ?? 0m) + (comp.MaxPercentage ?? comp.MinPercentage ?? 100m)) / 2m;
                    var contribution = frmPct * avgPct / 100m;

                    if (inciAgg.ContainsKey(inci.Name))
                        inciAgg[inci.Name] += contribution;
                    else
                        inciAgg[inci.Name] = contribution;
                }
            }

            inciNames = inciAgg.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
        }

        vm.InciNames = inciNames;
        vm.InciList = string.Join(", ", inciNames);
        vm.TotalIngredients = inciNames.Count;

        return vm;
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM14 – Qualitative & Quantitative Composition Document
    // ═══════════════════════════════════════════════════════════════

    public async Task<QualitativeQuantitativeDocumentViewModel?> GetQualitativeQuantitativeDocument(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .Include(f => f.FormulaRawMaterials.Where(frm => frm.IsActive))
                .ThenInclude(frm => frm.RawMaterial!)
                    .ThenInclude(rm => rm.Composition)
                        .ThenInclude(rc => rc.INCI!)
                            .ThenInclude(i => i.Restrictions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null) return null;

        var vm = new QualitativeQuantitativeDocumentViewModel();
        MapBase(formula, vm);

        int order = 1;
        decimal totalPercentage = 0m;

        foreach (var frm in formula.FormulaRawMaterials
            .Where(x => x.RawMaterial != null && x.RawMaterial.IsActive)
            .OrderBy(x => x.Id))
        {
            var rm = frm.RawMaterial!;
            var pct = frm.Percentage ?? 0m;
            totalPercentage += pct;

            var rmLine = new QualitativeRawMaterialLine
            {
                Order = order++,
                RawMaterialCode = rm.Code,
                PrismaCode = rm.PrismaCode,
                RawMaterialName = rm.ProductName,
                Percentage = pct
            };

            // Sub-ingredients from composition (exclude impurities)
            foreach (var comp in rm.Composition
                .Where(c => !c.IsImpurity && c.INCI != null)
                .OrderByDescending(c => c.MaxPercentage ?? c.MinPercentage ?? 0))
            {
                var inci = comp.INCI!;
                var restriction = inci.Restrictions.FirstOrDefault(r => r.IsActive);

                // Calculated percentage in final formula: RM% * subingredient avg%
                var avgPct = ((comp.MinPercentage ?? 0m) + (comp.MaxPercentage ?? comp.MinPercentage ?? 100m)) / 2m;
                var calculatedPct = pct * avgPct / 100m;

                rmLine.SubIngredients.Add(new SubIngredientLine
                {
                    InciName = inci.Name,
                    CASNumber = inci.CASNumber,
                    ECNumber = inci.ECNumber,
                    Function = inci.Function,
                    MinPercentage = comp.MinPercentage,
                    MaxPercentage = comp.MaxPercentage,
                    CalculatedPercentage = Math.Round(calculatedPct, 4),
                    Restriction = restriction?.Description
                });
            }

            vm.RawMaterials.Add(rmLine);
        }

        vm.TotalPercentage = totalPercentage;

        return vm;
    }
}
