// Author: Damianos Pythagoras Ougkrenidis
// ML - Document Intelligence (Auto-extract data from TDS/MSDS PDFs)

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace INCIS_SaaP.ML.DocumentIntelligence;

/// <summary>
/// Extracted document data record
/// </summary>
public class ExtractedDocumentData
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Source document ID
    /// </summary>
    public int? DocumentId { get; set; }
    
    /// <summary>
    /// Document type: "TDS", "MSDS", "ISO16128", "AllergenCertificate"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Associated raw material ID
    /// </summary>
    public int? RawMaterialId { get; set; }
    
    /// <summary>
    /// Extracted structured data (JSON)
    /// </summary>
    public string ExtractedDataJson { get; set; } = "{}";
    
    /// <summary>
    /// Extraction confidence (0-1)
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Whether extraction has been manually verified
    /// </summary>
    public bool IsVerified { get; set; }
    
    [MaxLength(100)]
    public string? VerifiedBy { get; set; }
    
    /// <summary>
    /// Raw extracted text
    /// </summary>
    public string? RawText { get; set; }
    
    /// <summary>
    /// Extraction method: "OCR", "PDFParse", "Manual"
    /// </summary>
    [MaxLength(20)]
    public string ExtractionMethod { get; set; } = "PDFParse";
    
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// TDS (Technical Data Sheet) extracted fields
/// </summary>
public class TDSExtractedData
{
    public string? ProductName { get; set; }
    public string? Manufacturer { get; set; }
    public string? INCIName { get; set; }
    public string? Description { get; set; }
    public string? Appearance { get; set; }
    public string? Color { get; set; }
    public string? Odor { get; set; }
    public decimal? pH { get; set; }
    public decimal? Density { get; set; }
    public string? Viscosity { get; set; }
    public string? Solubility { get; set; }
    public string? ShelfLife { get; set; }
    public string? StorageConditions { get; set; }
    public string? RecommendedUsageLevel { get; set; }
    public List<string>? Applications { get; set; }
    public List<string>? Compatibilities { get; set; }
    public List<string>? Incompatibilities { get; set; }
}

