// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Φυσική πιστοποίηση
/// </summary>
public class NaturalCertification
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    [MaxLength(100)]
    public string? CertificationType { get; set; }

    [MaxLength(100)]
    public string? CertifyingBody { get; set; }

    [MaxLength(50)]
    public string? CertificationLevel { get; set; }

    public decimal? NaturalPercentage { get; set; }

    public decimal? OrganicPercentage { get; set; }

    [MaxLength(200)]
    public string? CertifiedIngredients { get; set; }

    public DateTime? CertificationDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Ισχυρισμοί/Δηλώσεις προϊόντος
/// </summary>
public class Claim
{
    [Key]
    public int Id { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    [MaxLength(300)]
    public string? ClaimText { get; set; }

    [MaxLength(100)]
    public string? ClaimCategory { get; set; }

    [MaxLength(50)]
    public string? ClaimType { get; set; }

    public bool? IsVerified { get; set; }

    [MaxLength(500)]
    public string? SupportingEvidence { get; set; }

    [MaxLength(100)]
    public string? RegulatoryStatus { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
