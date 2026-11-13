// Author: Damianos Pythagoras Ougkrenidis
//
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using Microsoft.EntityFrameworkCore;

namespace INCIS_SaaP.Services;

/// <summary>
/// Υπηρεσία ελέγχου συμμόρφωσης βαρέων μετάλλων
/// Βάσει EU Cosmetics Regulation 1223/2009 και EU SCCS opinions
/// </summary>
public class HeavyMetalComplianceService
{
    private readonly INCISDbContext _context;

    // Όρια βαρέων μετάλλων (σε ppm = mg/kg) για καλλυντικά
    // Βάσει EU SCCS και διεθνών προτύπων
    private static readonly Dictionary<string, HeavyMetalLimit> METAL_LIMITS = new()
    {
        { "lead", new HeavyMetalLimit { Limit = 2.0m, Unit = "ppm", Regulation = "EU 1223/2009", Notes = "Μόλυβδος" } },
        { "arsenic", new HeavyMetalLimit { Limit = 2.0m, Unit = "ppm", Regulation = "EU 1223/2009", Notes = "Αρσενικό" } },
        { "cadmium", new HeavyMetalLimit { Limit = 1.0m, Unit = "ppm", Regulation = "EU 1223/2009", Notes = "Κάδμιο" } },
        { "mercury", new HeavyMetalLimit { Limit = 1.0m, Unit = "ppm", Regulation = "EU 1223/2009", Notes = "Υδράργυρος" } },
        { "nickel", new HeavyMetalLimit { Limit = 10.0m, Unit = "ppm", Regulation = "EU SCCS", Notes = "Νικέλιο - έλεγχος αλλεργίας" } },
        { "chromium", new HeavyMetalLimit { Limit = 5.0m, Unit = "ppm", Regulation = "EU SCCS", Notes = "Χρώμιο" } },
        { "antimony", new HeavyMetalLimit { Limit = 5.0m, Unit = "ppm", Regulation = "Industry Standard", Notes = "Αντιμόνιο" } },
        { "copper", new HeavyMetalLimit { Limit = 25.0m, Unit = "ppm", Regulation = "Industry Standard", Notes = "Χαλκός" } },
        { "zinc", new HeavyMetalLimit { Limit = 50.0m, Unit = "ppm", Regulation = "Industry Standard", Notes = "Ψευδάργυρος" } },
        { "iron", new HeavyMetalLimit { Limit = 100.0m, Unit = "ppm", Regulation = "Industry Standard", Notes = "Σίδηρος" } },
        { "manganese", new HeavyMetalLimit { Limit = 20.0m, Unit = "ppm", Regulation = "Industry Standard", Notes = "Μαγγάνιο" } }
    };

