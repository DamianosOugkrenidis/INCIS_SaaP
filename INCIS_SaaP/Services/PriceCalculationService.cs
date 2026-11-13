// Author: Damianos Pythagoras Ougkrenidis
//
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using Microsoft.EntityFrameworkCore;

namespace INCIS_SaaP.Services;

/// <summary>
/// Υπηρεσία υπολογισμού τιμών και κόστους
/// </summary>
public class PriceCalculationService
{
    private readonly INCISDbContext _context;

    // Προκαθορισμένα ποσοστά περιθωρίου κέρδους ανά κατηγορία
    private static readonly Dictionary<string, decimal> DEFAULT_MARGINS = new()
    {
        { "Economy", 0.15m },      // 15%
        { "Standard", 0.25m },      // 25%
        { "Premium", 0.40m },      // 40%
        { "Luxury", 0.60m },       // 60%
        { "Professional", 0.35m },   // 35%
        { "Default", 0.30m }        // 30%
    };

    // Φόροι ανά χώρα (παράδειγμα για Ελλάδα)
    private static readonly Dictionary<string, decimal> VAT_RATES = new()
    {
        { "GR", 0.24m },           // 24% Ελλάδα
        { "CY", 0.19m },           // 19% Κύπρος
        { "EU", 0.21m },           // Μέσος ΕΕ
        { "US", 0.08m },           // Μέσος ΗΠΑ
        { "UK", 0.20m }            // 20% UK
    };

    public PriceCalculationService(INCISDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Υπολογίζει τις τιμές για μια φόρμουλα
    /// </summary>
    public async Task<PriceCalculationResult> CalculatePrices(int formulaId)
    {
        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials)
                .ThenInclude(frm => frm.RawMaterial)
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        if (formula == null)
        {
            return new PriceCalculationResult
            {
                ErrorMessage = "Formula not found"
            };
        }

        return CalculatePricesFromFormulaMaterials(formula.FormulaRawMaterials.ToList());
    }

    /// <summary>
    /// Υπολογίζει τις τιμές από συνδέσεις φόρμουλας-υλικών (FormulaRawMaterial).
    /// </summary>
    public PriceCalculationResult CalculatePricesFromFormulaMaterials(List<FormulaRawMaterial> formulaMaterials)
    {
        var result = new PriceCalculationResult
        {
            CalculatedAt = DateTime.Now
        };

        decimal CostFor(FormulaRawMaterial frm)
        {
            var unitPrice = frm.RawMaterial?.Price ?? 0m;
            var pct = frm.Percentage ?? 0m;
            return unitPrice * pct / 100m;
        }

        // Υπολογισμός συνολικού κόστους υλικών
        result.TotalMaterialCost = formulaMaterials.Sum(CostFor);

        // Υπολογισμός κόστους παραγωγής (βασικό κόστος + εργασία)
        var productionCost = CalculateProductionCost(result.TotalMaterialCost);
        result.ProductionCost = productionCost;

        // Συνολικό κόστος
        result.TotalCost = result.TotalMaterialCost + productionCost;

        // Υπολογισμός τιμών με διαφορετικά περιθώρια
        result.Prices = CalculateAllPricePoints(result.TotalCost);

        // Ανάλυση κόστους ανά κατηγορία υλικού
        result.CostBreakdown = formulaMaterials
            .GroupBy(frm => frm.RawMaterial?.Category ?? "Other")
            .Select(g => new CostBreakdownItem
            {
                Category = g.Key,
                TotalCost = g.Sum(CostFor),
                Percentage = result.TotalMaterialCost > 0
                    ? (g.Sum(CostFor) / result.TotalMaterialCost * 100m)
                    : 0m
            })
            .OrderByDescending(c => c.TotalCost)
            .ToList();

        return result;
    }

    /// <summary>
    /// Backwards-compat overload (αν υπάρχουν λίστες RawMaterial χωρίς % – θεωρεί 100%).
    /// </summary>
    public PriceCalculationResult CalculatePricesFromMaterials(List<RawMaterial> materials)
    {
        var pseudo = materials.Select(m => new FormulaRawMaterial
        {
            RawMaterial = m,
            Percentage = 100m
        }).ToList();
        return CalculatePricesFromFormulaMaterials(pseudo);
    }

    /// <summary>
    /// Υπολογίζει το κόστος παραγωγής
    /// </summary>
    private decimal CalculateProductionCost(decimal materialCost)
    {
        // Βασικό κόστος παραγωγής: 15% του κόστους υλικών + βασικό ποσό
        var baseProductionCost = materialCost * 0.15m;
        
        // Ελάχιστο κόστος παραγωγής ανά μονάδα (π.χ. 0.50€)
        var minimumProductionCost = 0.50m;
        
        return Math.Max(baseProductionCost, minimumProductionCost);
    }

