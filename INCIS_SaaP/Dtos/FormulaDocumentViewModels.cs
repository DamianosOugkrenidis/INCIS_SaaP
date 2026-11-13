// Author: Damianos Pythagoras Ougkrenidis
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Dtos;

/// <summary>
/// Base ViewModel for all formula documents
/// </summary>
public class FormulaDocumentBaseViewModel
{
    public int FormulaId { get; set; }
    public string FormulaCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductType { get; set; }
    public string? ClientName { get; set; }
    public string? PPIdCode { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? Version { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// FORM1 - Formula Price Document
/// </summary>
public class FormulaPriceDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<PriceLineItem> Ingredients { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public List<AllergenLineItem> Allergens { get; set; } = new();
}

public class PriceLineItem
{
    public int Order { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string RawMaterialName { get; set; } = string.Empty;
    public string? InciName { get; set; }
    public string? SupplierName { get; set; }
    public decimal Percentage { get; set; }
    public decimal RMPrice { get; set; }
    public decimal ContributionPrice { get; set; }
}

public class AllergenLineItem
{
    public string InciName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? ECNumber { get; set; }
    public string? Restriction { get; set; }
}

/// <summary>
/// FORM2 - Natural Formula Document
/// </summary>
public class NaturalFormulaDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<NaturalIngredientLine> Ingredients { get; set; } = new();
    public decimal TotalNaturalPercentage { get; set; }
    public decimal TotalOrganicPercentage { get; set; }
    public decimal TotalBiodegradability { get; set; }
    public string? CertificationType { get; set; }
    public string? CertifyingBody { get; set; }
}

public class NaturalIngredientLine
{
    public int Order { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string RawMaterialName { get; set; } = string.Empty;
    public string? InciName { get; set; }
    public decimal Percentage { get; set; }
    public decimal? NaturalOriginPercentage { get; set; }
    public decimal? OrganicPercentage { get; set; }
    public string? Origin { get; set; }
    public bool IsBiodegradable { get; set; }
}

/// <summary>
/// FORM3 - Heavy Metals / Impurities Document
/// </summary>
public class HeavyMetalsDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<ImpurityLineItem> RawMaterialImpurities { get; set; } = new();
    public List<HeavyMetalAnalysisItem> HeavyMetalAnalysis { get; set; } = new();
    public bool IsOverallCompliant { get; set; }
    public string? ComplianceSummary { get; set; }
}

public class ImpurityLineItem
{
    public int Order { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string RawMaterialName { get; set; } = string.Empty;
    public string? InciName { get; set; }
    public decimal Percentage { get; set; }
    public string? ImpuritySpecs { get; set; }
    public bool HasRMI { get; set; }
}

public class HeavyMetalAnalysisItem
{
    public string MetalName { get; set; } = string.Empty;
    public decimal? LimitValue { get; set; }
    public decimal? DetectedValue { get; set; }
    public string Unit { get; set; } = "ppm";
    public string? TestMethod { get; set; }
    public bool IsCompliant { get; set; }
    public string? Regulation { get; set; }
}

/// <summary>
/// FORM4 - Identification of Regulated Ingredients Document
/// </summary>
public class RegulatedIngredientsDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<RegulatedIngredientLine> Ingredients { get; set; } = new();
    public int TotalRegulated { get; set; }
    public int TotalAllergens { get; set; }
}

public class RegulatedIngredientLine
{
    public int Order { get; set; }
    public string InciName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? ECNumber { get; set; }
    public string? Function { get; set; }
    public string? AnnexReference { get; set; }
    public decimal? MaxConcentration { get; set; }
    public decimal? ActualConcentration { get; set; }
    public bool IsAllergen { get; set; }
    public string? Restrictions { get; set; }
    public bool IsCompliant { get; set; }
}

/// <summary>
/// FORM5 - Quantitative INCI Composition Document
/// </summary>
public class QuantitativeInciDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<QuantitativeInciLine> Ingredients { get; set; } = new();
    public decimal TotalPercentage { get; set; }
}

public class QuantitativeInciLine
{
    public int Order { get; set; }
    public string InciName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? ECNumber { get; set; }
    public string? Function { get; set; }
    public decimal? Percentage { get; set; }
    public string? Restrictions { get; set; }
    public string? Origin { get; set; }
}

/// <summary>
/// FORM6 - Marketing Info Document
/// </summary>
public class MarketingInfoDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<MarketingIngredientLine> Ingredients { get; set; } = new();
    public string? ProductDescription { get; set; }
    public string? TargetAudience { get; set; }
    public string? KeyBenefits { get; set; }
    public string? UsageInstructions { get; set; }
}

public class MarketingIngredientLine
{
    public int Order { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string RawMaterialName { get; set; } = string.Empty;
    public string? InciName { get; set; }
    public string? ManufacturerName { get; set; }
    public string? SupplierName { get; set; }
    public bool HasMarketingInfo { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// FORM8 - Allergens Composition Document
/// </summary>
public class AllergensDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<AllergenCompositionLine> Allergens { get; set; } = new();
    public int TotalDeclared { get; set; }
    public int TotalAllergens { get; set; }
    public decimal DeclarationThresholdLeaveOn { get; set; } = 0.001m;  // 10 ppm
    public decimal DeclarationThresholdRinseOff { get; set; } = 0.01m; // 100 ppm
}

public class AllergenCompositionLine
{
    public int Order { get; set; }
    public string AllergenName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? ECNumber { get; set; }
    public string? AllergenType { get; set; }
    public decimal? Concentration { get; set; }
    public string? Unit { get; set; }
    public string? Source { get; set; }
    public bool IsDeclared { get; set; }
    public bool ExceedsThreshold { get; set; }
}

/// <summary>
/// FORM10 - Formula Claims Document
/// </summary>
public class FormulaClaimsDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<ClaimIngredientLine> Ingredients { get; set; } = new();
    public ClaimsSummary Summary { get; set; } = new();
}

public class ClaimIngredientLine
{
    public int Order { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string? RawMaterialName { get; set; } = string.Empty;
    public string? InciName { get; set; }
    public bool IsVegan { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsGMOFree { get; set; }
    public bool IsMicroplasticFree { get; set; }
    public bool IsPhthalateFree { get; set; }
    public bool IsPalmOilFree { get; set; }
    public bool IsSiliconeFree { get; set; }
}

public class ClaimsSummary
{
    public bool CanClaimVegan { get; set; }
    public bool CanClaimGlutenFree { get; set; }
    public bool CanClaimGMOFree { get; set; }
    public bool CanClaimMicroplasticFree { get; set; }
    public bool CanClaimPhthalateFree { get; set; }
    public bool CanClaimPalmOilFree { get; set; }
    public bool CanClaimSiliconeFree { get; set; }
    public List<string> VerifiedClaims { get; set; } = new();
    public List<string> FailedClaims { get; set; } = new();
}

/// <summary>
/// FORM11 - Summarized Toxicological Profile Document
/// </summary>
public class ToxicologicalProfileDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<ToxicologicalIngredientLine> Ingredients { get; set; } = new();
    public bool IsOverallSafe { get; set; }
    public string? SafetyConclusion { get; set; }
}

public class ToxicologicalIngredientLine
{
    public int Order { get; set; }
    public string InciName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? Function { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? NOAEL { get; set; }  // mg/kg bw/day
    public decimal? SED { get; set; }    // mg/kg bw/day (Systemic Exposure Dose)
    public decimal? MoS { get; set; }    // Margin of Safety
    public string? ToxicologicalCategory { get; set; }
    public bool IsSafe { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// FORM12 - Master Formula Document
/// </summary>
public class MasterFormulaDocumentViewModel : FormulaDocumentBaseViewModel
{
    public string? ManufacturingSite { get; set; }
    public string? ManufacturingMethod { get; set; }
    public string? PhysicochemicalProperties { get; set; }
    public string? MicrobiologicalProperties { get; set; }
    public string? PackagingInfo { get; set; }
    public string? Comments { get; set; }
    public List<MasterFormulaPhase> Phases { get; set; } = new();
    public decimal TotalPercentage { get; set; }
}

public class MasterFormulaPhase
{
    public string PhaseName { get; set; } = string.Empty;
    public List<MasterFormulaIngredientLine> Ingredients { get; set; } = new();
    public decimal PhaseTotal { get; set; }
}

public class MasterFormulaIngredientLine
{
    public int Order { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string? PrismaCode { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? InciName { get; set; }
    public string? SupplierName { get; set; }
    public string? ManufacturerName { get; set; }
    public decimal Percentage { get; set; }
    public string? Phase { get; set; }
}

/// <summary>
/// FORM13 - INCI List Document
/// </summary>
public class InciListDocumentViewModel : FormulaDocumentBaseViewModel
{
    public string InciList { get; set; } = string.Empty;
    public List<string> InciNames { get; set; } = new();
    public int TotalIngredients { get; set; }
}

/// <summary>
/// FORM14 - Qualitative &amp; Quantitative Composition Document
/// </summary>
public class QualitativeQuantitativeDocumentViewModel : FormulaDocumentBaseViewModel
{
    public List<QualitativeRawMaterialLine> RawMaterials { get; set; } = new();
    public decimal TotalPercentage { get; set; }
}

public class QualitativeRawMaterialLine
{
    public int Order { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string? PrismaCode { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public List<SubIngredientLine> SubIngredients { get; set; } = new();
}

public class SubIngredientLine
{
    public string InciName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? ECNumber { get; set; }
    public string? Function { get; set; }
    public decimal? MinPercentage { get; set; }
    public decimal? MaxPercentage { get; set; }
    public decimal? CalculatedPercentage { get; set; }
    public string? Restriction { get; set; }
}

/// <summary>
/// Documents Index ViewModel - shows all available documents for a formula
/// </summary>
public class FormulaDocumentsIndexViewModel
{
    public int FormulaId { get; set; }
    public string FormulaCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductType { get; set; }
    public string? ClientName { get; set; }
    public List<DocumentInfo> AvailableDocuments { get; set; } = new();
}

public class DocumentInfo
{
    public string FormNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
    public bool HasData { get; set; }
}
