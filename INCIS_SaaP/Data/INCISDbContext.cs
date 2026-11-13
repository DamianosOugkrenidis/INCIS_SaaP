// Author: Damianos Pythagoras Ougkrenidis

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
// Advanced Architecture & Security
using INCIS_SaaP.Architecture.EventSourcing;
using INCIS_SaaP.Architecture.OutboxPattern;
using INCIS_SaaP.Infrastructure.Encryption;
using INCIS_SaaP.Infrastructure.Privacy;
using INCIS_SaaP.Infrastructure.Security.ABAC;
using INCIS_SaaP.Infrastructure.Security.DLP;
using INCIS_SaaP.Infrastructure.Security.ZeroTrust;
// ML & AI
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.ML.RegulatoryIntelligence;
using INCIS_SaaP.ML.QualityPrediction;
using INCIS_SaaP.ML.SemanticSearch;
using INCIS_SaaP.ML.DocumentIntelligence;

namespace INCIS_SaaP.Data;

public class INCISDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    // Multi-tenant isolation context — υπολογίζεται LAZILY από το τρέχον HttpContext,
    // ανεξάρτητα από τη σειρά κατασκευής του DbContext στο pipeline (το TenantId τίθεται
    // από το TenantMiddleware στο HttpContext.Items μετά το auth). Τα EF global query
    // filters αξιολογούνται per-query και διαβάζουν τις τρέχουσες τιμές.
    private string? _currentUserId => _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    private int? _currentTenantId => _httpContextAccessor?.HttpContext?.Items["TenantId"] as int?;
    private bool _isPrivileged
    {
        get
        {
            var u = _httpContextAccessor?.HttpContext?.User;
            return u?.Identity?.IsAuthenticated == true
                   && (u.IsInRole(AppRoles.Admin) || u.IsInRole(AppRoles.SuperUser));
        }
    }

    public INCISDbContext(DbContextOptions<INCISDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Formula> Formulas { get; set; }
    public DbSet<INCIIngredient> INCIIngredients { get; set; }
    public DbSet<SafetyData> SafetyData { get; set; }
    public DbSet<PriceInfo> PriceInfos { get; set; }
    public DbSet<Allergen> Allergens { get; set; }
    public DbSet<HeavyMetal> HeavyMetals { get; set; }
    public DbSet<MarketingInfo> MarketingInfos { get; set; }
    public DbSet<RawMaterial> RawMaterials { get; set; }
    public DbSet<FormulaRawMaterial> FormulaRawMaterials { get; set; }
    public DbSet<NaturalCertification> NaturalCertifications { get; set; }
    public DbSet<INCIS_SaaP.Models.Claim> Claims { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }
    
    // New models from manual
    public DbSet<Client> Clients { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<INCI> INCIs { get; set; }
    public DbSet<Restriction> Restrictions { get; set; }

    // Toxicological & Claims
    public DbSet<ToxicologicalData> ToxicologicalData { get; set; }
    public DbSet<RawMaterialClaim> RawMaterialClaims { get; set; }

    // Production
    public DbSet<ProductionBatch> ProductionBatches { get; set; }
    public DbSet<ProductionBatchLine> ProductionBatchLines { get; set; }

    // Feature Flags
    public DbSet<INCIS_SaaP.Services.FeatureFlag> FeatureFlags { get; set; }

    // Multi-tenancy & SaaS
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<BillingInvoice> BillingInvoices { get; set; }

    // Documents
    public DbSet<Document> Documents { get; set; }

    // Webhooks
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }

    // CosIng Database (EU Regulation 1223/2009)
    public DbSet<CosIngEntry> CosIngEntries { get; set; }
    public DbSet<CosIngIngredient> CosIngIngredients { get; set; }

    // Formula Version Control & Templates
    public DbSet<FormulaVersion> FormulaVersions { get; set; }
    public DbSet<FormulaVersionDetail> FormulaVersionDetails { get; set; }
    public DbSet<FormulaVersionComparison> FormulaVersionComparisons { get; set; }
    public DbSet<TemplateCategory> TemplateCategories { get; set; }
    public DbSet<FormulaTemplate> FormulaTemplates { get; set; }
    public DbSet<FormulaTemplateUsage> FormulaTemplateUsages { get; set; }
    public DbSet<TemplateRawMaterial> TemplateRawMaterials { get; set; }

    // Formula Approval Workflow
    public DbSet<FormulaApproval> FormulaApprovals { get; set; }
    public DbSet<ApprovalStep> ApprovalSteps { get; set; }
    public DbSet<ApprovalComment> ApprovalComments { get; set; }
    public DbSet<ApprovalWorkflowTemplate> ApprovalWorkflowTemplates { get; set; }

    // ============================================================
    // ADVANCED ARCHITECTURE - Event Sourcing & Outbox
    // ============================================================
    public DbSet<StoredEvent> StoredEvents { get; set; }
    public DbSet<AggregateSnapshot> AggregateSnapshots { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    // ============================================================
    // DATA PROTECTION - Encryption & Tokenization
    // ============================================================
    public DbSet<EncryptionKeyMetadata> EncryptionKeys { get; set; }
    public DbSet<TokenVaultEntry> TokenVault { get; set; }

    // ============================================================
    // ACCESS CONTROL - ABAC
    // ============================================================
    public DbSet<AbacPolicy> AbacPolicies { get; set; }
    public DbSet<UserSecurityAttribute> UserSecurityAttributes { get; set; }
    public DbSet<BreakGlassAccessRecord> BreakGlassAccessRecords { get; set; }

    // ============================================================
    // SECURITY - DLP & Zero Trust
    // ============================================================
    public DbSet<DlpPolicy> DlpPolicies { get; set; }
    public DbSet<DlpExportRecord> DlpExportRecords { get; set; }
    public DbSet<ServiceIdentity> ServiceIdentities { get; set; }
    public DbSet<TrustVerificationRecord> TrustVerificationRecords { get; set; }

    // ============================================================
    // PRIVACY - GDPR Compliance
    // ============================================================
    public DbSet<ConsentRecord> ConsentRecords { get; set; }
    public DbSet<DataClassification> DataClassifications { get; set; }
    public DbSet<ErasureRequest> ErasureRequests { get; set; }
    public DbSet<PrivacyImpactAssessment> PrivacyImpactAssessments { get; set; }

    // ============================================================
    // ML - Formula Intelligence
    // ============================================================
    public DbSet<FormulaTrainingRecord> FormulaTrainingRecords { get; set; }
    public DbSet<FormulaCombinationLog> FormulaCombinationLogs { get; set; }
    public DbSet<IngredientCompatibility> IngredientCompatibilities { get; set; }

    // ============================================================
    // ML - Regulatory Intelligence
    // ============================================================
    public DbSet<RegulatoryChange> RegulatoryChanges { get; set; }
    public DbSet<AllergenRiskAssessment> AllergenRiskAssessments { get; set; }
    public DbSet<INCISynonym> INCISynonyms { get; set; }

    // ============================================================
    // ML - Quality & Production
    // ============================================================
    public DbSet<BatchQualityRecord> BatchQualityRecords { get; set; }
    public DbSet<ShelfLifeRecord> ShelfLifeRecords { get; set; }
    public DbSet<AnomalyRecord> AnomalyRecords { get; set; }
    public DbSet<ImageQCRecord> ImageQCRecords { get; set; }

    // ============================================================
    // ML - Semantic Search & Document Intelligence
    // ============================================================
    public DbSet<EmbeddingRecord> EmbeddingRecords { get; set; }
    public DbSet<ExtractedDocumentData> ExtractedDocumentData { get; set; }

    /// <summary>
    /// Αναθέτει αυτόματα τον τρέχοντα χρήστη ως ιδιοκτήτη σε νέες οντότητες
    /// (per-user data isolation), εφόσον δεν έχει οριστεί ήδη.
    /// </summary>
    private void ApplyOwnership()
    {
        // Δημιουργός (audit)
        if (!string.IsNullOrEmpty(_currentUserId))
        {
            foreach (var entry in this.ChangeTracker.Entries<IOwnedEntity>()
                         .Where(e => e.State == EntityState.Added && string.IsNullOrEmpty(e.Entity.OwnerId)))
            {
                entry.Entity.OwnerId = _currentUserId;
            }
        }

        // Tenant (multi-tenant isolation)
        if (_currentTenantId.HasValue)
        {
            foreach (var entry in this.ChangeTracker.Entries<ITenantOwned>()
                         .Where(e => e.State == EntityState.Added && e.Entity.TenantId == null))
            {
                entry.Entity.TenantId = _currentTenantId;
            }
        }
    }

    private void ApplyAuditLogRules()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        var userId = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = httpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

        var entries = this.ChangeTracker.Entries()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                        && e.Entity.GetType() != typeof(AuditLog))
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                EntityName = entry.Metadata.Name,
                EntityId = GetPrimaryKeyValue(entry) ?? string.Empty,
                Action = entry.State switch
                {
                    EntityState.Added => AuditAction.Created.ToString(),
                    EntityState.Modified => AuditAction.Updated.ToString(),
                    EntityState.Deleted => AuditAction.Deleted.ToString(),
                    _ => throw new ArgumentOutOfRangeException()
                },
                UserId = userId,
                UserEmail = userEmail,
                OldValues = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                    ? JsonSerializer.Serialize(entry.OriginalValues.ToObject())
                    : null,
                NewValues = entry.State == EntityState.Added || entry.State == EntityState.Modified
                    ? JsonSerializer.Serialize(entry.CurrentValues.ToObject())
                    : null,
                Timestamp = DateTime.UtcNow
            };

            if (httpContext != null)
            {
                auditLog.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                auditLog.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
            }

            this.Add(auditLog);
        }
    }

    private string? GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null) return null;
        var keyProperty = key.Properties.FirstOrDefault();
        if (keyProperty == null) return null;
        var value = entry.Property(keyProperty.Name).CurrentValue;
        return value?.ToString();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyOwnership();
        ApplyAuditLogRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyOwnership();
        ApplyAuditLogRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============ Global decimal precision (avoid silent truncation warnings) ============
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(4);
                }
            }
        }

        // ============ Multi-tenant data isolation (global query filters) ============
        // Κάθε tenant (εταιρεία) βλέπει μόνο τα δικά του δεδομένα. Οι Admin/SuperUser
        // και τα background tasks (χωρίς tenant context) βλέπουν τα πάντα.
        // Εγγραφές με TenantId == null είναι κοινές/legacy (ορατές σε όλους).
        modelBuilder.Entity<Formula>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.TenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<Client>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.TenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<RawMaterial>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.TenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.TenantId == null || e.TenantId == _currentTenantId);

        // Υπο-δεδομένα φόρμουλας: κληρονομούν την απομόνωση μέσω της Formula (αποτροπή IDOR).
        modelBuilder.Entity<Allergen>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<HeavyMetal>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<INCIIngredient>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<INCIS_SaaP.Models.Claim>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<ToxicologicalData>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<MarketingInfo>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<PriceInfo>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<NaturalCertification>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<SafetyData>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        modelBuilder.Entity<FormulaRawMaterial>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);

        // Documents & Webhooks: strict tenant isolation (no legacy sharing) — prevention of cross-tenant IDOR.
        modelBuilder.Entity<Document>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.TenantId == _currentTenantId);
        modelBuilder.Entity<WebhookSubscription>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.TenantId == _currentTenantId);
        modelBuilder.Entity<WebhookDelivery>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.Subscription!.TenantId == _currentTenantId);
        modelBuilder.Entity<BillingInvoice>().HasQueryFilter(e =>
            _currentTenantId == null || _isPrivileged || e.TenantId == _currentTenantId);

        // Configure ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Email);
        });

         // Formula configuration with indexes
         modelBuilder.Entity<Formula>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.FormulaCode).IsRequired().HasMaxLength(100);
             entity.HasIndex(e => e.FormulaCode).IsUnique();
             entity.HasIndex(e => e.ProductName); // Changed from FormulaName to ProductName
             entity.HasIndex(e => e.ProductType);
             entity.HasIndex(e => e.CreationDate);
             
             // Configure FormulaRawMaterials (join entity)
             entity.HasMany(e => e.FormulaRawMaterials)
                   .WithOne(frm => frm.Formula)
                   .HasForeignKey(frm => frm.FormulaId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

        // INCIIngredient configuration
        modelBuilder.Entity<INCIIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Formula)
                  .WithMany(f => f.INCIIngredients)
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.INCIName);
        });

        // SafetyData configuration
        modelBuilder.Entity<SafetyData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Formula)
                  .WithOne(f => f.SafetyData)
                  .HasForeignKey<SafetyData>(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.FormulaId);
        });

        // PriceInfo configuration
        modelBuilder.Entity<PriceInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Formula)
                  .WithOne(f => f.PriceInfo)
                  .HasForeignKey<PriceInfo>(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Allergen configuration
        modelBuilder.Entity<Allergen>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Formula)
                  .WithMany(f => f.Allergens)
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.AllergenName);
        });

        // HeavyMetal configuration
        modelBuilder.Entity<HeavyMetal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Formula)
                  .WithMany(f => f.HeavyMetals)
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MarketingInfo configuration
        modelBuilder.Entity<MarketingInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Formula)
                  .WithOne(f => f.MarketingInfo)
                  .HasForeignKey<MarketingInfo>(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

         // RawMaterial configuration
         modelBuilder.Entity<RawMaterial>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
             entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
             entity.HasIndex(e => e.Code);
             entity.HasIndex(e => e.ProductName);

             // Configure RawMaterialComposition (matches navigation property RawMaterial)
             entity.HasMany(e => e.Composition)
                   .WithOne(rc => rc.RawMaterial)
                   .HasForeignKey(rc => rc.RawMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);

             // Configure RawMaterialSample (matches navigation property RawMaterial)
             entity.HasMany(e => e.Samples)
                   .WithOne(s => s.RawMaterial)
                   .HasForeignKey(s => s.RawMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);

             // Configure FormulaRawMaterial (join entity)
             entity.HasMany(e => e.FormulaRawMaterials)
                   .WithOne(frm => frm.RawMaterial)
                   .HasForeignKey(frm => frm.RawMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

        // NaturalCertification configuration
        modelBuilder.Entity<NaturalCertification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Formula)
                  .WithOne(f => f.NaturalCertification)
                  .HasForeignKey<NaturalCertification>(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Client configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Code);
            
            // Relationship with Formulas
            entity.HasMany(e => e.Formulas)
                  .WithOne(f => f.Client)
                  .HasForeignKey(f => f.ClientId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Supplier configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name);

            // Relationship with RawMaterials (uses navigation Supplier on RawMaterial)
            entity.HasMany(e => e.RawMaterials)
                  .WithOne(rm => rm.Supplier)
                  .HasForeignKey(rm => rm.SupplierId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // INCI configuration
        modelBuilder.Entity<INCI>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CASNumber);
            entity.HasIndex(e => e.ECNumber);

            // RawMaterial.MainINCI -> INCI.RawMaterials (uses RawMaterial.INCIId)
            entity.HasMany(e => e.RawMaterials)
                  .WithOne(rm => rm.MainINCI)
                  .HasForeignKey(rm => rm.INCIId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Restrictions defined explicitly via Restriction.INCI navigation below; skip here.
        });

        // Restriction configuration
        modelBuilder.Entity<Restriction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.INCI)
                  .WithMany(i => i.Restrictions)
                  .HasForeignKey(e => e.INCIId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Regulation).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Regulation);
        });

         // Claim configuration
         modelBuilder.Entity<INCIS_SaaP.Models.Claim>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.HasOne(e => e.Formula)
                   .WithMany(f => f.Claims)
                   .HasForeignKey(e => e.FormulaId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

         // ToxicologicalData configuration
         modelBuilder.Entity<ToxicologicalData>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.HasOne(e => e.Formula)
                   .WithMany(f => f.ToxicologicalData)
                   .HasForeignKey(e => e.FormulaId)
                   .OnDelete(DeleteBehavior.Cascade);
             entity.HasIndex(e => e.FormulaId);
             entity.HasIndex(e => e.InciName);
         });

         // RawMaterialClaim configuration
         modelBuilder.Entity<RawMaterialClaim>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.HasOne(e => e.RawMaterial)
                   .WithOne(rm => rm.Claims)
                   .HasForeignKey<RawMaterialClaim>(e => e.RawMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);
             entity.HasIndex(e => e.RawMaterialId).IsUnique();
         });

         // SystemSettings configuration
         modelBuilder.Entity<SystemSettings>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.RawMaterialPassword).HasMaxLength(100);
             entity.Property(e => e.FormulasPassword).HasMaxLength(100);
         });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
        });

        // FeatureFlag configuration
        modelBuilder.Entity<INCIS_SaaP.Services.FeatureFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.SubscriptionPlan).WithMany().HasForeignKey(e => e.SubscriptionPlanId).OnDelete(DeleteBehavior.SetNull);
        });

        // SubscriptionPlan configuration
        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // BillingInvoice configuration
        modelBuilder.Entity<BillingInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        // WebhookSubscription configuration
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        // WebhookDelivery configuration
        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WebhookSubscriptionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Subscription).WithMany().HasForeignKey(e => e.WebhookSubscriptionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationUser -> Tenant relationship
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(e => e.Tenant).WithMany(t => t.Users).HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.SetNull);
        });

        // ProductionBatch configuration
        modelBuilder.Entity<ProductionBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchCode).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.BatchCode).IsUnique();
            entity.HasIndex(e => e.ProductionDate);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Formula)
                  .WithMany()
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Lines)
                  .WithOne(l => l.ProductionBatch)
                  .HasForeignKey(l => l.ProductionBatchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductionBatchLine configuration
        modelBuilder.Entity<ProductionBatchLine>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.RawMaterial)
                  .WithMany()
                  .HasForeignKey(e => e.RawMaterialId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============ CosIng Database Tables (EU Regulation 1223/2009) ============

        // CosIngEntry - Παραρτήματα II-VI
        modelBuilder.Entity<CosIngEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CosIngEntries");

            entity.HasIndex(e => e.Annex);
            entity.HasIndex(e => e.CASNumber);
            entity.HasIndex(e => e.ECNumber);
            entity.HasIndex(e => e.InciName);
            entity.HasIndex(e => e.EntryNumber);
            entity.HasIndex(e => new { e.Annex, e.EntryNumber });
        });

        // CosIngIngredient - Inventory INCI substances
        modelBuilder.Entity<CosIngIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CosIngIngredients");

            entity.HasIndex(e => e.InciName);
            entity.HasIndex(e => e.CASNumber);
            entity.HasIndex(e => e.ECNumber);
            entity.HasIndex(e => e.CosIngRefNo);
            entity.HasIndex(e => e.Function);
        });

        // ============================================================
        // ADVANCED ARCHITECTURE - Event Sourcing
        // ============================================================
        modelBuilder.Entity<StoredEvent>(entity =>
        {
            entity.HasKey(e => e.SequenceNumber);
            entity.Property(e => e.SequenceNumber).ValueGeneratedOnAdd();
            entity.ToTable("EventStore");
            entity.HasIndex(e => new { e.AggregateType, e.AggregateId, e.Version }).IsUnique();
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.OccurredOn);
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<AggregateSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AggregateSnapshots");
            entity.HasIndex(e => new { e.AggregateType, e.AggregateId });
        });

        // Outbox Pattern
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.ToTable("OutboxMessages");
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });

        // ============================================================
        // DATA PROTECTION - Encryption Keys & Token Vault
        // ============================================================
        modelBuilder.Entity<EncryptionKeyMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EncryptionKeys");
            entity.HasIndex(e => e.KeyId).IsUnique();
            entity.HasIndex(e => new { e.Purpose, e.IsActive });
        });

        modelBuilder.Entity<TokenVaultEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("TokenVault");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        // ============================================================
        // ACCESS CONTROL - ABAC
        // ============================================================
        modelBuilder.Entity<AbacPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AbacPolicies");
            entity.HasIndex(e => new { e.ResourceType, e.Action });
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<UserSecurityAttribute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserSecurityAttributes");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.AttributeName });
        });

        modelBuilder.Entity<BreakGlassAccessRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("BreakGlassAccessRecords");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AccessedAt);
        });

        // ============================================================
        // SECURITY - DLP & Zero Trust
        // ============================================================
        modelBuilder.Entity<DlpPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DlpPolicies");
            entity.HasIndex(e => new { e.ResourceType, e.ActionType });
        });

        modelBuilder.Entity<DlpExportRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DlpExportRecords");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExportedAt);
            entity.HasIndex(e => e.WatermarkId);
        });

        modelBuilder.Entity<ServiceIdentity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ServiceIdentities");
            entity.HasIndex(e => e.ServiceId).IsUnique();
        });

        modelBuilder.Entity<TrustVerificationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("TrustVerificationRecords");
            entity.HasIndex(e => e.RequestorId);
            entity.HasIndex(e => e.VerifiedAt);
        });

        // ============================================================
        // PRIVACY - GDPR
        // ============================================================
        modelBuilder.Entity<ConsentRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ConsentRecords");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.DataCategory, e.Purpose });
        });

        modelBuilder.Entity<DataClassification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DataClassifications");
            entity.HasIndex(e => new { e.EntityType, e.FieldName }).IsUnique();
        });

        modelBuilder.Entity<ErasureRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ErasureRequests");
            entity.HasIndex(e => e.SubjectUserId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<PrivacyImpactAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PrivacyImpactAssessments");
            entity.HasIndex(e => e.FeatureName);
        });

        // ============================================================
        // ML - Formula Intelligence
        // ============================================================
        modelBuilder.Entity<FormulaTrainingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_FormulaTrainingRecords");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.ProductType);
            entity.HasIndex(e => e.PassedOverall);
        });

        modelBuilder.Entity<FormulaCombinationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_FormulaCombinationLogs");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.ProductType);
            entity.HasIndex(e => e.LoggedAt);
        });

        modelBuilder.Entity<IngredientCompatibility>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_IngredientCompatibility");
            entity.HasIndex(e => new { e.RawMaterial1Id, e.RawMaterial2Id }).IsUnique();
        });

        // ============================================================
        // ML - Regulatory Intelligence
        // ============================================================
        modelBuilder.Entity<RegulatoryChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_RegulatoryChanges");
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.DetectedAt);
        });

        modelBuilder.Entity<AllergenRiskAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_AllergenRiskAssessments");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.RiskLevel);
        });

        modelBuilder.Entity<INCISynonym>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_INCISynonyms");
            entity.HasIndex(e => e.INCIId);
            entity.HasIndex(e => e.Synonym);
            entity.HasIndex(e => e.INCIName);
        });

        // ============================================================
        // ML - Quality & Production
        // ============================================================
        modelBuilder.Entity<BatchQualityRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_BatchQualityRecords");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.ProductionBatchId);
            entity.HasIndex(e => e.PassedQC);
        });

        modelBuilder.Entity<ShelfLifeRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_ShelfLifeRecords");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.ProductType);
        });

        modelBuilder.Entity<AnomalyRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_AnomalyRecords");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.Parameter);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.DetectedAt);
        });

        modelBuilder.Entity<ImageQCRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_ImageQCRecords");
            entity.HasIndex(e => e.ProductionBatchId);
        });

        // ============================================================
        // ML - Semantic Search & Document Intelligence
        // ============================================================
        modelBuilder.Entity<EmbeddingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_EmbeddingRecords");
            entity.HasIndex(e => new { e.EntityType, e.EntityId }).IsUnique();
        });

        modelBuilder.Entity<ExtractedDocumentData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ML_ExtractedDocumentData");
            entity.HasIndex(e => e.RawMaterialId);
            entity.HasIndex(e => e.DocumentType);
        });

        // ============================================================
        // FORMULA VERSION CONTROL
        // ============================================================
        modelBuilder.Entity<FormulaVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FormulaVersions");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => new { e.FormulaId, e.VersionNumber }).IsUnique();
            entity.HasOne(e => e.Formula)
                  .WithMany()
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Tenant isolation μέσω της Formula (αποτροπή cross-tenant διαρροής εκδόσεων)
            entity.HasQueryFilter(e =>
                _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        });

        modelBuilder.Entity<FormulaVersionDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FormulaVersionDetails");
            entity.HasIndex(e => e.FormulaVersionId);
            entity.HasOne(e => e.FormulaVersion)
                  .WithMany()
                  .HasForeignKey(e => e.FormulaVersionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e =>
                _currentTenantId == null || _isPrivileged
                || e.FormulaVersion!.Formula!.TenantId == null
                || e.FormulaVersion.Formula.TenantId == _currentTenantId);
        });

        modelBuilder.Entity<FormulaVersionComparison>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FormulaVersionComparisons");
            entity.HasIndex(e => e.FormulaId);
        });

        // ============================================================
        // FORMULA TEMPLATES
        // ============================================================
        modelBuilder.Entity<TemplateCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("TemplateCategories");
            entity.HasIndex(e => e.Name);
            entity.HasOne(e => e.ParentCategory)
                  .WithMany(e => e.SubCategories)
                  .HasForeignKey(e => e.ParentCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FormulaTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FormulaTemplates");
            entity.HasIndex(e => e.TemplateCode);
            entity.HasIndex(e => e.ProductType);
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Templates)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.TemplateUsages)
                  .WithOne(u => u.Template)
                  .HasForeignKey(u => u.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FormulaTemplateUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FormulaTemplateUsages");
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.FormulaId);
            // Restrict: αποφυγή πολλαπλών cascade paths (Formula & Template -> Usage)
            entity.HasOne(e => e.Formula)
                  .WithMany()
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        });

        modelBuilder.Entity<TemplateRawMaterial>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("TemplateRawMaterials");
            entity.HasIndex(e => e.TemplateId);
            entity.HasOne(e => e.Template)
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RawMaterial)
                  .WithMany()
                  .HasForeignKey(e => e.RawMaterialId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================================================
        // FORMULA APPROVAL WORKFLOW
        // ============================================================
        modelBuilder.Entity<FormulaApproval>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FormulaApprovals");
            entity.HasIndex(e => e.FormulaId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ApprovalRequestCode).IsUnique();

            // Restrict: αποφυγή πολλαπλών cascade paths (Formula -> Approval & Formula -> Version -> Approval)
            entity.HasOne(e => e.Formula)
                  .WithMany()
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FormulaVersion)
                  .WithMany()
                  .HasForeignKey(e => e.FormulaVersionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ApprovalSteps)
                  .WithOne(s => s.FormulaApproval)
                  .HasForeignKey(s => s.FormulaApprovalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Comments)
                  .WithOne(c => c.FormulaApproval)
                  .HasForeignKey(c => c.FormulaApprovalId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Tenant isolation μέσω της Formula
            entity.HasQueryFilter(e =>
                _currentTenantId == null || _isPrivileged || e.Formula!.TenantId == null || e.Formula.TenantId == _currentTenantId);
        });

        modelBuilder.Entity<ApprovalStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ApprovalSteps");
            entity.HasIndex(e => e.FormulaApprovalId);
            entity.HasIndex(e => new { e.FormulaApprovalId, e.StepNumber });

            entity.HasQueryFilter(e =>
                _currentTenantId == null || _isPrivileged
                || e.FormulaApproval!.Formula!.TenantId == null
                || e.FormulaApproval.Formula.TenantId == _currentTenantId);
        });

        modelBuilder.Entity<ApprovalComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ApprovalComments");
            entity.HasIndex(e => e.FormulaApprovalId);

            // Restrict: το ApprovalComment φτάνει ήδη cascade μέσω FormulaApproval
            entity.HasOne(e => e.ApprovalStep)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovalStepId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Self-reference για threaded replies
            entity.HasOne(e => e.ParentComment)
                  .WithMany(e => e.Replies)
                  .HasForeignKey(e => e.ParentCommentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentTenantId == null || _isPrivileged
                || e.FormulaApproval!.Formula!.TenantId == null
                || e.FormulaApproval.Formula.TenantId == _currentTenantId);
        });

        modelBuilder.Entity<ApprovalWorkflowTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ApprovalWorkflowTemplates");
            entity.HasIndex(e => e.TemplateName);
            entity.HasIndex(e => e.ProductType);
        });
    }
}
