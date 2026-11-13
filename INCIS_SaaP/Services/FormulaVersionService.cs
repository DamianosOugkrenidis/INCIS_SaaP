// Author: Damianos Pythagoras Ougkrenidis
//
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για version control φορμούλων
/// </summary>
public class FormulaVersionService
{
    private readonly INCISDbContext _context;
    private readonly ILogger<FormulaVersionService> _logger;

    public FormulaVersionService(INCISDbContext context, ILogger<FormulaVersionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Δημιουργία νέας έκδοσης φόρμουλας
    /// </summary>
    public async Task<FormulaVersion> CreateVersionAsync(int formulaId, string changeDescription, 
        string changeType, string changeReason, string userId)
    {
        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials).ThenInclude(frm => frm.RawMaterial)
            .Include(f => f.INCIIngredients)
            .Include(f => f.SafetyData)
            .Include(f => f.PriceInfo)
            .Include(f => f.Allergens)
            .Include(f => f.HeavyMetals)
            .Include(f => f.MarketingInfo)
            .Include(f => f.NaturalCertification)
            .Include(f => f.Claims)
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null)
            throw new ArgumentException($"Formula with ID {formulaId} not found");

        // Serialize την τρέχουσα κατάσταση της φόρμουλας
        var formulaSnapshot = SerializeFormulaSnapshot(formula);
        var checksum = CalculateChecksum(formulaSnapshot);

        // Βρες τον επόμενο version number
        var lastVersion = await _context.Set<FormulaVersion>()
            .Where(fv => fv.FormulaId == formulaId)
            .OrderByDescending(fv => fv.VersionNumber)
            .FirstOrDefaultAsync();

        int nextVersionNumber = (lastVersion?.VersionNumber ?? 0) + 1;

        // Δημιούργησε την νέα έκδοση
        var version = new FormulaVersion
        {
            FormulaId = formulaId,
            VersionNumber = nextVersionNumber,
            ChangeDescription = changeDescription,
            ChangeType = changeType,
            ChangeReason = changeReason,
            VersionData = formulaSnapshot,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            IsCurrentVersion = true,
            Checksum = checksum,
            SnapshotSize = Encoding.UTF8.GetByteCount(formulaSnapshot),
            ApprovalStatus = "Pending"
        };

        // Ενημέρωσε τις παλιές εκδόσεις να μην είναι current
        var oldVersions = await _context.Set<FormulaVersion>()
            .Where(fv => fv.FormulaId == formulaId && fv.IsCurrentVersion)
            .ToListAsync();

        foreach (var oldVersion in oldVersions)
        {
            oldVersion.IsCurrentVersion = false;
        }

        _context.Set<FormulaVersion>().Add(version);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created version {nextVersionNumber} for formula {formulaId} by user {userId}");

