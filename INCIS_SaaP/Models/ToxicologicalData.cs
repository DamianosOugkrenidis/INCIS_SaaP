// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Τοξικολογικά δεδομένα ανά συστατικό INCI σε φόρμουλα
/// Χρησιμοποιείται στο FORM11 - Summarized Toxicological Profile
/// </summary>
public class ToxicologicalData
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    [MaxLength(200)]
    public string? InciName { get; set; }

    [MaxLength(100)]
    public string? CASNumber { get; set; }

    [MaxLength(100)]
    public string? Function { get; set; }

    /// <summary>
    /// Ποσοστό στη φόρμουλα (% w/w)
    /// </summary>
    public decimal? Percentage { get; set; }

    /// <summary>
    /// No Observed Adverse Effect Level (mg/kg bw/day)
    /// </summary>
    public decimal? NOAEL { get; set; }

    /// <summary>
    /// Systemic Exposure Dose (mg/kg bw/day)
    /// Calculated: SED = Concentration × DAa × A × f / (BW × 1000)
    /// DAa = Dermal Absorption, A = Amount applied, f = frequency, BW = body weight
    /// </summary>
    public decimal? SED { get; set; }

    /// <summary>
    /// Margin of Safety (MoS = NOAEL / SED)
    /// Safe if >= 100
    /// </summary>
    public decimal? MoS { get; set; }

    /// <summary>
    /// Toxicological category (e.g., "A" (safe), "B" (limited data), "C" (concern))
    /// </summary>
    [MaxLength(50)]
    public string? ToxicologicalCategory { get; set; }

    /// <summary>
    /// Αν θεωρείται ασφαλές βάσει MoS >= 100
    /// </summary>
    public bool IsSafe { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Πηγή των τοξικολογικών δεδομένων (SCCS, CIR, etc.)
    /// </summary>
    [MaxLength(200)]
    public string? DataSource { get; set; }

    public DateTime? AssessmentDate { get; set; }
}

/// <summary>
/// Ισχυρισμός ανά πρώτη ύλη (per-ingredient claim tracking)
/// Χρησιμοποιείται στο FORM10 - Formula Claims
/// </summary>
public class RawMaterialClaim
{
    [Key]
    public int Id { get; set; }

    public int RawMaterialId { get; set; }

    [ForeignKey("RawMaterialId")]
    public virtual RawMaterial? RawMaterial { get; set; }

    public bool IsVegan { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsGMOFree { get; set; }
    public bool IsMicroplasticFree { get; set; }
    public bool IsPhthalateFree { get; set; }
    public bool IsPalmOilFree { get; set; }
    public bool IsSiliconeFree { get; set; }
    public bool IsParabenFree { get; set; }
    public bool IsSulphateFree { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime? VerifiedDate { get; set; }
}
