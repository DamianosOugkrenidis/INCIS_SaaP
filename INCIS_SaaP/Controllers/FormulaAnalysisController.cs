// Author: Damianos Pythagoras Ougkrenidis
//
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Controller για ανάλυση φόρμουλων (υπολογισμοί από Excel λειτουργίες)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]



public class FormulaAnalysisController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly AllergenService _allergenService;
    private readonly PriceCalculationService _priceService;
    private readonly HeavyMetalComplianceService _heavyMetalService;
    private readonly IFormulaIntelligenceService _intelligence;
    private readonly IFormulaSuccessModel _successModel;

    public FormulaAnalysisController(
        INCISDbContext context,
        IFormulaIntelligenceService intelligence,
        IFormulaSuccessModel successModel)
    {
        _context = context;
        _allergenService = new AllergenService(context);
        _priceService = new PriceCalculationService(context);
        _heavyMetalService = new HeavyMetalComplianceService(context);
        _intelligence = intelligence;
        _successModel = successModel;
    }

    /// <summary>
    /// Πρόβλεψη πιθανότητας επιτυχίας φόρμουλας με πραγματικό ML.NET μοντέλο
    /// (binary classification). Επιστρέφει πιθανότητα, επίπεδο ρίσκου, τους
    /// σημαντικότερους παράγοντες και πλήρη διαφάνεια για το μοντέλο.
    /// </summary>
    [HttpGet("{formulaId}/predict-success")]
    public async Task<IActionResult> PredictSuccess(int formulaId, CancellationToken ct)
    {
        try
        {
            var formula = await _context.Formulas.FindAsync(new object?[] { formulaId }, ct);
            if (formula == null)
                return NotFound(new { error = "Formula not found" });

            var p = await _intelligence.PredictFormulaSuccessAsync(formulaId, ct);
            return Ok(new
            {
                formulaId,
                formula.FormulaCode,
                successProbability = Math.Round(p.SuccessProbability, 4),
                stabilityProbability = Math.Round(p.StabilityProbability, 4),
                challengeProbability = Math.Round(p.ChallengeProbability, 4),
                riskLevel = p.RiskLevel,
                modelConfidence = Math.Round(p.ModelConfidence, 3),
                topFactors = p.TopFactors,
                recommendations = p.Recommendations,
                model = new
                {
                    source = p.ModelSource,
                    auc = Math.Round(p.ModelAuc, 3),
                    accuracy = Math.Round(p.ModelAccuracy, 3),
                    trainingSamples = p.TrainingSampleCount,
                    trainedAtUtc = p.ModelTrainedAtUtc
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Καταγραφή ΠΡΑΓΜΑΤΙΚΟΥ αποτελέσματος τεστ φόρμουλας (data loop) — τροφοδοτεί το ML
    /// με labeled δεδομένα και επανεκπαιδεύει το μοντέλο ώστε να «ωριμάζει» με πραγματικά δεδομένα.
    /// </summary>
    [HttpPost("{formulaId}/record-outcome")]
    public async Task<IActionResult> RecordOutcome(int formulaId, [FromBody] OutcomeRequest req, CancellationToken ct)
    {
        try
        {
            var total = await _intelligence.RecordOutcomeAsync(
                formulaId, req.PassedOverall, req.PassedStability, req.PassedChallenge, req.FailureReason, ct);

            // Η επανεκπαίδευση του ML μοντέλου είναι CPU-heavy — την εκτελούμε ASYNCHRONOUSLY
            // σε background (Hangfire) ώστε το request να απαντά άμεσα και να μη μπλοκάρονται
            // threads/CPU όταν πολλοί χρήστες καταγράφουν outcomes ταυτόχρονα.
            var retrainJobId = BackgroundJob.Enqueue<BackgroundJobs>(job => job.RetrainMlModel());

            return Ok(new
            {
                formulaId,
                message = "Το αποτέλεσμα καταγράφηκε. Η επανεκπαίδευση του μοντέλου προγραμματίστηκε στο παρασκήνιο.",
                totalLabeledRecords = total,
                retrainJobId
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Πληροφορίες/μετρικές του εκπαιδευμένου ML μοντέλου (διαφάνεια).</summary>
    [HttpGet("ml/model-info")]
    public async Task<IActionResult> ModelInfo(CancellationToken ct)
    {
        var meta = await _successModel.EnsureTrainedAsync(ct);
        return Ok(new
        {
            isTrained = meta.IsTrained,
            trainingSource = meta.TrainingSource,
            trainedAtUtc = meta.TrainedAtUtc,
            sampleCount = meta.SampleCount,
            realSampleCount = meta.RealSampleCount,
            syntheticSampleCount = meta.SyntheticSampleCount,
            accuracy = Math.Round(meta.Accuracy, 3),
            areaUnderRocCurve = Math.Round(meta.AreaUnderRocCurve, 3),
            f1Score = Math.Round(meta.F1Score, 3),
            globalFactors = meta.GlobalFactors,
            note = meta.TrainingSource is "Synthetic" or "Hybrid"
                ? "Το μοντέλο εκπαιδεύτηκε (μερικώς) σε συνθετικά δεδομένα λόγω περιορισμένων πραγματικών δειγμάτων· η ακρίβεια βελτιώνεται καθώς συσσωρεύονται πραγματικά δεδομένα."
                : "Το μοντέλο εκπαιδεύτηκε σε πραγματικά δεδομένα."
        });
    }

    /// <summary>Χειροκίνητη επανεκπαίδευση του ML μοντέλου (Admin/SuperUser).</summary>
    [HttpPost("ml/retrain")]
    [Authorize(Roles = "Admin,SuperUser")]
    public async Task<IActionResult> RetrainModel(CancellationToken ct)
    {
        var meta = await _successModel.RetrainAsync(ct);
        return Ok(new
        {
            message = "Model retrained",
            trainingSource = meta.TrainingSource,
            sampleCount = meta.SampleCount,
            realSampleCount = meta.RealSampleCount,
            areaUnderRocCurve = Math.Round(meta.AreaUnderRocCurve, 3),
            accuracy = Math.Round(meta.Accuracy, 3)
        });
    }

    /// <summary>
    /// Πλήρης ανάλυση φόρμουλας - όλοι οι έλεγχοι
    /// </summary>
    [HttpGet("{formulaId}/analyze")]
    public async Task<IActionResult> AnalyzeFormula(int formulaId)
    {
        try
        {
            var formula = await _context.Formulas.FindAsync(formulaId);
            if (formula == null)
            {
                return NotFound(new { error = "Formula not found" });
            }

            // Εκτέλεση όλων των αναλύσεων
            var allergenResult = await _allergenService.CheckFormulaAllergens(formulaId);
            var priceResult = await _priceService.CalculatePrices(formulaId);
            var heavyMetalResult = await _heavyMetalService.CheckCompliance(formulaId);

            return Ok(new
            {
                formulaId = formulaId,
                formulaCode = formula.FormulaCode,
                formulaName = formula.FormulaName,
                analyzedAt = DateTime.Now,
                allergenCheck = allergenResult,
                priceCalculation = priceResult,
                heavyMetalCompliance = heavyMetalResult
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Έλεγχος αλλεργιογόνων
    /// </summary>
    [HttpGet("{formulaId}/allergens")]
    public async Task<IActionResult> CheckAllergens(int formulaId)
    {
        try
        {
            var result = await _allergenService.CheckFormulaAllergens(formulaId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Υπολογισμός τιμών
    /// </summary>
    [HttpGet("{formulaId}/prices")]
    public async Task<IActionResult> CalculatePrices(int formulaId)
    {
        try
        {
            var result = await _priceService.CalculatePrices(formulaId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Έλεγχος βαρέων μετάλλων
    /// </summary>
    [HttpGet("{formulaId}/heavymetals")]
    public async Task<IActionResult> CheckHeavyMetals(int formulaId)
    {
        try
        {
            var result = await _heavyMetalService.CheckCompliance(formulaId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Λίστα αλλεργιογόνων ΕΕ που απαιτούν δήλωση
    /// </summary>
    [HttpGet("allergens/eu-list")]
    public IActionResult GetEUAllergens()
    {
        var allergens = AllergenService.GetEUDeclaredAllergens();
        return Ok(new { allergens = allergens, count = allergens.Count });
    }

    /// <summary>
    /// Όρια βαρέων μετάλλων
    /// </summary>
    [HttpGet("heavymetals/limits")]
    public IActionResult GetHeavyMetalLimits()
    {
        var limits = HeavyMetalComplianceService.GetAllLimits();
        return Ok(limits);
    }

    /// <summary>
    /// Απλός υπολογισμός τιμής
    /// </summary>
    [HttpPost("calculate-price")]
    public IActionResult CalculatePrice([FromBody] PriceCalculationRequest request)
    {
        try
        {
            var retailPrice = _priceService.CalculateRecommendedRetailPrice(
                request.CostPerUnit, 
                request.MarginPercentage / 100
            );

            var priceWithVAT = _priceService.CalculatePriceWithVAT(retailPrice, request.CountryCode ?? "GR");

            return Ok(new
            {
                costPerUnit = request.CostPerUnit,
                marginPercentage = request.MarginPercentage,
                recommendedRetailPrice = Math.Round(retailPrice, 2),
                priceWithVAT = Math.Round(priceWithVAT, 2),
                currency = request.CountryCode == "GR" ? "EUR" : "USD"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request για υπολογισμό τιμής
/// </summary>
public class PriceCalculationRequest
{
    public decimal CostPerUnit { get; set; }
    public decimal MarginPercentage { get; set; } = 30; // Default 30%
    public string? CountryCode { get; set; } = "GR";
}

/// <summary>Request για καταγραφή πραγματικού αποτελέσματος τεστ φόρμουλας.</summary>
public class OutcomeRequest
{
    public bool PassedOverall { get; set; }
    public bool? PassedStability { get; set; }
    public bool? PassedChallenge { get; set; }
    public string? FailureReason { get; set; }
}
