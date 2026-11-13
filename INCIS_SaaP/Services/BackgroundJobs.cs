// Author: Damianos Pythagoras Ougkrenidis
//
using Hangfire;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.Models;
using Microsoft.EntityFrameworkCore;

namespace INCIS_SaaP.Services;

/// <summary>
/// Background jobs service using Hangfire
/// </summary>
public class BackgroundJobs
{
    private readonly INCISDbContext _context;
    private readonly ILogger<BackgroundJobs> _logger;
    private readonly IFormulaSuccessModel _formulaModel;

    public BackgroundJobs(INCISDbContext context, ILogger<BackgroundJobs> logger, IFormulaSuccessModel formulaModel)
    {
        _context = context;
        _logger = logger;
        _formulaModel = formulaModel;
    }

    /// <summary>
    /// Επανεκπαίδευση του ML.NET μοντέλου πρόβλεψης επιτυχίας φόρμουλας
    /// (ώστε να ενσωματώνει νέα δεδομένα καθώς προστίθενται).
    /// </summary>
    [AutomaticRetry(Attempts = 1)]
    public async Task RetrainMlModel()
    {
        try
        {
            var meta = await _formulaModel.RetrainAsync();
            _logger.LogInformation(
                "ML retrain complete. Source={Source} Samples={Samples} (real={Real}) AUC={Auc:F3}",
                meta.TrainingSource, meta.SampleCount, meta.RealSampleCount, meta.AreaUnderRocCurve);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML model retrain failed");
        }
    }

    /// <summary>
    /// Καθαρισμός παλιών audit logs
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task CleanupOldAuditLogs()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            var oldLogs = await _context.AuditLogs
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} old audit logs", oldLogs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up audit logs");
        }
    }

    /// <summary>
    /// Ενημέρωση στατιστικών
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task UpdateStatistics()
    {
        try
        {
            var stats = new
            {
                TotalFormulas = await _context.Formulas.CountAsync(),
                TotalIngredients = await _context.INCIIngredients.CountAsync(),
                TotalAllergens = await _context.Allergens.CountAsync(),
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Statistics updated: {TotalFormulas} formulas", stats.TotalFormulas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating statistics");
        }
    }

    /// <summary>
    /// Αποστολή υπενθυμίσεων
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendExpiryReminders()
    {
        try
        {
            var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);
            
            var expiringCertifications = await _context.NaturalCertifications
                .Where(c => c.ExpiryDate <= thirtyDaysFromNow && c.ExpiryDate > DateTime.UtcNow)
                .Include(c => c.Formula)
                .ToListAsync();

            foreach (var cert in expiringCertifications)
            {
                _logger.LogWarning("Certification expiring: {FormulaCode} - {ExpiryDate}", 
                    cert.Formula?.FormulaCode, cert.ExpiryDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending expiry reminders");
        }
    }

    /// <summary>
    /// Καθαρισμός expired tokens
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task CleanupExpiredTokens()
    {
        try
        {
            var expiredUsers = await _context.Users
                .Where(u => u.PasswordResetTokenExpiry != null && u.PasswordResetTokenExpiry < DateTime.UtcNow)
                .ToListAsync();

            foreach (var user in expiredUsers)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired tokens", expiredUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up tokens");
        }
    }

    /// <summary>
    /// Weekly backup notification
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task WeeklyBackupNotification()
    {
        _logger.LogInformation("Weekly backup job triggered at {Time}", DateTime.UtcNow);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Hangfire job scheduler
/// </summary>
public class JobScheduler
{
    public static void ScheduleJobs()
    {
        // Καθημερινός καθαρισμός audit logs (κάθε μέρα στις 2:00)
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "cleanup-audit-logs",
            job => job.CleanupOldAuditLogs(),
            "0 2 * * *");

        // Ενημέρωση στατιστικών (κάθε ώρα)
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "update-statistics",
            job => job.UpdateStatistics(),
            "0 * * * *");

        // Υπενθυμίσεις λήξης (κάθε μέρα στις 9:00)
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "expiry-reminders",
            job => job.SendExpiryReminders(),
            "0 9 * * *");

        // Καθαρισμός expired tokens (κάθε μέρα στις 3:00)
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "cleanup-tokens",
            job => job.CleanupExpiredTokens(),
            "0 3 * * *");

        // Weekly backup (Κάθε Κυριακή στις 4:00)
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "weekly-backup",
            job => job.WeeklyBackupNotification(),
            "0 4 * * 0");

        // Εβδομαδιαία επανεκπαίδευση ML μοντέλου (Κάθε Κυριακή στις 5:00)
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "ml-retrain-formula-success",
            job => job.RetrainMlModel(),
            "0 5 * * 0");
    }
}
