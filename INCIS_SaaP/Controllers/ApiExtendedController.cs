// Author: Damianos Pythagoras Ougkrenidis
// Extended API Controller με all missing endpoints for full Mproject compliance

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Dtos;
using System.Security.Claims;
using ClosedXML.Excel;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Extended API Controller με full CRUD operations και advanced features
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[EnableRateLimiting("Strict")]
public class ApiExtendedController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiExtendedController> _logger;

    public ApiExtendedController(INCISDbContext context, ILogger<ApiExtendedController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Clients CRUD

    /// <summary>
    /// Get all clients (paginated)
    /// </summary>
    [HttpGet("clients")]
    public async Task<IActionResult> GetClients([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.Clients.Where(c => c.IsActive);
        var total = await query.CountAsync();
        
        var clients = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Address = c.Address,
                Email = c.Email,
                Phone = c.Phone,
                ContactPerson = c.ContactPerson,
                ContactPhone = c.ContactPhone,
                ContactEmail = c.ContactEmail,
                IsActive = c.IsActive
            })
            .ToListAsync();

        return Ok(new
        {
            data = clients,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get client by ID
    /// </summary>
    [HttpGet("clients/{id}")]
    public async Task<IActionResult> GetClient(int id)
    {
        var client = await _context.Clients
            .Where(c => c.Id == id)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Address = c.Address,
                Email = c.Email,
                Phone = c.Phone,
                ContactPerson = c.ContactPerson,
                ContactPhone = c.ContactPhone,
                ContactEmail = c.ContactEmail,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync();

        if (client == null)
            return NotFound(new { message = "Client not found" });

        return Ok(client);
    }

    /// <summary>
    /// Create new client
    /// </summary>
    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var client = new Client
        {
            Name = dto.Name,
            Code = dto.Code,
            Address = dto.Address,
            Email = dto.Email,
            Phone = dto.Phone,
            ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {Id} created by user {UserId}", client.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, new { id = client.Id, message = "Client created successfully" });
    }

    /// <summary>
    /// Update existing client
    /// </summary>
    [HttpPut("clients/{id}")]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound(new { message = "Client not found" });

        client.Name = dto.Name;
        client.Code = dto.Code;
        client.Address = dto.Address;
        client.Email = dto.Email;
        client.Phone = dto.Phone;
        client.ContactPerson = dto.ContactPerson;
        client.ContactPhone = dto.ContactPhone;
        client.ContactEmail = dto.ContactEmail;
        client.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {Id} updated by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Client updated successfully" });
    }

    /// <summary>
    /// Delete client (soft delete)
    /// </summary>
    [HttpDelete("clients/{id}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound(new { message = "Client not found" });

        client.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {Id} deleted by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Client deleted successfully" });
    }

    #endregion

    #region Suppliers CRUD

    /// <summary>
    /// Get all suppliers (paginated)
    /// </summary>
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.Suppliers.Where(s => s.IsActive);
        var total = await query.CountAsync();
        
        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Email = s.Email,
                Phone = s.Phone,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(new
        {
            data = suppliers,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    [HttpGet("suppliers/{id}")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var supplier = await _context.Suppliers
            .Where(s => s.Id == id)
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Email = s.Email,
                Phone = s.Phone,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync();

        if (supplier == null)
            return NotFound(new { message = "Supplier not found" });

        return Ok(supplier);
    }

    /// <summary>
    /// Create new supplier
    /// </summary>
    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var supplier = new Supplier
        {
            Name = dto.Name,
            Address = dto.Address,
            Email = dto.Email,
            Phone = dto.Phone,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Supplier {Id} created by user {UserId}", supplier.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, new { id = supplier.Id, message = "Supplier created successfully" });
    }

    /// <summary>
    /// Update existing supplier
    /// </summary>
    [HttpPut("suppliers/{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound(new { message = "Supplier not found" });

        supplier.Name = dto.Name;
        supplier.Address = dto.Address;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Supplier {Id} updated by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Supplier updated successfully" });
    }

    /// <summary>
    /// Delete supplier (soft delete)
    /// </summary>
    [HttpDelete("suppliers/{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound(new { message = "Supplier not found" });

        supplier.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Supplier {Id} deleted by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Supplier deleted successfully" });
    }

    #endregion

    #region INCIS CRUD

    /// <summary>
    /// Get all INCIS (paginated)
    /// </summary>
    [HttpGet("incis")]
    public async Task<IActionResult> GetINCIS([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.INCIs.Where(i => i.IsActive);
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search) || 
                                    (i.CASNumber != null && i.CASNumber.Contains(search)) ||
                                    (i.ECNumber != null && i.ECNumber.Contains(search)));
        }

        var total = await query.CountAsync();
        
        var incis = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new INCIDto
            {
                Id = i.Id,
                Name = i.Name,
                CASNumber = i.CASNumber,
                ECNumber = i.ECNumber,
                Description = i.Description,
                Function = i.Function,
                Synonyms = i.Synonyms,
                IsActive = i.IsActive
            })
            .ToListAsync();

        return Ok(new
        {
            data = incis,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get INCI by ID
    /// </summary>
    [HttpGet("incis/{id}")]
    public async Task<IActionResult> GetINCI(int id)
    {
        var inci = await _context.INCIs
            .Where(i => i.Id == id)
            .Select(i => new INCIDto
            {
                Id = i.Id,
                Name = i.Name,
                CASNumber = i.CASNumber,
                ECNumber = i.ECNumber,
                Description = i.Description,
                Function = i.Function,
                Synonyms = i.Synonyms,
                IsActive = i.IsActive
            })
            .FirstOrDefaultAsync();

        if (inci == null)
            return NotFound(new { message = "INCI not found" });

        return Ok(inci);
    }

    /// <summary>
    /// Create new INCI
    /// </summary>
    [HttpPost("incis")]
    public async Task<IActionResult> CreateINCI([FromBody] CreateINCIDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var inci = new INCI
        {
            Name = dto.Name,
            CASNumber = dto.CASNumber,
            ECNumber = dto.ECNumber,
            Description = dto.Description,
            Function = dto.Function,
            Synonyms = dto.Synonyms,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.INCIs.Add(inci);
        await _context.SaveChangesAsync();

        _logger.LogInformation("INCI {Id} created by user {UserId}", inci.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return CreatedAtAction(nameof(GetINCI), new { id = inci.Id }, new { id = inci.Id, message = "INCI created successfully" });
    }

    /// <summary>
    /// Update existing INCI
    /// </summary>
    [HttpPut("incis/{id}")]
    public async Task<IActionResult> UpdateINCI(int id, [FromBody] UpdateINCIDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var inci = await _context.INCIs.FindAsync(id);
        if (inci == null)
            return NotFound(new { message = "INCI not found" });

        inci.Name = dto.Name;
        inci.CASNumber = dto.CASNumber;
        inci.ECNumber = dto.ECNumber;
        inci.Description = dto.Description;
        inci.Function = dto.Function;
        inci.Synonyms = dto.Synonyms;
        inci.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("INCI {Id} updated by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "INCI updated successfully" });
    }

    /// <summary>
    /// Delete INCI (soft delete)
    /// </summary>
    [HttpDelete("incis/{id}")]
    public async Task<IActionResult> DeleteINCI(int id)
    {
        var inci = await _context.INCIs.FindAsync(id);
        if (inci == null)
            return NotFound(new { message = "INCI not found" });

        inci.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("INCI {Id} deleted by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "INCI deleted successfully" });
    }

    #endregion

    #region Raw Materials CRUD

    /// <summary>
    /// Get all raw materials (paginated)
    /// </summary>
    [HttpGet("rawmaterials")]
    public async Task<IActionResult> GetRawMaterials([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.RawMaterials.Include(r => r.Supplier).Where(r => r.IsActive);
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.ProductName.Contains(search) || r.Code.Contains(search));
        }

        var total = await query.CountAsync();
        
        var rawMaterials = await query
            .OrderBy(r => r.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RawMaterialDto
            {
                Id = r.Id,
                Code = r.Code,
                ProductName = r.ProductName,
                ManufacturerName = r.ManufacturerName,
                Category = r.Category,
                Price = r.Price,
                SupplierId = r.SupplierId,
                SupplierName = r.Supplier != null ? r.Supplier.Name : null,
                IsActive = r.IsActive,
                IsFullyRegistered = r.IsFullyRegistered,
                IsEvaluated = r.IsEvaluated
            })
            .ToListAsync();

        return Ok(new
        {
            data = rawMaterials,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get raw material by ID
    /// </summary>
    [HttpGet("rawmaterials/{id}")]
    public async Task<IActionResult> GetRawMaterial(int id)
    {
        var rawMaterial = await _context.RawMaterials
            .Include(r => r.Supplier)
            .Include(r => r.MainINCI)
            .Where(r => r.Id == id)
            .FirstOrDefaultAsync();

        if (rawMaterial == null)
            return NotFound(new { message = "Raw material not found" });

        return Ok(rawMaterial);
    }

    /// <summary>
    /// Create new raw material
    /// </summary>
    [HttpPost("rawmaterials")]
    public async Task<IActionResult> CreateRawMaterial([FromBody] CreateRawMaterialDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var rawMaterial = new RawMaterial
        {
            Code = dto.Code,
            ProductName = dto.ProductName,
            ManufacturerName = dto.ManufacturerName,
            ManufacturerAddress = dto.ManufacturerAddress,
            ManufacturerPhone = dto.ManufacturerPhone,
            ManufacturerEmail = dto.ManufacturerEmail,
            Allergens = dto.Allergens,
            NaturalOriginPercentage = dto.NaturalOriginPercentage,
            ShelfLife = dto.ShelfLife,
            StorageConditions = dto.StorageConditions,
            Form = dto.Form,
            Color = dto.Color,
            Odor = dto.Odor,
            Density = dto.Density,
            Solubility = dto.Solubility,
            Compatibilities = dto.Compatibilities,
            Price = dto.Price,
            HealthAndSafety = dto.HealthAndSafety,
            Comments = dto.Comments,
            Category = dto.Category,
            SupplierId = dto.SupplierId,
            INCIId = dto.INCIId,
            HasTDS = dto.HasTDS,
            HasMSDS = dto.HasMSDS,
            HasISO16128 = dto.HasISO16128,
            HasBSE_TSE = dto.HasBSE_TSE,
            HasVegan = dto.HasVegan,
            HasRMI = dto.HasRMI,
            HasAllergenCertificate = dto.HasAllergenCertificate,
            HasPhthalates_BHT_Statement = dto.HasPhthalates_BHT_Statement,
            HasSolventVehicleStatement = dto.HasSolventVehicleStatement,
            HasMarketingInfo = dto.HasMarketingInfo,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.RawMaterials.Add(rawMaterial);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Raw material {Id} created by user {UserId}", rawMaterial.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return CreatedAtAction(nameof(GetRawMaterial), new { id = rawMaterial.Id }, new { id = rawMaterial.Id, message = "Raw material created successfully" });
    }

    /// <summary>
    /// Update existing raw material
    /// </summary>
    [HttpPut("rawmaterials/{id}")]
    public async Task<IActionResult> UpdateRawMaterial(int id, [FromBody] UpdateRawMaterialDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial == null)
            return NotFound(new { message = "Raw material not found" });

        rawMaterial.Code = dto.Code;
        rawMaterial.ProductName = dto.ProductName;
        rawMaterial.ManufacturerName = dto.ManufacturerName;
        rawMaterial.ManufacturerAddress = dto.ManufacturerAddress;
        rawMaterial.ManufacturerPhone = dto.ManufacturerPhone;
        rawMaterial.ManufacturerEmail = dto.ManufacturerEmail;
        rawMaterial.Allergens = dto.Allergens;
        rawMaterial.NaturalOriginPercentage = dto.NaturalOriginPercentage;
        rawMaterial.ShelfLife = dto.ShelfLife;
        rawMaterial.StorageConditions = dto.StorageConditions;
        rawMaterial.Form = dto.Form;
        rawMaterial.Color = dto.Color;
        rawMaterial.Odor = dto.Odor;
        rawMaterial.Density = dto.Density;
        rawMaterial.Solubility = dto.Solubility;
        rawMaterial.Compatibilities = dto.Compatibilities;
        rawMaterial.Price = dto.Price;
        rawMaterial.HealthAndSafety = dto.HealthAndSafety;
        rawMaterial.Comments = dto.Comments;
        rawMaterial.Category = dto.Category;
        rawMaterial.SupplierId = dto.SupplierId;
        rawMaterial.INCIId = dto.INCIId;
        rawMaterial.HasTDS = dto.HasTDS;
        rawMaterial.HasMSDS = dto.HasMSDS;
        rawMaterial.HasISO16128 = dto.HasISO16128;
        rawMaterial.HasBSE_TSE = dto.HasBSE_TSE;
        rawMaterial.HasVegan = dto.HasVegan;
        rawMaterial.HasRMI = dto.HasRMI;
        rawMaterial.HasAllergenCertificate = dto.HasAllergenCertificate;
        rawMaterial.HasPhthalates_BHT_Statement = dto.HasPhthalates_BHT_Statement;
        rawMaterial.HasSolventVehicleStatement = dto.HasSolventVehicleStatement;
        rawMaterial.HasMarketingInfo = dto.HasMarketingInfo;
        rawMaterial.IsActive = dto.IsActive;
        rawMaterial.IsFullyRegistered = dto.IsFullyRegistered;
        rawMaterial.IsEvaluated = dto.IsEvaluated;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Raw material {Id} updated by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Raw material updated successfully" });
    }

    /// <summary>
    /// Delete raw material (soft delete)
    /// </summary>
    [HttpDelete("rawmaterials/{id}")]
    public async Task<IActionResult> DeleteRawMaterial(int id)
    {
        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial == null)
            return NotFound(new { message = "Raw material not found" });

        rawMaterial.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Raw material {Id} deleted by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Raw material deleted successfully" });
    }

    /// <summary>
    /// Copy raw material with optional samples and composition
    /// </summary>
    [HttpPost("rawmaterials/copy")]
    public async Task<IActionResult> CopyRawMaterial([FromBody] CopyRawMaterialDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var source = await _context.RawMaterials
            .Include(r => r.Composition)
            .Include(r => r.Samples)
            .FirstOrDefaultAsync(r => r.Id == dto.SourceRawMaterialId);

        if (source == null)
            return NotFound(new { message = "Source raw material not found" });

        // Create new raw material
        var newRawMaterial = new RawMaterial
        {
            Code = dto.NewCode,
            ProductName = dto.NewProductName,
            ManufacturerName = source.ManufacturerName,
            ManufacturerAddress = source.ManufacturerAddress,
            ManufacturerPhone = source.ManufacturerPhone,
            ManufacturerEmail = source.ManufacturerEmail,
            Allergens = source.Allergens,
            NaturalOriginPercentage = source.NaturalOriginPercentage,
            ShelfLife = source.ShelfLife,
            StorageConditions = source.StorageConditions,
            Form = source.Form,
            Color = source.Color,
            Odor = source.Odor,
            Density = source.Density,
            Solubility = source.Solubility,
            Compatibilities = source.Compatibilities,
            Price = source.Price,
            HealthAndSafety = source.HealthAndSafety,
            Comments = source.Comments,
            Category = source.Category,
            SupplierId = source.SupplierId,
            INCIId = source.INCIId,
            HasTDS = source.HasTDS,
            HasMSDS = source.HasMSDS,
            HasISO16128 = source.HasISO16128,
            HasBSE_TSE = source.HasBSE_TSE,
            HasVegan = source.HasVegan,
            HasRMI = source.HasRMI,
            HasAllergenCertificate = source.HasAllergenCertificate,
            HasPhthalates_BHT_Statement = source.HasPhthalates_BHT_Statement,
            HasSolventVehicleStatement = source.HasSolventVehicleStatement,
            HasMarketingInfo = source.HasMarketingInfo,
            CreatedAt = DateTime.Now,
            IsActive = true,
            IsFullyRegistered = source.IsFullyRegistered,
            IsEvaluated = source.IsEvaluated
        };

        _context.RawMaterials.Add(newRawMaterial);
        await _context.SaveChangesAsync();

        // Copy composition if requested
        if (dto.CopyComposition && source.Composition.Any())
        {
            foreach (var comp in source.Composition)
            {
                var newComp = new RawMaterialComposition
                {
                    RawMaterialId = newRawMaterial.Id,
                    INCIId = comp.INCIId,
                    MinPercentage = comp.MinPercentage,
                    MaxPercentage = comp.MaxPercentage,
                    IsImpurity = comp.IsImpurity
                };
                _context.Set<RawMaterialComposition>().Add(newComp);
            }
            await _context.SaveChangesAsync();
        }

        // Copy samples if requested  
        if (dto.CopySamples && source.Samples.Any())
        {
            foreach (var sample in source.Samples)
            {
                var newSample = new RawMaterialSample
                {
                    RawMaterialId = newRawMaterial.Id,
                    LotNumber = sample.LotNumber + "-COPY",
                    ReceivedDate = sample.ReceivedDate,
                    ExpiryDate = sample.ExpiryDate,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                _context.Set<RawMaterialSample>().Add(newSample);
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Raw material {SourceId} copied to {NewId} by user {UserId}", 
            dto.SourceRawMaterialId, newRawMaterial.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return CreatedAtAction(nameof(GetRawMaterial), new { id = newRawMaterial.Id }, 
            new { id = newRawMaterial.Id, message = "Raw material copied successfully" });
    }

    #endregion

    #region Sample Management

    /// <summary>
    /// Get samples for a raw material
    /// </summary>
    [HttpGet("rawmaterials/{id}/samples")]
    public async Task<IActionResult> GetRawMaterialSamples(int id)
    {
        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial == null)
            return NotFound(new { message = "Raw material not found" });

        var samples = await _context.Set<RawMaterialSample>()
            .Where(s => s.RawMaterialId == id)
            .OrderByDescending(s => s.ReceivedDate)
            .Select(s => new RawMaterialSampleDto
            {
                Id = s.Id,
                RawMaterialId = s.RawMaterialId,
                RawMaterialName = rawMaterial.ProductName,
                LotNumber = s.LotNumber,
                ReceivedDate = s.ReceivedDate,
                ExpiryDate = s.ExpiryDate,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(samples);
    }

    /// <summary>
    /// Create sample for raw material
    /// </summary>
    [HttpPost("rawmaterials/{id}/samples")]
    public async Task<IActionResult> CreateSample(int id, [FromBody] CreateSampleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial == null)
            return NotFound(new { message = "Raw material not found" });

        var sample = new RawMaterialSample
        {
            RawMaterialId = id,
            LotNumber = dto.LotNumber,
            ReceivedDate = dto.ReceivedDate,
            ExpiryDate = dto.ExpiryDate,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.Set<RawMaterialSample>().Add(sample);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sample {Id} created for raw material {RawMaterialId} by user {UserId}", 
            sample.Id, id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { id = sample.Id, message = "Sample created successfully" });
    }

    /// <summary>
    /// Update sample
    /// </summary>
    [HttpPut("rawmaterials/{id}/samples/{sampleId}")]
    public async Task<IActionResult> UpdateSample(int id, int sampleId, [FromBody] UpdateSampleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var sample = await _context.Set<RawMaterialSample>()
            .FirstOrDefaultAsync(s => s.Id == sampleId && s.RawMaterialId == id);

        if (sample == null)
            return NotFound(new { message = "Sample not found" });

        sample.LotNumber = dto.LotNumber;
        sample.ReceivedDate = dto.ReceivedDate;
        sample.ExpiryDate = dto.ExpiryDate;
        sample.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Sample {Id} updated by user {UserId}", sampleId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Sample updated successfully" });
    }

    /// <summary>
    /// Delete sample
    /// </summary>
    [HttpDelete("rawmaterials/{id}/samples/{sampleId}")]
    public async Task<IActionResult> DeleteSample(int id, int sampleId)
    {
        var sample = await _context.Set<RawMaterialSample>()
            .FirstOrDefaultAsync(s => s.Id == sampleId && s.RawMaterialId == id);

        if (sample == null)
            return NotFound(new { message = "Sample not found" });

        sample.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sample {Id} deleted by user {UserId}", sampleId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Sample deleted successfully" });
    }

    #endregion

    #region Change Supplier

    /// <summary>
    /// Change supplier for a raw material
    /// </summary>
    [HttpPost("rawmaterials/{id}/change-supplier")]
    public async Task<IActionResult> ChangeSupplier(int id, [FromBody] ChangeSupplierDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial == null)
            return NotFound(new { message = "Raw material not found" });

        var newSupplier = await _context.Suppliers.FindAsync(dto.NewSupplierId);
        if (newSupplier == null)
            return NotFound(new { message = "New supplier not found" });

        var oldSupplierId = rawMaterial.SupplierId;
        rawMaterial.SupplierId = dto.NewSupplierId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Raw material {Id} supplier changed from {OldSupplierId} to {NewSupplierId} by user {UserId}. Reason: {Reason}", 
            id, oldSupplierId, dto.NewSupplierId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value, dto.Reason);

        return Ok(new { message = "Supplier changed successfully" });
    }

    #endregion

    #region Search Formulas by Raw Material

    /// <summary>
    /// Search formulas that use a specific raw material
    /// </summary>
    [HttpGet("rawmaterials/{id}/search-formulas")]
    public async Task<IActionResult> SearchFormulas(int id)
    {
        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial == null)
            return NotFound(new { message = "Raw material not found" });

        var formulas = await _context.FormulaRawMaterials
            .Where(frm => frm.RawMaterialId == id && frm.IsActive)
            .Include(frm => frm.Formula)
            .Select(frm => new
            {
                FormulaId = frm.FormulaId,
                frm.Formula!.FormulaCode,
                frm.Formula.ProductName,
                frm.Formula.ProductType,
                Percentage = frm.Percentage,
                frm.Formula.CreationDate
            })
            .OrderByDescending(f => f.CreationDate)
            .ToListAsync();

        return Ok(new
        {
            rawMaterialId = id,
            rawMaterialName = rawMaterial.ProductName,
            formulasCount = formulas.Count,
            formulas
        });
    }

    #endregion

    #region Restrictions CRUD

    /// <summary>
    /// Get all restrictions (paginated)
    /// </summary>
    [HttpGet("restrictions")]
    public async Task<IActionResult> GetRestrictions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.Restrictions.Include(r => r.INCI).Where(r => r.IsActive);
        var total = await query.CountAsync();
        
        var restrictions = await query
            .OrderBy(r => r.INCI!.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RestrictionDto
            {
                Id = r.Id,
                INCIId = r.INCIId,
                INCIName = r.INCI!.Name,
                Regulation = r.Regulation,
                Description = r.Description,
                MaxPercentage = r.MaxPercentage,
                IsRinseOffOnly = r.IsRinseOffOnly,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(new
        {
            data = restrictions,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get restriction by ID
    /// </summary>
    [HttpGet("restrictions/{id}")]
    public async Task<IActionResult> GetRestriction(int id)
    {
        var restriction = await _context.Restrictions
            .Include(r => r.INCI)
            .Where(r => r.Id == id)
            .Select(r => new RestrictionDto
            {
                Id = r.Id,
                INCIId = r.INCIId,
                INCIName = r.INCI!.Name,
                Regulation = r.Regulation,
                Description = r.Description,
                MaxPercentage = r.MaxPercentage,
                IsRinseOffOnly = r.IsRinseOffOnly,
                IsActive = r.IsActive
            })
            .FirstOrDefaultAsync();

        if (restriction == null)
            return NotFound(new { message = "Restriction not found" });

        return Ok(restriction);
    }

    /// <summary>
    /// Create new restriction
    /// </summary>
    [HttpPost("restrictions")]
    public async Task<IActionResult> CreateRestriction([FromBody] CreateRestrictionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var restriction = new Restriction
        {
            INCIId = dto.INCIId,
            Regulation = dto.Regulation,
            Description = dto.Description,
            MaxPercentage = dto.MaxPercentage,
            IsRinseOffOnly = dto.IsRinseOffOnly,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.Restrictions.Add(restriction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Restriction {Id} created by user {UserId}", restriction.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return CreatedAtAction(nameof(GetRestriction), new { id = restriction.Id }, new { id = restriction.Id, message = "Restriction created successfully" });
    }

    /// <summary>
    /// Update existing restriction
    /// </summary>
    [HttpPut("restrictions/{id}")]
    public async Task<IActionResult> UpdateRestriction(int id, [FromBody] UpdateRestrictionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var restriction = await _context.Restrictions.FindAsync(id);
        if (restriction == null)
            return NotFound(new { message = "Restriction not found" });

        restriction.INCIId = dto.INCIId;
        restriction.Regulation = dto.Regulation;
        restriction.Description = dto.Description;
        restriction.MaxPercentage = dto.MaxPercentage;
        restriction.IsRinseOffOnly = dto.IsRinseOffOnly;
        restriction.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Restriction {Id} updated by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Restriction updated successfully" });
    }

    /// <summary>
    /// Delete restriction (soft delete)
    /// </summary>
    [HttpDelete("restrictions/{id}")]
    public async Task<IActionResult> DeleteRestriction(int id)
    {
        var restriction = await _context.Restrictions.FindAsync(id);
        if (restriction == null)
            return NotFound(new { message = "Restriction not found" });

        restriction.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Restriction {Id} deleted by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Restriction deleted successfully" });
    }

    #endregion

    #region Formula Validation

    /// <summary>
    /// Validate formula by RA Head
    /// </summary>
    [HttpPost("formulas/{id}/validate")]
    public async Task<IActionResult> ValidateFormula(int id, [FromBody] ValidateFormulaDto? dto = null)
    {
        var formula = await _context.Formulas.FindAsync(id);
        if (formula == null)
            return NotFound(new { message = "Formula not found" });

        if (formula.ValidationDate.HasValue)
            return BadRequest(new { message = "Formula is already validated" });

        formula.ValidationDate = DateTime.Now;
        
        // Add comments if provided
        if (dto != null && !string.IsNullOrWhiteSpace(dto.Comments))
        {
            formula.Comments = formula.Comments != null 
                ? formula.Comments + "\n[Validation] " + dto.Comments 
                : "[Validation] " + dto.Comments;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Formula {Id} validated by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new 
        { 
            message = "Formula validated successfully", 
            validationDate = formula.ValidationDate 
        });
    }

    /// <summary>
    /// Invalidate formula (remove validation)
    /// </summary>
    [HttpPost("formulas/{id}/invalidate")]
    public async Task<IActionResult> InvalidateFormula(int id)
    {
        var formula = await _context.Formulas.FindAsync(id);
        if (formula == null)
            return NotFound(new { message = "Formula not found" });

        formula.ValidationDate = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Formula {Id} invalidated by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Formula invalidated successfully" });
    }

    #endregion

    #region WIP Management

    /// <summary>
    /// Toggle WIP (Work In Progress) flag for formula
    /// </summary>
    [HttpPost("formulas/{id}/wip")]
    public async Task<IActionResult> ToggleWIP(int id, [FromBody] ToggleWIPDto? dto = null)
    {
        var formula = await _context.Formulas.FindAsync(id);
        if (formula == null)
            return NotFound(new { message = "Formula not found" });

        formula.IsWIP = dto?.IsWIP ?? !formula.IsWIP;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Formula {Id} WIP toggled to {IsWIP} by user {UserId}", 
            id, formula.IsWIP, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new
        {
            message = $"Formula WIP set to {(formula.IsWIP ? "true" : "false")}",
            isWIP = formula.IsWIP
        });
    }

    #endregion

    #region Natural % Calculation (ISO 16128)

    /// <summary>
    /// Calculate natural percentage for formula based on ISO 16128
    /// </summary>
    [HttpGet("formulas/{id}/natural-calculation")]
    public async Task<IActionResult> CalculateNaturalPercentage(int id)
    {
        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials)
            .ThenInclude(frm => frm.RawMaterial)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formula == null)
            return NotFound(new { message = "Formula not found" });

        var ingredients = new List<NaturalIngredientDto>();
        decimal totalNatural = 0;

        foreach (var frm in formula.FormulaRawMaterials.Where(frm => frm.IsActive && frm.RawMaterial != null))
        {
            var percentage = frm.Percentage ?? 0;
            var naturalOrigin = frm.RawMaterial!.NaturalOriginPercentage ?? 0;
            var contribution = (percentage * naturalOrigin) / 100;

            ingredients.Add(new NaturalIngredientDto
            {
                IngredientName = frm.RawMaterial.ProductName,
                Percentage = percentage,
                NaturalOriginPercentage = naturalOrigin,
                ContributionToTotal = contribution
            });

            totalNatural += contribution;
        }

        var result = new NaturalCalculationDto
        {
            FormulaId = id,
            TotalNaturalPercentage = Math.Round(totalNatural, 2),
            Ingredients = ingredients.OrderByDescending(i => i.ContributionToTotal).ToList()
        };

        return Ok(result);
    }

    #endregion

    #region Auto Water Calculation

    /// <summary>
    /// Calculate required water percentage for formula
    /// </summary>
    [HttpGet("formulas/{id}/water-calculation")]
    public async Task<IActionResult> CalculateWater(int id)
    {
        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formula == null)
            return NotFound(new { message = "Formula not found" });

        var totalRawMaterial = formula.CalculateTotalRawMaterialPercentage();
        var waterNeeded = formula.CalculateRequiredWaterPercentage();
        var status = formula.GetWaterPercentageStatus();

        return Ok(new
        {
            formulaId = id,
            totalRawMaterialPercentage = Math.Round(totalRawMaterial, 2),
            waterPercentageNeeded = Math.Round(waterNeeded, 2),
            status,
            isValid = formula.IsRawMaterialPercentageValid(),
            canBeAnhydrous = formula.CanBeAnhydrous()
        });
    }

    #endregion
}

// Additional DTOs
public class ToggleWIPDto
{
    public bool IsWIP { get; set; }
}