        return version;
    }

    /// <summary>
    /// Λήψη όλων των εκδόσεων μιας φόρμουλας
    /// </summary>
    public async Task<List<FormulaVersion>> GetVersionHistoryAsync(int formulaId)
    {
        return await _context.Set<FormulaVersion>()
            .Where(fv => fv.FormulaId == formulaId)
            .OrderByDescending(fv => fv.VersionNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Λήψη συγκεκριμένης έκδοσης
    /// </summary>
    public async Task<FormulaVersion?> GetVersionAsync(int versionId)
    {
        return await _context.Set<FormulaVersion>()
            .Include(fv => fv.Formula)
            .FirstOrDefaultAsync(fv => fv.Id == versionId);
    }

    /// <summary>
    /// Σύγκριση δύο εκδόσεων
    /// </summary>
    public async Task<FormulaVersionComparison> CompareVersionsAsync(int sourceVersionId, int targetVersionId, string userId)
    {
        var sourceVersion = await GetVersionAsync(sourceVersionId);
        var targetVersion = await GetVersionAsync(targetVersionId);

        if (sourceVersion == null || targetVersion == null)
            throw new ArgumentException("One or both versions not found");

        if (sourceVersion.FormulaId != targetVersion.FormulaId)
            throw new ArgumentException("Versions must belong to the same formula");

        // Deserialize τα snapshots και σύγκρινε τα top-level πεδία/συλλογές
        var sourceDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sourceVersion.VersionData) ?? new();
        var targetDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(targetVersion.VersionData) ?? new();

        var diffs = new List<(string Field, string? Source, string? Target)>();
        foreach (var key in sourceDictionary.Keys.Union(targetDictionary.Keys).OrderBy(k => k, StringComparer.Ordinal))
        {
            sourceDictionary.TryGetValue(key, out var sv);
            targetDictionary.TryGetValue(key, out var tv);
            var sJson = sv.ValueKind == JsonValueKind.Undefined ? null : sv.GetRawText();
            var tJson = tv.ValueKind == JsonValueKind.Undefined ? null : tv.GetRawText();
            if (!string.Equals(sJson, tJson, StringComparison.Ordinal))
                diffs.Add((key, sJson, tJson));
        }

        var differences = diffs.Select(d => new { Field = d.Field, Source = d.Source, Target = d.Target }).ToList();

        var comparison = new FormulaVersionComparison
        {
            FormulaId = sourceVersion.FormulaId,
            SourceVersionId = sourceVersionId,
            TargetVersionId = targetVersionId,
            DifferencesJson = JsonSerializer.Serialize(differences),
            Summary = diffs.Count == 0
                ? $"No differences between v{sourceVersion.VersionNumber} and v{targetVersion.VersionNumber}"
                : $"{diffs.Count} difference(s) between v{sourceVersion.VersionNumber} and v{targetVersion.VersionNumber}",
            ComparedAt = DateTime.UtcNow,
            ComparedBy = userId
        };

        // Detailed rows (for UI/reporting)
        foreach (var d in diffs)
        {
            _context.Set<FormulaVersionDetail>().Add(new FormulaVersionDetail
            {
                FormulaVersionId = targetVersionId,
                DetailType = "Field",
                FieldName = d.Field,
                OldValue = d.Source,
                NewValue = d.Target,
                ChangeAction = "Modified"
            });
        }

        _context.Set<FormulaVersionComparison>().Add(comparison);
        await _context.SaveChangesAsync();

        return comparison;
    }

    /// <summary>
    /// Επαναφορά φόρμουλας σε προηγούμενη έκδοση
    /// </summary>
    public async Task<bool> RestoreVersionAsync(int formulaId, int versionId, string userId)
    {
        var version = await GetVersionAsync(versionId);
        if (version == null || version.FormulaId != formulaId)
            return false;

        var formula = await _context.Formulas.FindAsync(formulaId);
        if (formula == null)
            return false;

        // Deserialize το snapshot και εφάρμοσε το στην φόρμουλα
        var root = JsonSerializer.Deserialize<JsonElement>(version.VersionData);

        string? S(string name) => root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
        int? N(string name) => root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v) ? v : null;
        bool? B(string name) => root.TryGetProperty(name, out var e) && (e.ValueKind == JsonValueKind.True || e.ValueKind == JsonValueKind.False) ? e.GetBoolean() : null;
        DateTime? D(string name) => root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String
            && DateTime.TryParse(e.GetString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;

        // Scalar fields
        formula.FormulaCode = S("FormulaCode") ?? formula.FormulaCode;
        formula.ProductName = S("ProductName");
        formula.FormulaName = S("FormulaName");
        formula.Version = S("Version");
        formula.Description = S("Description");
        formula.ProductType = S("ProductType");
        formula.ManufacturingSite = S("ManufacturingSite");
        formula.ManufacturingMethod = S("ManufacturingMethod");
        formula.PPIdCode = S("PPIdCode");
        formula.PackagingInfo = S("PackagingInfo");
        formula.PhysicochemicalProperties = S("PhysicochemicalProperties");
        formula.MicrobiologicalProperties = S("MicrobiologicalProperties");
        formula.Comments = S("Comments");
        if (N("ClientId") is { } clientId) formula.ClientId = clientId;
        if (N("TestNumber") is { } testNumber) formula.TestNumber = testNumber;
        if (B("IsExternalDevelopment") is { } ext) formula.IsExternalDevelopment = ext;
        if (B("IsWIP") is { } wip) formula.IsWIP = wip;
        if (D("IssueDate") is { } issue) formula.IssueDate = issue;
        if (D("CreationDate") is { } created) formula.CreationDate = created;
        if (D("ValidationDate") is { } validated) formula.ValidationDate = validated;
        formula.LastModified = DateTime.UtcNow;

        // Composition: raw materials
        var existingRms = await _context.FormulaRawMaterials.Where(x => x.FormulaId == formulaId).ToListAsync();
        _context.FormulaRawMaterials.RemoveRange(existingRms);
        if (root.TryGetProperty("RawMaterials", out var rms) && rms.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in rms.EnumerateArray())
            {
                if (!r.TryGetProperty("RawMaterialId", out var rmIdEl) || !rmIdEl.TryGetInt32(out var rmId) || rmId <= 0) continue;
                _context.FormulaRawMaterials.Add(new FormulaRawMaterial
                {
                    FormulaId = formulaId,
                    RawMaterialId = rmId,
                    Percentage = r.TryGetProperty("Percentage", out var pct) && pct.ValueKind == JsonValueKind.Number ? pct.GetDecimal() : null,
                    Phase = r.TryGetProperty("Phase", out var ph) && ph.ValueKind == JsonValueKind.String ? ph.GetString() : null,
                    IsActive = true
                });
            }
        }

        // Composition: INCI ingredients
        var existingInci = await _context.INCIIngredients.Where(x => x.FormulaId == formulaId).ToListAsync();
        _context.INCIIngredients.RemoveRange(existingInci);
        if (root.TryGetProperty("INCIIngredients", out var incis) && incis.ValueKind == JsonValueKind.Array)
        {
            foreach (var i in incis.EnumerateArray())
            {
                var inciName = i.TryGetProperty("INCIName", out var nEl) && nEl.ValueKind == JsonValueKind.String ? nEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(inciName)) continue;
                _context.INCIIngredients.Add(new INCIIngredient
                {
                    FormulaId = formulaId,
                    INCIName = inciName!,
                    Function = i.TryGetProperty("Function", out var fEl) && fEl.ValueKind == JsonValueKind.String ? fEl.GetString() : null,
                    Percentage = i.TryGetProperty("Percentage", out var iPct) && iPct.ValueKind == JsonValueKind.Number ? iPct.GetDecimal() : null,
                    CASNumber = i.TryGetProperty("CASNumber", out var casEl) && casEl.ValueKind == JsonValueKind.String ? casEl.GetString() : null,
                    ECNumber = i.TryGetProperty("ECNumber", out var ecEl) && ecEl.ValueKind == JsonValueKind.String ? ecEl.GetString() : null
                });
            }
        }

        await _context.SaveChangesAsync();

        // Δημιούργησε νέα έκδοση με την επαναφορά
        var restored = await CreateVersionAsync(formulaId,
            $"Restored core composition from version {version.VersionNumber}",
            "Restoration",
            $"Restored by {userId}. NOTE: scalar fields + raw materials + INCI ingredients were restored; sub-documents (safety/price/marketing/allergens/heavy metals/claims) are NOT reverted by this operation.",
            userId);

        _logger.LogInformation("Restored formula {FormulaId} to version {Version} by {User} (new version id {NewVersionId})",
            formulaId, version.VersionNumber, userId, restored.Id);

        return true;
    }

    /// <summary>
    /// Έγκριση έκδοσης
    /// </summary>
    public async Task<bool> ApproveVersionAsync(int versionId, string approverId)
    {
        var version = await GetVersionAsync(versionId);
        if (version == null)
            return false;

        version.ApprovalStatus = "Approved";
        version.ApprovedBy = approverId;
        version.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Version {versionId} approved by {approverId}");

        return true;
    }

    /// <summary>
    /// Απόρριψη έκδοσης
    /// </summary>
    public async Task<bool> RejectVersionAsync(int versionId, string rejectorId, string reason)
    {
        var version = await GetVersionAsync(versionId);
        if (version == null)
            return false;

        version.ApprovalStatus = "Rejected";
        version.ApprovedBy = rejectorId;
        version.ApprovedAt = DateTime.UtcNow;
        version.Notes = $"Rejected: {reason}";

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Version {versionId} rejected by {rejectorId}");

        return true;
    }

    /// <summary>
    /// Serialize φόρμουλα σε JSON snapshot
    /// </summary>
    private string SerializeFormulaSnapshot(Formula formula)
    {
        var snapshot = new
        {
            formula.Id,
            formula.FormulaCode,
            formula.ProductName,
            formula.ProductType,
            formula.ManufacturingSite,
            formula.ManufacturingMethod,
            formula.PPIdCode,
            formula.IssueDate,
            formula.CreationDate,
            formula.PackagingInfo,
            formula.PhysicochemicalProperties,
            formula.MicrobiologicalProperties,
            formula.Comments,
            formula.ClientId,
            formula.TestNumber,
            formula.IsExternalDevelopment,
            formula.IsWIP,
            formula.ValidationDate,
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
                Percentage = ii.Percentage,
                CASNumber = ii.CASNumber,
                ECNumber = ii.ECNumber
            }).ToList(),
            SafetyData = formula.SafetyData,
            PriceInfo = formula.PriceInfo,
            Allergens = formula.Allergens.ToList(),
            HeavyMetals = formula.HeavyMetals.ToList(),
            MarketingInfo = formula.MarketingInfo,
            NaturalCertification = formula.NaturalCertification,
            Claims = formula.Claims.ToList()
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Υπολογισμός checksum για επαλήθευση
    /// </summary>
    private string CalculateChecksum(string data)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Επαλήθευση ακεραιότητας έκδοσης
    /// </summary>
    public bool VerifyVersionIntegrity(FormulaVersion version)
    {
        var calculatedChecksum = CalculateChecksum(version.VersionData);
        return calculatedChecksum == version.Checksum;
    }
}
