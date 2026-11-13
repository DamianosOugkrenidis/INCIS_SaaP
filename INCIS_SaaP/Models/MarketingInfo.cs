// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Πληροφορίες μάρκετινγκ
/// </summary>
public class MarketingInfo
{
    [Key]
    public int Id { get; set; }
    
    public int FormulaId { get; set; }
    
    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }
    
    [MaxLength(200)]
    public string? ProductName { get; set; }
    
    [MaxLength(500)]
    public string? ProductDescription { get; set; }
    
    [MaxLength(100)]
    public string? TargetAudience { get; set; }
    
    [MaxLength(50)]
    public string? AgeRange { get; set; }
    
    [MaxLength(50)]
    public string? GenderTarget { get; set; }
    
    [MaxLength(200)]
    public string? KeyBenefits { get; set; }
    
    [MaxLength(500)]
    public string? UsageInstructions { get; set; }
    
    [MaxLength(100)]
    public string? PackagingType { get; set; }
    
    [MaxLength(100)]
    public string? DistributionChannel { get; set; }
    
    public DateTime? LaunchDate { get; set; }
}
