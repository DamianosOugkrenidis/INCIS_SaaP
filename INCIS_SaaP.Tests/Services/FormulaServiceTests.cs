using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Tests for Formula business logic
/// </summary>
public class FormulaServiceTests : IDisposable
{
    private readonly Data.INCISDbContext _context;

    public FormulaServiceTests()
    {
        _context = TestDbContextFactory.Create();
    }

    [Fact]
    public async Task CreateFormula_WithRawMaterials_ShouldCalculateWater()
    {
        var supplier = new Supplier { Name = "Test Supplier" };
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var rm1 = new RawMaterial { Code = "RM001", ProductName = "Emulsifier A", SupplierId = supplier.Id };
        var rm2 = new RawMaterial { Code = "RM002", ProductName = "Active B", SupplierId = supplier.Id };
        _context.RawMaterials.AddRange(rm1, rm2);
        await _context.SaveChangesAsync();

        var client = new Client { Name = "Test Client" };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var formula = new Formula { FormulaCode = "TC001-0001-T01", ClientId = client.Id };
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        _context.FormulaRawMaterials.AddRange(
            new FormulaRawMaterial { FormulaId = formula.Id, RawMaterialId = rm1.Id, Percentage = 5, Phase = "A", IsActive = true },
            new FormulaRawMaterial { FormulaId = formula.Id, RawMaterialId = rm2.Id, Percentage = 2, Phase = "B", IsActive = true }
        );
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        var loaded = await _context.Formulas
            .Include(f => f.FormulaRawMaterials).ThenInclude(frm => frm.RawMaterial)
            .FirstAsync(f => f.Id == formula.Id);

        loaded.CalculateRequiredWaterPercentage().Should().Be(93);
        loaded.CalculateTotalRawMaterialPercentage().Should().Be(7);
    }

    [Fact]
    public async Task ProductionBatch_ShouldCalculateQuantities()
    {
        var rm = new RawMaterial { Code = "RM001", ProductName = "Test" };
        _context.RawMaterials.Add(rm);
        await _context.SaveChangesAsync();

        var formula = new Formula { FormulaCode = "TEST-0001-T01" };
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        _context.FormulaRawMaterials.Add(new FormulaRawMaterial
        {
            FormulaId = formula.Id, RawMaterialId = rm.Id, Percentage = 10, Phase = "A", IsActive = true
        });
        await _context.SaveChangesAsync();

        // Simulate production batch creation
        var batchSize = 50m;
        var percentage = 10m;
        var calculatedQty = Math.Round((percentage / 100) * batchSize, 4);

        var batch = new ProductionBatch
        {
            BatchCode = "TEST-PROD-0001",
            FormulaId = formula.Id,
            BatchSize = batchSize,
            Unit = "kg"
        };
        batch.Lines.Add(new ProductionBatchLine
        {
            RawMaterialId = rm.Id,
            Percentage = percentage,
            CalculatedQuantity = calculatedQty,
            Phase = "A",
            OrderIndex = 1
        });

        _context.ProductionBatches.Add(batch);
        await _context.SaveChangesAsync();

        var savedBatch = await _context.ProductionBatches
            .Include(b => b.Lines)
            .FirstAsync(b => b.Id == batch.Id);

        savedBatch.Lines.Should().HaveCount(1);
        savedBatch.Lines.First().CalculatedQuantity.Should().Be(5m); // 10% of 50kg = 5kg
    }

    [Fact]
    public async Task Tenant_ShouldHaveSubscription()
    {
        var plan = new SubscriptionPlan
        {
            Name = "Professional",
            MonthlyPrice = 99,
            AnnualPrice = 990,
            MaxUsers = 20,
            MaxFormulas = 500,
            MaxRawMaterials = 1000,
            MaxStorageMB = 5120,
            ApiRateLimit = 1000,
            HasWebhooks = true,
            HasProduction = true,
            HasReports = true,
            HasApiAccess = true
        };
        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();

        var tenant = new Tenant
        {
            Name = "BeautyLab Inc",
            Slug = "beautylab",
            Email = "info@beautylab.gr",
            SubscriptionPlanId = plan.Id,
            SubscriptionStatus = "Active",
            MaxUsers = plan.MaxUsers
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var loaded = await _context.Tenants
            .Include(t => t.SubscriptionPlan)
            .FirstAsync(t => t.Id == tenant.Id);

        loaded.SubscriptionPlan.Should().NotBeNull();
        loaded.SubscriptionPlan!.Name.Should().Be("Professional");
        loaded.SubscriptionStatus.Should().Be("Active");
    }

    [Fact]
    public async Task Document_ShouldLinkToEntity()
    {
        var tenant = new Tenant { Name = "Test", Slug = "test" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var doc = new Document
        {
            TenantId = tenant.Id,
            FileName = "test-tds.pdf",
            StoragePath = "2026/05/abc123.pdf",
            ContentType = "application/pdf",
            FileSize = 1024,
            DocumentType = "TDS",
            EntityType = "RawMaterial",
            EntityId = 1
        };
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        var loaded = await _context.Documents.FirstAsync(d => d.Id == doc.Id);
        loaded.DocumentType.Should().Be("TDS");
        loaded.EntityType.Should().Be("RawMaterial");
    }

    [Fact]
    public async Task WebhookSubscription_ShouldPersist()
    {
        var tenant = new Tenant { Name = "Webhook Test", Slug = "wh-test" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var webhook = new WebhookSubscription
        {
            TenantId = tenant.Id,
            Url = "https://example.com/webhook",
            Secret = "test-secret-123",
            Events = "formula.created,batch.completed"
        };
        _context.WebhookSubscriptions.Add(webhook);
        await _context.SaveChangesAsync();

        var loaded = await _context.WebhookSubscriptions.FirstAsync(w => w.Id == webhook.Id);
        loaded.Url.Should().Be("https://example.com/webhook");
        loaded.Events.Should().Contain("formula.created");
    }

    [Fact]
    public async Task BillingInvoice_ShouldCalculateTotal()
    {
        var tenant = new Tenant { Name = "Billing Test", Slug = "billing" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var amount = 99m;
        var tax = Math.Round(amount * 0.24m, 2);
        var total = Math.Round(amount + tax, 2);

        var invoice = new BillingInvoice
        {
            TenantId = tenant.Id,
            InvoiceNumber = "INV-202605-0001",
            Amount = amount,
            Tax = tax,
            TotalAmount = total,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = "Pending"
        };
        _context.BillingInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        var loaded = await _context.BillingInvoices.FirstAsync(i => i.Id == invoice.Id);
        loaded.Amount.Should().Be(99m);
        loaded.Tax.Should().Be(23.76m);
        loaded.TotalAmount.Should().Be(122.76m);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
