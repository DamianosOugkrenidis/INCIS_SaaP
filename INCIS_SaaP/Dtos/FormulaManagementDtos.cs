// Author: Damianos Pythagoras Ougkrenidis
// DTOs for Formula Management Advanced Features
//
using System.ComponentModel.DataAnnotations;

namespace INCIS_SaaP.Dtos;

/// <summary>
/// DTO for creating a new formula version
/// </summary>
public class CreateFormulaVersionDto
{
    [Required]
    public string ChangeDescription { get; set; } = string.Empty;
    
    [Required]
    public string ChangeType { get; set; } = "Minor";
    
    public string? ChangeReason { get; set; }
}

/// <summary>
/// DTO for template creation
/// </summary>
public class CreateTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string TemplateCode { get; set; } = string.Empty;
    
    public int? CategoryId { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for approval submission
/// </summary>
public class SubmitForApprovalDto
{
    [Required]
    public int FormulaId { get; set; }
    
    public string? Title { get; set; }
    
    public string? Description { get; set; }
    
    public string ApprovalType { get; set; } = "Standard";
    
    public string Priority { get; set; } = "Medium";
    
    public int? WorkflowTemplateId { get; set; }
    
    public DateTime? Deadline { get; set; }
}

/// <summary>
/// DTO for step approval
/// </summary>
public class ApprovalStepActionDto
{
    [Required]
    public string Comments { get; set; } = string.Empty;
    
    public List<string>? Attachments { get; set; }
}

/// <summary>
/// DTO for formula cloning
/// </summary>
public class CloneFormulaDto
{
    [Required]
    [MaxLength(100)]
    public string NewFormulaCode { get; set; } = string.Empty;
    
    public int? TargetClientId { get; set; }
    
    public Dictionary<string, object>? Modifications { get; set; }
}

/// <summary>
/// DTO for batch operation
/// </summary>
public class BatchOperationDto
{
    [Required]
    public string OperationType { get; set; } = string.Empty;
    
    [Required]
    public List<int> FormulaIds { get; set; } = new();
    
    public Dictionary<string, object>? Parameters { get; set; }
    
    public string? Title { get; set; }
    
    public string Priority { get; set; } = "Medium";
}

/// <summary>
/// DTO for version comparison result
/// </summary>
public class VersionComparisonDto
{
    public int SourceVersionNumber { get; set; }
    
    public int TargetVersionNumber { get; set; }
    
    public List<FieldComparisonDto> Differences { get; set; } = new();
    
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// DTO for field comparison
/// </summary>
public class FieldComparisonDto
{
    public string FieldName { get; set; } = string.Empty;
    
    public string? OldValue { get; set; }
    
    public string? NewValue { get; set; }
    
    public string ChangeType { get; set; } = "Modified";
}

/// <summary>
/// DTO for approval statistics
/// </summary>
public class ApprovalStatisticsDto
{
    public int TotalApprovals { get; set; }
    
    public int PendingApprovals { get; set; }
    
    public int ApprovedCount { get; set; }
    
    public int RejectedCount { get; set; }
    
    public decimal AverageApprovalTimeHours { get; set; }
    
    public int OverdueCount { get; set; }
}