/// <summary>
/// MSDS (Material Safety Data Sheet) extracted fields
/// </summary>
public class MSDSExtractedData
{
    public string? ProductName { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerAddress { get; set; }
    public string? ManufacturerPhone { get; set; }
    public string? EmergencyPhone { get; set; }
    public List<HazardStatement>? HazardStatements { get; set; }
    public List<PrecautionaryStatement>? PrecautionaryStatements { get; set; }
    public string? StorageRequirements { get; set; }
    public string? DisposalMethods { get; set; }
    public decimal? FlashPoint { get; set; }
    public string? FirstAidMeasures { get; set; }
    public List<CompositionEntry>? Composition { get; set; }
}

public class HazardStatement
{
    public string Code { get; set; } = string.Empty; // e.g., "H302"
    public string Description { get; set; } = string.Empty;
}

public class PrecautionaryStatement
{
    public string Code { get; set; } = string.Empty; // e.g., "P264"
    public string Description { get; set; } = string.Empty;
}

public class CompositionEntry
{
    public string SubstanceName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public decimal? MinPercentage { get; set; }
    public decimal? MaxPercentage { get; set; }
}

/// <summary>
/// Document Intelligence Service interface
/// </summary>
public interface IDocumentIntelligenceService
{
    /// <summary>
    /// Extracts structured data from a TDS PDF
    /// </summary>
    Task<ExtractedDocumentData> ExtractTDSAsync(string filePath, int? rawMaterialId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extracts structured data from an MSDS PDF
    /// </summary>
    Task<ExtractedDocumentData> ExtractMSDSAsync(string filePath, int? rawMaterialId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Auto-populates raw material fields from extracted document data
    /// </summary>
    Task<AutoPopulateResult> AutoPopulateRawMaterialAsync(int rawMaterialId, int extractedDataId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extracts INCI composition from a composition document
    /// </summary>
    Task<List<CompositionEntry>> ExtractCompositionAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifies consistency between different documents for same raw material
    /// </summary>
    Task<DocumentConsistencyReport> CheckConsistencyAsync(int rawMaterialId, CancellationToken cancellationToken = default);
}

public class AutoPopulateResult
{
    public bool IsSuccess { get; init; }
    public int FieldsPopulated { get; init; }
    public List<string> PopulatedFields { get; init; } = new();
    public List<string> SkippedFields { get; init; } = new();
    public double OverallConfidence { get; init; }
}

public class DocumentConsistencyReport
{
    public int RawMaterialId { get; init; }
    public bool IsConsistent { get; init; }
    public List<ConsistencyIssue> Issues { get; init; } = new();
}

public class ConsistencyIssue
{
    public string FieldName { get; init; } = string.Empty;
    public string Document1 { get; init; } = string.Empty;
    public string Value1 { get; init; } = string.Empty;
    public string Document2 { get; init; } = string.Empty;
    public string Value2 { get; init; } = string.Empty;
    public string Severity { get; init; } = "Warning";
}

/// <summary>
/// Document Intelligence Service implementation
/// Uses regex-based extraction (can be upgraded to Azure Document Intelligence/LayoutLM)
/// </summary>
public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    // Common patterns for extracting data from documents
    private static readonly Regex PHPattern = new(@"pH\s*[:=]?\s*(\d+\.?\d*)\s*[-–]?\s*(\d+\.?\d*)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DensityPattern = new(@"[Dd]ensity\s*[:=]?\s*(\d+\.?\d*)\s*[-–]?\s*(\d+\.?\d*)?\s*(g/cm|kg/L|g/ml)?", RegexOptions.Compiled);
    private static readonly Regex ShelfLifePattern = new(@"[Ss]helf\s*[Ll]ife\s*[:=]?\s*(\d+)\s*(months?|years?)", RegexOptions.Compiled);
    private static readonly Regex CASPattern = new(@"(\d{2,7}-\d{2}-\d)", RegexOptions.Compiled);
    private static readonly Regex PercentagePattern = new(@"(\d+\.?\d*)\s*[-–]\s*(\d+\.?\d*)\s*%", RegexOptions.Compiled);
    private static readonly Regex HStatementPattern = new(@"(H\d{3})\s*[:=]?\s*(.+?)(?=H\d{3}|P\d{3}|$)", RegexOptions.Compiled);

    public DocumentIntelligenceService(IServiceProvider serviceProvider, ILogger<DocumentIntelligenceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<ExtractedDocumentData> ExtractTDSAsync(string filePath, int? rawMaterialId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting TDS data from: {FilePath}", filePath);

        // In production, would use OCR/PDF parsing library
        var rawText = await ExtractTextFromFileAsync(filePath, cancellationToken);
        
        var tdsData = new TDSExtractedData();
        double confidence = 0;
        int fieldsExtracted = 0;

        // Extract pH
        var phMatch = PHPattern.Match(rawText);
        if (phMatch.Success)
        {
            tdsData.pH = decimal.Parse(phMatch.Groups[1].Value);
            fieldsExtracted++;
        }

        // Extract density
        var densityMatch = DensityPattern.Match(rawText);
        if (densityMatch.Success)
        {
            tdsData.Density = decimal.Parse(densityMatch.Groups[1].Value);
            fieldsExtracted++;
        }

        // Extract shelf life
        var shelfMatch = ShelfLifePattern.Match(rawText);
        if (shelfMatch.Success)
        {
            var value = int.Parse(shelfMatch.Groups[1].Value);
            var unit = shelfMatch.Groups[2].Value.ToLower();
            tdsData.ShelfLife = unit.StartsWith("year") ? $"{value} years" : $"{value} months";
            fieldsExtracted++;
        }

        confidence = fieldsExtracted > 0 ? Math.Min(0.9, 0.3 + fieldsExtracted * 0.1) : 0.1;

        var extractedData = new ExtractedDocumentData
        {
            DocumentType = "TDS",
            RawMaterialId = rawMaterialId,
            ExtractedDataJson = JsonSerializer.Serialize(tdsData),
            Confidence = confidence,
            RawText = rawText.Length > 10000 ? rawText[..10000] : rawText,
            ExtractionMethod = "PDFParse"
        };

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();
        dbContext.Set<ExtractedDocumentData>().Add(extractedData);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("TDS extraction complete: {FieldCount} fields extracted (confidence: {Confidence:P0})", 
            fieldsExtracted, confidence);

        return extractedData;
    }

    public async Task<ExtractedDocumentData> ExtractMSDSAsync(string filePath, int? rawMaterialId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting MSDS data from: {FilePath}", filePath);

        var rawText = await ExtractTextFromFileAsync(filePath, cancellationToken);
        var msdsData = new MSDSExtractedData();
        int fieldsExtracted = 0;

        // Extract H statements
        var hStatements = HStatementPattern.Matches(rawText);
        if (hStatements.Any())
        {
            msdsData.HazardStatements = hStatements.Select(m => new HazardStatement
            {
                Code = m.Groups[1].Value,
                Description = m.Groups[2].Value.Trim()
            }).ToList();
            fieldsExtracted++;
        }

        // Extract CAS numbers and composition
        var casMatches = CASPattern.Matches(rawText);
        if (casMatches.Any())
        {
            msdsData.Composition = casMatches.Select(m => new CompositionEntry
            {
                CASNumber = m.Groups[1].Value
            }).DistinctBy(c => c.CASNumber).ToList();
            fieldsExtracted++;
        }

        var confidence = fieldsExtracted > 0 ? Math.Min(0.85, 0.3 + fieldsExtracted * 0.1) : 0.1;

        var extractedData = new ExtractedDocumentData
        {
            DocumentType = "MSDS",
            RawMaterialId = rawMaterialId,
            ExtractedDataJson = JsonSerializer.Serialize(msdsData),
            Confidence = confidence,
            RawText = rawText.Length > 10000 ? rawText[..10000] : rawText,
            ExtractionMethod = "PDFParse"
        };

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();
        dbContext.Set<ExtractedDocumentData>().Add(extractedData);
        await dbContext.SaveChangesAsync(cancellationToken);

        return extractedData;
    }

    public async Task<AutoPopulateResult> AutoPopulateRawMaterialAsync(int rawMaterialId, int extractedDataId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var rm = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(dbContext.Set<Models.RawMaterial>().Where(r => r.Id == rawMaterialId), cancellationToken);
        
        var extracted = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(dbContext.Set<ExtractedDocumentData>().Where(e => e.Id == extractedDataId), cancellationToken);

        if (rm == null || extracted == null)
            return new AutoPopulateResult { IsSuccess = false };

        var populated = new List<string>();
        var skipped = new List<string>();

        if (extracted.DocumentType == "TDS")
        {
            var tds = JsonSerializer.Deserialize<TDSExtractedData>(extracted.ExtractedDataJson);
            if (tds != null)
            {
                if (!string.IsNullOrEmpty(tds.ShelfLife) && string.IsNullOrEmpty(rm.ShelfLife))
                { rm.ShelfLife = tds.ShelfLife; populated.Add("ShelfLife"); }
                else if (!string.IsNullOrEmpty(tds.ShelfLife)) skipped.Add("ShelfLife (already set)");

                if (tds.Density.HasValue && !rm.Density.HasValue)
                { rm.Density = tds.Density; populated.Add("Density"); }

                if (!string.IsNullOrEmpty(tds.Color) && string.IsNullOrEmpty(rm.Color))
                { rm.Color = tds.Color; populated.Add("Color"); }

                if (!string.IsNullOrEmpty(tds.StorageConditions) && string.IsNullOrEmpty(rm.StorageConditions))
                { rm.StorageConditions = tds.StorageConditions; populated.Add("StorageConditions"); }

                if (!string.IsNullOrEmpty(tds.Solubility) && string.IsNullOrEmpty(rm.Solubility))
                { rm.Solubility = tds.Solubility; populated.Add("Solubility"); }

                if (!string.IsNullOrEmpty(tds.Viscosity) && string.IsNullOrEmpty(rm.Viscosity))
                { rm.Viscosity = tds.Viscosity; populated.Add("Viscosity"); }
            }
        }
        else if (extracted.DocumentType == "MSDS")
        {
            var msds = JsonSerializer.Deserialize<MSDSExtractedData>(extracted.ExtractedDataJson);
            if (msds != null)
            {
                if (!string.IsNullOrEmpty(msds.ManufacturerAddress) && string.IsNullOrEmpty(rm.ManufacturerAddress))
                { rm.ManufacturerAddress = msds.ManufacturerAddress; populated.Add("ManufacturerAddress"); }

                if (!string.IsNullOrEmpty(msds.ManufacturerPhone) && string.IsNullOrEmpty(rm.ManufacturerPhone))
                { rm.ManufacturerPhone = msds.ManufacturerPhone; populated.Add("ManufacturerPhone"); }

                if (!string.IsNullOrEmpty(msds.StorageRequirements) && string.IsNullOrEmpty(rm.StorageConditions))
                { rm.StorageConditions = msds.StorageRequirements; populated.Add("StorageConditions"); }
            }
        }

        if (populated.Any())
            await dbContext.SaveChangesAsync(cancellationToken);

        return new AutoPopulateResult
        {
            IsSuccess = populated.Any(),
            FieldsPopulated = populated.Count,
            PopulatedFields = populated,
            SkippedFields = skipped,
            OverallConfidence = extracted.Confidence
        };
    }

    public async Task<List<CompositionEntry>> ExtractCompositionAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rawText = await ExtractTextFromFileAsync(filePath, cancellationToken);
        var entries = new List<CompositionEntry>();

        // Look for patterns like "INCI NAME    10 - 30 %"
        var lines = rawText.Split('\n');
        foreach (var line in lines)
        {
            var pctMatch = PercentagePattern.Match(line);
            if (pctMatch.Success)
            {
                var substanceName = line[..pctMatch.Index].Trim();
                var casMatch = CASPattern.Match(line);

                entries.Add(new CompositionEntry
                {
                    SubstanceName = substanceName,
                    CASNumber = casMatch.Success ? casMatch.Groups[1].Value : null,
                    MinPercentage = decimal.Parse(pctMatch.Groups[1].Value),
                    MaxPercentage = decimal.Parse(pctMatch.Groups[2].Value)
                });
            }
        }

        return entries;
    }

    public async Task<DocumentConsistencyReport> CheckConsistencyAsync(int rawMaterialId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var documents = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<ExtractedDocumentData>()
                    .Where(d => d.RawMaterialId == rawMaterialId && d.IsVerified),
                cancellationToken);

        var issues = new List<ConsistencyIssue>();

        // Compare TDS vs MSDS for common fields
        var tds = documents.FirstOrDefault(d => d.DocumentType == "TDS");
        var msds = documents.FirstOrDefault(d => d.DocumentType == "MSDS");

        if (tds != null && msds != null)
        {
            var tdsData = JsonSerializer.Deserialize<TDSExtractedData>(tds.ExtractedDataJson);
            var msdsData = JsonSerializer.Deserialize<MSDSExtractedData>(msds.ExtractedDataJson);

            if (tdsData?.ProductName != null && msdsData?.ProductName != null 
                && tdsData.ProductName != msdsData.ProductName)
            {
                issues.Add(new ConsistencyIssue
                {
                    FieldName = "ProductName",
                    Document1 = "TDS",
                    Value1 = tdsData.ProductName,
                    Document2 = "MSDS",
                    Value2 = msdsData.ProductName,
                    Severity = "Warning"
                });
            }
        }

        return new DocumentConsistencyReport
        {
            RawMaterialId = rawMaterialId,
            IsConsistent = !issues.Any(),
            Issues = issues
        };
    }

    private static Task<string> ExtractTextFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        // In production, would use:
        // - QuestPDF for PDF reading
        // - Tesseract OCR for scanned documents
        // - Azure Document Intelligence for complex layouts
        
        // For now, try to read as text if it's a text-based file
        if (File.Exists(filePath))
        {
            return File.ReadAllTextAsync(filePath, cancellationToken);
        }

        return Task.FromResult(string.Empty);
    }
}

