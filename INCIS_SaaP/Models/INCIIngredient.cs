// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Συστατικό INCI (Διεθνής ονομασία καλλυντικών συστατικών)
/// </summary>
public class INCIIngredient
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    [MaxLength(200)]
    public string? INCIName { get; set; }

    [MaxLength(100)]
    public string? Function { get; set; }

    public decimal? Percentage { get; set; }

    [MaxLength(100)]
    public string? CASNumber { get; set; }

    [MaxLength(100)]
    public string? ECNumber { get; set; }

    [MaxLength(500)]
    public string? Origin { get; set; }
}