    public HeavyMetalComplianceService(INCISDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ελέγχει τη συμμόρφωση βαρέων μετάλλων μιας φόρμουλας
    /// </summary>
    public async Task<HeavyMetalComplianceResult> CheckCompliance(int formulaId)
    {
        var heavyMetals = await _context.HeavyMetals
            .Where(h => h.FormulaId == formulaId)
            .ToListAsync();

        return CheckCompliance(heavyMetals);
    }

    /// <summary>
    /// Ελέγχει τη συμμόρφωση λίστας βαρέων μετάλλων
    /// </summary>
    public HeavyMetalComplianceResult CheckCompliance(List<HeavyMetal> heavyMetals)
    {
        var result = new HeavyMetalComplianceResult
        {
            TotalMetalsTested = heavyMetals.Count,
            CheckedAt = DateTime.Now
        };

        foreach (var metal in heavyMetals)
        {
            var check = CheckMetalCompliance(metal);
            result.MetalResults.Add(check);

            if (check.IsCompliant)
                result.CompliantMetals++;
            else
                result.NonCompliantMetals++;

            if (check.IsDetected)
                result.MetalsDetected++;
        }

        result.IsOverallCompliant = result.NonCompliantMetals == 0;
        result.CompliancePercentage = result.TotalMetalsTested > 0
            ? (decimal)result.CompliantMetals / result.TotalMetalsTested * 100
            : 100;

        result.Summary = GenerateSummary(result);

        return result;
    }

    /// <summary>
    /// Ελέγχει τη συμμόρφωση ενός μεμονωμένου μετάλλου
    /// </summary>
    public MetalComplianceCheck CheckMetalCompliance(HeavyMetal metal)
    {
        var result = new MetalComplianceCheck
        {
            MetalName = metal.MetalName ?? "Unknown",
            DetectedValue = metal.DetectedValue,
            LimitValue = metal.LimitValue,
            Unit = metal.Unit ?? "ppm",
            TestMethod = metal.TestMethod
        };

        // Καθορισμός ορίου
        var metalKey = metal.MetalName?.ToLower() ?? "";
        
        if (METAL_LIMITS.TryGetValue(metalKey, out var standardLimit))
        {
            result.StandardLimit = standardLimit.Limit;
            result.Regulation = standardLimit.Regulation;
            result.Notes = standardLimit.Notes;
            
            // Χρήση του μεγαλύτερου ορίου (το πιο αυστηρό)
            result.ApplicableLimit = Math.Max(
                metal.LimitValue ?? standardLimit.Limit,
                standardLimit.Limit
            );
        }
        else
        {
            // Χρήση ορίου από τη βάση δεδομένων ή προκαθορισμένου
            result.StandardLimit = metal.LimitValue ?? 10.0m;
            result.ApplicableLimit = metal.LimitValue ?? 10.0m;
            result.Regulation = "User Defined";
        }

        // Έλεγχος αν εντοπίστηκε μέταλλο
        result.IsDetected = (metal.DetectedValue ?? 0) > 0;

        // Έλεγχος συμμόρφωσης
        if (result.IsDetected)
        {
            result.IsCompliant = metal.DetectedValue <= result.ApplicableLimit;
            
            if (!result.IsCompliant)
            {
                var exceedance = metal.DetectedValue - result.ApplicableLimit;
                result.ExceedanceAmount = exceedance;
                result.ExceedancePercentage = (exceedance / result.ApplicableLimit) * 100;
                result.ComplianceMessage = $"Υπέρβαση ορίου κατά {result.ExceedancePercentage:F1}%";
            }
            else
            {
                result.ComplianceMessage = "Εντός ορίων";
            }
        }
        else
        {
            result.IsCompliant = true;
            result.ComplianceMessage = "Δεν ανιχνεύτηκε";
        }

        return result;
    }

    /// <summary>
    /// Ελέγχει αν απαιτείται δοκιμή βαρέων μετάλλων
    /// </summary>
    public bool RequiresHeavyMetalTesting(string productType)
    {
        // Βάσει EU 1223/2009, τα βαρέα μέταλλα πρέπει να ελέγχονται
        // ιδιαίτερα για προϊόντα που μένουν στο δέρμα
        var productsRequiringTesting = new[] 
        { 
            "cream", "lotion", "makeup", "foundation", 
            "serum", "oil", "balm", "butter"
        };

        return productsRequiringTesting.Any(p => 
            productType?.ToLower().Contains(p) ?? false);
    }

    /// <summary>
    /// Υπολογίζει τη συνολική επιβάρυνση βαρέων μετάλλων
    /// </summary>
    public decimal CalculateTotalHeavyMetalBurden(List<HeavyMetal> metals)
    {
        return metals.Sum(m => m.DetectedValue ?? 0);
    }

    /// <summary>
    /// Επιστρέφει τα όρια για όλα τα μέταλλα
    /// </summary>
    public static Dictionary<string, HeavyMetalLimit> GetAllLimits()
    {
        return METAL_LIMITS;
    }

    /// <summary>
    /// Ελέγχει τη διαθεσιμότητα ορίου για ένα μέταλλο
    /// </summary>
    public static bool HasLimit(string metalName)
    {
        return METAL_LIMITS.ContainsKey(metalName?.ToLower() ?? "");
    }

    private string GenerateSummary(HeavyMetalComplianceResult result)
    {
        if (result.IsOverallCompliant)
        {
            if (result.MetalsDetected == 0)
            {
                return $"✓ Συμμορφούμενο - Κανένα βαρύ μέταλλο δεν ανιχνεύτηκε";
            }
            else
            {
                return $"✓ Συμμορφούμενο - {result.CompliantMetals}/{result.TotalMetalsTested} μέταλλα εντός ορίων";
            }
        }
        else
        {
            return $"✗ Μη συμμορφούμενο - {result.NonCompliantMetals} μέταλλα εκτός ορίων";
        }
    }
}

/// <summary>
/// Όριο βαρέων μετάλλων
/// </summary>
public class HeavyMetalLimit
{
    public decimal Limit { get; set; }
    public string Unit { get; set; } = "ppm";
    public string Regulation { get; set; } = "";
    public string Notes { get; set; } = "";
}

/// <summary>
/// Αποτέλεσμα ελέγχου συμμόρφωσης
/// </summary>
public class HeavyMetalComplianceResult
{
    public int TotalMetalsTested { get; set; }
    public int MetalsDetected { get; set; }
    public int CompliantMetals { get; set; }
    public int NonCompliantMetals { get; set; }
    public bool IsOverallCompliant { get; set; }
    public decimal CompliancePercentage { get; set; }
    public string Summary { get; set; } = "";
    public DateTime CheckedAt { get; set; }
    public List<MetalComplianceCheck> MetalResults { get; set; } = new();
}

/// <summary>
/// Έλεγχος συμμόρφωσης μεμονωμένου μετάλλου
/// </summary>
public class MetalComplianceCheck
{
    public string MetalName { get; set; } = "";
    public decimal? DetectedValue { get; set; }
    public decimal? LimitValue { get; set; }
    public decimal StandardLimit { get; set; }
    public decimal ApplicableLimit { get; set; }
    public string Unit { get; set; } = "ppm";
    public string? TestMethod { get; set; }
    public string Regulation { get; set; } = "";
    public string? Notes { get; set; }
    public bool IsDetected { get; set; }
    public bool IsCompliant { get; set; }
    public decimal? ExceedanceAmount { get; set; }
    public decimal? ExceedancePercentage { get; set; }
    public string? ComplianceMessage { get; set; }
}
