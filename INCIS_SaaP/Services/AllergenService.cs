// Author: Damianos Pythagoras Ougkrenidis
//
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using Microsoft.EntityFrameworkCore;

namespace INCIS_SaaP.Services;

/// <summary>
/// Υπηρεσία ελέγχου αλλεργιογόνων σύμφωνα με τον Κανονισμό ΕΚ αριθ. 1223/2009
/// </summary>
public class AllergenService
{
    private readonly INCISDbContext _context;

    // Ελάχιστα όρια δηλώσιμων αλλεργιογόνων (κατά EU Cosmetics Regulation 1223/2009)
    private static readonly Dictionary<string, decimal> EU_ALLERGEN_THRESHOLDS = new()
    {
        // Μείζονα αλλεργιογόνα που απαιτούν δήλωση σε κάθε περίπτωση
        { "alpha-isomethyl ionone", 0.001m },  // 0.001% = 10 ppm
        { "amyl cinnamal", 0.001m },
        { "amylcinnamyl alcohol", 0.001m },
        { "anise alcohol", 0.001m },
        { "benzyl alcohol", 0.001m },
        { "benzyl benzoate", 0.001m },
        { "benzyl cinnamate", 0.001m },
        { "benzyl salicylate", 0.001m },
        { "cinnamal", 0.001m },
        { "cinnamyl alcohol", 0.001m },
        { "citral", 0.001m },
        { "citronellol", 0.001m },
        { "coumarin", 0.001m },
        { "eugenol", 0.001m },
        { "farnesol", 0.001m },
        { "geraniol", 0.001m },
        { "hexyl cinnamal", 0.001m },
        { "hydroxycitronellal", 0.001m },
        { "limonene", 0.001m },
        { "linalool", 0.001m },
        { "methyl 2-octynoate", 0.001m },
        { "butylphenyl methylpropional", 0.001m },
        { "hexyl salicylate", 0.001m },
        { "2-benzylheptan-1-ol", 0.001m },
        { "phenoxyethanol", 0.001m }
    };

