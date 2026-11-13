// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers
{
    [Authorize]
    public class ImportController : Controller
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private readonly ImportService _importService;
        private readonly IInciCatalogImportService _inciCatalogImportService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImportController> _logger;

        public ImportController(
            ImportService importService,
            IInciCatalogImportService inciCatalogImportService,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<ImportController> logger)
        {
            _importService = importService;
            _inciCatalogImportService = inciCatalogImportService;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Index() => View();

        /// <summary>
        /// Εισαγωγή του καταλόγου INCI από το αρχείο-πηγή ALL_INCIS_NEW_FILE (.xls/.xlsx).
        /// Idempotent upsert με κλειδί το INCI Name· δεν διαγράφει άλλα δεδομένα.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> InciCatalog(IFormFile? file)
        {
            var result = new INCIS_SaaP.Dtos.InciCatalogImportResult
            {
                DataSource = InciCatalogImportService.DataSourceName
            };

            if (file == null || file.Length == 0)
            {
                result.FatalError = "Δεν επιλέχθηκε αρχείο ή το αρχείο είναι κενό.";
                return Json(result);
            }
            if (file.Length > MaxFileSizeBytes)
            {
                result.FatalError = "Το αρχείο υπερβαίνει το όριο των 10 MB.";
                return Json(result);
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xls" && ext != ".xlsx")
            {
                result.FatalError = "Επιτρέπονται αρχεία Excel (.xls/.xlsx).";
                return Json(result);
            }

            await using var stream = file.OpenReadStream();
            return Json(await _inciCatalogImportService.ImportAsync(stream, file.FileName));
        }

        /// <summary>
        /// Εισαγωγή του καταλόγου INCI από τη διαδρομή που ορίζεται στο
        /// <c>InciCatalog:SourcePath</c> (default: ALL_INCIS_NEW_FILE.xls στον ρίζα του content root).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedInciCatalog()
        {
            var path = _configuration["InciCatalog:SourcePath"];
            if (string.IsNullOrWhiteSpace(path))
            {
                var candidates = new[]
                {
                    Path.Combine(_environment.ContentRootPath, "ALL_INCIS_NEW_FILE.xls"),
                    Path.Combine(_environment.ContentRootPath, "..", "ALL_INCIS_NEW_FILE.xls"),
                    Path.Combine(_environment.ContentRootPath, "..", "..", "ALL_INCIS_NEW_FILE.xls")
                };
                path = candidates.Select(Path.GetFullPath).FirstOrDefault(System.IO.File.Exists) ?? candidates[1];
            }

            var result = await _inciCatalogImportService.ImportFromFileAsync(path);
            return Json(result);
        }

        /// <summary>Κατέβασμα Excel template για συγκεκριμένο τύπο οντότητας.</summary>
        [HttpGet]
        public IActionResult Template(ImportEntityType type)
        {
            var bytes = _importService.GenerateTemplate(type);
            var fileName = $"INCIS_Import_Template_{type}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>Ανέβασμα + προεπισκόπηση/validation (χωρίς αποθήκευση).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> Preview(ImportEntityType type, IFormFile? file)
        {
            var validationError = ValidateFile(file);
            if (validationError != null)
            {
                var errResult = new ImportPreviewResult { Type = type, FatalError = validationError };
                return View(errResult);
            }

            await using var stream = file!.OpenReadStream();
            var result = await _importService.ParseAndValidateAsync(type, stream, file.FileName);
            return View(result);
        }

        /// <summary>Οριστική εισαγωγή έγκυρων γραμμών (transaction, skip invalid/duplicates).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> Commit(ImportEntityType type, IFormFile? file)
        {
            var validationError = ValidateFile(file);
            if (validationError != null)
            {
                var errResult = new ImportCommitResult { Type = type, FatalError = validationError };
                return View("Result", errResult);
            }

            await using var stream = file!.OpenReadStream();
            var result = await _importService.CommitAsync(type, stream, file.FileName);
            return View("Result", result);
        }

        private static string? ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "Δεν επιλέχθηκε αρχείο ή το αρχείο είναι κενό.";
            if (file.Length > MaxFileSizeBytes)
                return "Το αρχείο υπερβαίνει το όριο των 10 MB.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
                return "Επιτρέπονται μόνο αρχεία Excel (.xlsx).";
            return null;
        }
    }
}
