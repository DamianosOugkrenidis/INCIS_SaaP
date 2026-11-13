// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Αλλεργιογόνα στη φόρμουλα
/// </summary>
public class Allergen
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    [MaxLength(200)]
    public string? AllergenName { get; set; }

    [MaxLength(50)]
    public string? AllergenType { get; set; }

    public decimal? Concentration { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    public bool? IsDeclared { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Βαρέα μέταλλα στη φόρμουλα
/// </summary>
public class HeavyMetal
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    [MaxLength(100)]
    public string? MetalName { get; set; }

    public decimal? LimitValue { get; set; }

    public decimal? DetectedValue { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(50)]
    public string? TestMethod { get; set; }

    public bool? IsCompliant { get; set; }

    public DateTime? TestDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