    public AllergenService(INCISDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ελέγχει όλα τα αλλεργιογόνα μιας φόρμουλας
    /// </summary>
    public async Task<AllergenCheckResult> CheckFormulaAllergens(int formulaId)
    {
        var allergens = await _context.Allergens
            .Where(a => a.FormulaId == formulaId)
            .ToListAsync();

        return CheckAllergens(allergens);
    }

    /// <summary>
    /// Ελέγχει τη λίστα αλλεργιογόνων
    /// </summary>
    public AllergenCheckResult CheckAllergens(List<Allergen> allergens)
    {
        var result = new AllergenCheckResult
        {
            TotalAllergens = allergens.Count,
            CheckedAt = DateTime.Now
        };

        foreach (var allergen in allergens)
        {
            var check = CheckAllergen(allergen);
            result.AllergenResults.Add(check);

            if (check.RequiresDeclaration)
                result.AllergensRequiringDeclaration++;

            if (check.IsAboveThreshold)
                result.AllergensAboveThreshold++;

            if (!check.IsCompliant)
                result.NonCompliantAllergens++;
        }

        result.IsCompliant = result.NonCompliantAllergens == 0;
        result.Summary = GenerateSummary(result);

        return result;
    }

    /// <summary>
    /// Ελέγχει ένα μεμονωμένο αλλεργιογόνο
    /// </summary>
    public AllergenCheckItem CheckAllergen(Allergen allergen)
    {
        var result = new AllergenCheckItem
        {
            AllergenName = allergen.AllergenName ?? "Unknown",
            Concentration = allergen.Concentration,
            Unit = allergen.Unit,
            IsDeclared = allergen.IsDeclared ?? false
        };

        // Έλεγχος αν υπάρχει κατώφλι για αυτό το αλλεργιογόνο
        var thresholdKey = allergen.AllergenName?.ToLower() ?? "";
        
        if (EU_ALLERGEN_THRESHOLDS.TryGetValue(thresholdKey, out var threshold))
        {
            result.Threshold = threshold;
            result.IsAboveThreshold = (allergen.Concentration ?? 0) > threshold;
            result.ThresholdUnit = "%";
            
            // Σύμφωνα με EU 1223/2009, τα αλλεργιογόνα πρέπει να δηλώνονται 
            // αν η συγκέντρωση > 0.001% (10 ppm) σε ξεπλυμένα προϊόντα
            // ή > 0.01% (100 ppm) σε μη ξεπλυμένα προϊόντα
            result.RequiresDeclaration = (allergen.Concentration ?? 0) >= threshold;
        }
        else
        {
            // Για αλλεργιογόνα χωρίς καθορισμένο όριο, χρησιμοποιούμε γενικό κατώφλι
            var defaultThreshold = 0.001m; // 0.001%
            result.Threshold = defaultThreshold;
            result.IsAboveThreshold = (allergen.Concentration ?? 0) > defaultThreshold;
            result.RequiresDeclaration = result.IsAboveThreshold;
        }

        // Έλεγχος συμμόρφωσης
        result.IsCompliant = !result.RequiresDeclaration || result.IsDeclared;

        if (!result.IsCompliant)
        {
            result.ComplianceMessage = result.RequiresDeclaration && !result.IsDeclared
                ? $"Απαιτείται δήλωση στην ετικέτα - Συγκέντρωση: {allergen.Concentration}%"
                : "Υπέρβαση ορίου ασφαλείας";
        }

        return result;
    }

    /// <summary>
    /// Ελέγχει αν υπάρχουν μη δηλωμένα αλλεργιογόνα
    /// </summary>
    public async Task<List<Allergen>> GetUndeclaredAllergens(int formulaId)
    {
        return await _context.Allergens
            .Where(a => a.FormulaId == formulaId && a.IsDeclared != true)
            .ToListAsync();
    }

    /// <summary>
    /// Υπολογίζει τη συνολική συγκέντρωση αλλεργιογόνων
    /// </summary>
    public decimal CalculateTotalAllergenConcentration(List<Allergen> allergens)
    {
        return allergens.Sum(a => a.Concentration ?? 0);
    }

    /// <summary>
    /// Λίστα αλλεργιογόνων που απαιτούν δήλωση (26 allergens)
    /// </summary>
    public static List<string> GetEUDeclaredAllergens()
    {
        return EU_ALLERGEN_THRESHOLDS.Keys.ToList();
    }

    private string GenerateSummary(AllergenCheckResult result)
    {
        if (result.IsCompliant)
        {
            return $"✓ Συμμορφούμενο - {result.AllergensRequiringDeclaration} αλλεργιογόνα απαιτούν δήλωση";
        }
        else
        {
            return $"✗ Μη συμμορφούμενο - {result.NonCompliantAllergens} αλλεργιογόνα χωρίς δήλωση";
        }
    }
}

/// <summary>
/// Αποτέλεσμα ελέγχου αλλεργιογόνων
/// </summary>
public class AllergenCheckResult
{
    public int TotalAllergens { get; set; }
    public int AllergensRequiringDeclaration { get; set; }
    public int AllergensAboveThreshold { get; set; }
    public int NonCompliantAllergens { get; set; }
    public bool IsCompliant { get; set; }
    public string Summary { get; set; } = "";
    public DateTime CheckedAt { get; set; }
    public List<AllergenCheckItem> AllergenResults { get; set; } = new();
}

/// <summary>
/// Αποτέλεσμα ελέγχου μεμονωμένου αλλεργιογόνου
/// </summary>
public class AllergenCheckItem
{
    public string AllergenName { get; set; } = "";
    public decimal? Concentration { get; set; }
    public string? Unit { get; set; }
    public decimal Threshold { get; set; }
    public string? ThresholdUnit { get; set; }
    public bool IsAboveThreshold { get; set; }
    public bool RequiresDeclaration { get; set; }
    public bool IsDeclared { get; set; }
    public bool IsCompliant { get; set; }
    public string? ComplianceMessage { get; set; }
}
