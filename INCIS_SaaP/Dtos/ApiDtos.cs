// Author: Damianos Pythagoras Ougkrenidis
// DTOs for API endpoints

using System.ComponentModel.DataAnnotations;

namespace INCIS_SaaP.Dtos;

// Client DTOs
public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; }
}

public class CreateClientDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Code { get; set; }
    
    [MaxLength(200)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    
    [MaxLength(50)]
    public string? ContactPhone { get; set; }
    
    [MaxLength(100)]
    [EmailAddress]
    public string? ContactEmail { get; set; }
}

public class UpdateClientDto : CreateClientDto
{
    public bool IsActive { get; set; } = true;
}

// Supplier DTOs
public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSupplierDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [MaxLength(50)]
    public string? Phone { get; set; }
}

public class UpdateSupplierDto : CreateSupplierDto
{
    public bool IsActive { get; set; } = true;
}

// INCI DTOs
public class INCIDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? ECNumber { get; set; }
    public string? Description { get; set; }
    public string? Function { get; set; }
    public string? Synonyms { get; set; }
    public bool IsActive { get; set; }
}

public class CreateINCIDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? CASNumber { get; set; }
    
    [MaxLength(100)]
    public string? ECNumber { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(200)]
    public string? Function { get; set; }
    
    [MaxLength(200)]
    public string? Synonyms { get; set; }
}

public class UpdateINCIDto : CreateINCIDto
{
    public bool IsActive { get; set; } = true;
}

// RawMaterial DTOs
public class RawMaterialDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ManufacturerName { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public bool IsActive { get; set; }
    public bool IsFullyRegistered { get; set; }
    public bool IsEvaluated { get; set; }
}

public class CreateRawMaterialDto
{
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? ManufacturerName { get; set; }
    
    [MaxLength(200)]
    public string? ManufacturerAddress { get; set; }
    
    [MaxLength(100)]
    public string? ManufacturerPhone { get; set; }
    
    [MaxLength(100)]
    [EmailAddress]
    public string? ManufacturerEmail { get; set; }
    
    [MaxLength(500)]
    public string? Allergens { get; set; }
    
    public decimal? NaturalOriginPercentage { get; set; }
    
    [MaxLength(100)]
    public string? ShelfLife { get; set; }
    
    [MaxLength(200)]
    public string? StorageConditions { get; set; }
    
    [MaxLength(100)]
    public string? Form { get; set; }
    
    [MaxLength(100)]
    public string? Color { get; set; }
    
    [MaxLength(100)]
    public string? Odor { get; set; }
    
    public decimal? Density { get; set; }
    
    [MaxLength(200)]
    public string? Solubility { get; set; }
    
    [MaxLength(500)]
    public string? Compatibilities { get; set; }
    
    public decimal? Price { get; set; }
    
    [MaxLength(1000)]
    public string? HealthAndSafety { get; set; }
    
    [MaxLength(500)]
    public string? Comments { get; set; }
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public int? SupplierId { get; set; }
    public int? INCIId { get; set; }
    
    // Document flags
    public bool HasTDS { get; set; }
    public bool HasMSDS { get; set; }
    public bool HasISO16128 { get; set; }
    public bool HasBSE_TSE { get; set; }
    public bool HasVegan { get; set; }
    public bool HasRMI { get; set; }
    public bool HasAllergenCertificate { get; set; }
    public bool HasPhthalates_BHT_Statement { get; set; }
    public bool HasSolventVehicleStatement { get; set; }
    public bool HasMarketingInfo { get; set; }
}

public class UpdateRawMaterialDto : CreateRawMaterialDto
{
    public bool IsActive { get; set; } = true;
    public bool IsFullyRegistered { get; set; }
    public bool IsEvaluated { get; set; }
}

// Sample DTOs
public class RawMaterialSampleDto
{
    public int Id { get; set; }
    public int RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExpired => DateTime.Now > ExpiryDate;
    public bool IsActive { get; set; }
}

public class CreateSampleDto
{
    public int RawMaterialId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string LotNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime ReceivedDate { get; set; }
    
    [Required]
    public DateTime ExpiryDate { get; set; }
}

public class UpdateSampleDto : CreateSampleDto
{
    public bool IsActive { get; set; } = true;
}

// Restriction DTOs
public class RestrictionDto
{
    public int Id { get; set; }
    public int INCIId { get; set; }
    public string? INCIName { get; set; }
    public string? Regulation { get; set; }
    public string? Description { get; set; }
    public decimal? MaxPercentage { get; set; }
    public bool IsRinseOffOnly { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRestrictionDto
{
    [Required]
    public int INCIId { get; set; }
    
    [MaxLength(200)]
    public string? Regulation { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public decimal? MaxPercentage { get; set; }
    
    public bool IsRinseOffOnly { get; set; }
}

public class UpdateRestrictionDto : CreateRestrictionDto
{
    public bool IsActive { get; set; } = true;
}

// Formula Validation DTOs
public class ValidateFormulaDto
{
    [Required]
    public int FormulaId { get; set; }
    
    public string? Comments { get; set; }
}

// Change Supplier DTOs
public class ChangeSupplierDto
{
    [Required]
    public int RawMaterialId { get; set; }
    
    [Required]
    public int NewSupplierId { get; set; }
    
    public string? Reason { get; set; }
}

// Natural Calculation DTOs
public class NaturalCalculationDto
{
    public int FormulaId { get; set; }
    public decimal TotalNaturalPercentage { get; set; }
    public List<NaturalIngredientDto> Ingredients { get; set; } = new();
}

public class NaturalIngredientDto
{
    public string IngredientName { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal NaturalOriginPercentage { get; set; }
    public decimal ContributionToTotal { get; set; }
}

// Copy Raw Material DTO
public class CopyRawMaterialDto
{
    [Required]
    public int SourceRawMaterialId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string NewCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string NewProductName { get; set; } = string.Empty;
    
    public bool CopySamples { get; set; } = false;
    public bool CopyComposition { get; set; } = true;
}
