// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Δεδομένα ασφαλείας φόρμουλας
/// </summary>
public class SafetyData
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    [MaxLength(50)]
    public string? IrritationLevel { get; set; }

    [MaxLength(50)]
    public string? SkinCompatibility { get; set; }

    [MaxLength(50)]
    public string? EyeIrritation { get; set; }

    public bool? IsSafeForPregnancy { get; set; }

    [MaxLength(50)]
    public string? Phototoxicity { get; set; }

    [MaxLength(50)]
    public string? Sensitization { get; set; }

    [MaxLength(1000)]
    public string? Warnings { get; set; }

    [MaxLength(1000)]
    public string? Precautions { get; set; }

    public DateTime? SafetyAssessmentDate { get; set; }
}

/// <summary>
/// Πληροφορίες τιμολόγησης
/// </summary>
public class PriceInfo
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    public decimal? UnitPrice { get; set; }

    [MaxLength(20)]
    public string? Currency { get; set; }

    public decimal? CostPerUnit { get; set; }

    public decimal? ProfitMargin { get; set; }

    public decimal? RecommendedRetailPrice { get; set; }

    [MaxLength(100)]
    public string? PriceCategory { get; set; }

    public DateTime? LastPriceUpdate { get; set; }
}