    /// <summary>
    /// Υπολογίζει όλα τα σημεία τιμών
    /// </summary>
    private Dictionary<string, PricePoint> CalculateAllPricePoints(decimal totalCost)
    {
        var prices = new Dictionary<string, PricePoint>();

        // Υπολογισμός για κάθε κατηγορία
        foreach (var margin in DEFAULT_MARGINS)
        {
            var pricePoint = CalculatePriceWithMargin(totalCost, margin.Value, margin.Key);
            prices[margin.Key] = pricePoint;
        }

        // Υπολογισμός προτεινόμενης τιμής λιανικής με ΦΠΑ
        var defaultPrice = prices["Default"];
        defaultPrice.RetailPriceWithVAT = defaultPrice.RetailPrice * (1 + VAT_RATES["GR"]);
        
        return prices;
    }

    /// <summary>
    /// Υπολογίζει τιμή με συγκεκριμένο περιθώριο
    /// </summary>
    private PricePoint CalculatePriceWithMargin(decimal cost, decimal marginPercentage, string category)
    {
        // Τιμή χωρίς ΦΠΑ = Κόστος / (1 - Περιθώριο)
        var priceWithoutVAT = cost / (1 - marginPercentage);
        
        // Έλεγχος για μηδενικό περιθώριο
        if (marginPercentage >= 1)
        {
            priceWithoutVAT = cost * 2; // Διπλασιασμός ως fallback
        }

        // Στρογγυλοποίηση σε λογικό αριθμό
        priceWithoutVAT = Math.Round(priceWithoutVAT, 2);

        return new PricePoint
        {
            Category = category,
            MarginPercentage = marginPercentage * 100,
            WholesalePrice = priceWithoutVAT,
            RetailPrice = priceWithoutVAT * 1.24m, // Με 24% VAT
            ProfitMargin = priceWithoutVAT - cost,
            ProfitPercentage = cost > 0 ? ((priceWithoutVAT - cost) / cost * 100) : 0
        };
    }

    /// <summary>
    /// Υπολογίζει την προτεινόμενη τιμή λιανικής
    /// </summary>
    public decimal CalculateRecommendedRetailPrice(decimal costPerUnit, decimal marginPercentage = 0.30m)
    {
        return costPerUnit / (1 - marginPercentage);
    }

    /// <summary>
    /// Υπολογίζει το περιθώριο κέρδους
    /// </summary>
    public decimal CalculateProfitMargin(decimal cost, decimal sellingPrice)
    {
        if (sellingPrice <= 0) return 0;
        return (sellingPrice - cost) / sellingPrice * 100;
    }

    /// <summary>
    /// Υπολογίζει το κόστος ανά μονάδα από συνολικό κόστος
    /// </summary>
    public decimal CalculateCostPerUnit(decimal totalCost, int units)
    {
        if (units <= 0) return 0;
        return totalCost / units;
    }

    /// <summary>
    /// Εφαρμόζει έκπτωση στην τιμή
    /// </summary>
    public decimal ApplyDiscount(decimal price, decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100) return price;
        return price * (1 - discountPercentage / 100);
    }

    /// <summary>
    /// Υπολογίζει τιμή με ΦΠΑ
    /// </summary>
    public decimal CalculatePriceWithVAT(decimal price, string countryCode = "GR")
    {
        var vatRate = VAT_RATES.ContainsKey(countryCode) ? VAT_RATES[countryCode] : VAT_RATES["EU"];
        return price * (1 + vatRate);
    }
}

/// <summary>
/// Αποτέλεσμα υπολογισμού τιμών
/// </summary>
public class PriceCalculationResult
{
    public decimal TotalMaterialCost { get; set; }
    public decimal ProductionCost { get; set; }
    public decimal TotalCost { get; set; }
    public Dictionary<string, PricePoint> Prices { get; set; } = new();
    public List<CostBreakdownItem> CostBreakdown { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Σημείο τιμής για μια κατηγορία
/// </summary>
public class PricePoint
{
    public string Category { get; set; } = "";
    public decimal MarginPercentage { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal RetailPriceWithVAT { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal ProfitPercentage { get; set; }
}

/// <summary>
/// Ανάλυση κόστους ανά κατηγορία
/// </summary>
public class CostBreakdownItem
{
    public string Category { get; set; } = "";
    public decimal TotalCost { get; set; }
    public decimal Percentage { get; set; }
}
