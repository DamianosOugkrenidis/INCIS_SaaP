// Author: Damianos Pythagoras Ougkrenidis
//
using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services;

/// <summary>
/// Υπηρεσία μαζικής εισαγωγής (bulk import) από Excel (.xlsx).
/// Υποστηρίζει Clients, Suppliers, INCIs, RawMaterials, Formulas με template download,
/// validation, duplicate detection (εντός αρχείου + έναντι βάσης), resolve foreign keys
/// μέσω ονόματος (Supplier/Client) και ασφαλή commit (single atomic SaveChanges).
/// </summary>
public class ImportService
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ImportService> _logger;

    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public ImportService(INCISDbContext context, ILogger<ImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ============================================================
    // COLUMN SCHEMAS (η/οι υποχρεωτική/ές είναι στην αρχή, βλ. RequiredFields)
    // ============================================================
    private static readonly Dictionary<ImportEntityType, string[]> Schemas = new()
    {
        [ImportEntityType.Clients] = new[]
        {
            "Name", "Code", "Address", "Email", "Phone", "AFM", "DOY",
            "ContactPerson", "ContactPhone", "ContactEmail", "Comments"
        },
        [ImportEntityType.Suppliers] = new[]
        {
            "Name", "Address", "Email", "Phone", "AFM", "DOY", "Comments"
        },
        [ImportEntityType.INCIs] = new[]
        {
            "Name", "CASNumber", "ECNumber", "Function", "Synonyms", "Description"
        },
        [ImportEntityType.RawMaterials] = new[]
        {
            "Code", "ProductName", "SupplierName", "Category", "Price",
            "NaturalOriginPercentage", "PrismaCode", "ManufacturerName", "Form", "Color", "Comments"
        },
        [ImportEntityType.Formulas] = new[]
        {
            "FormulaCode", "ProductName", "FormulaName", "Version", "ProductType",
            "ClientName", "Description", "ManufacturingSite", "Comments"
        }
    };

    /// <summary>Υποχρεωτικά πεδία ανά τύπο.</summary>
    private static string[] RequiredFields(ImportEntityType type) => type switch
    {
        ImportEntityType.RawMaterials => new[] { "Code", "ProductName" },
        ImportEntityType.Formulas => new[] { "FormulaCode" },
        _ => new[] { "Name" }
    };

    /// <summary>Κύριο πεδίο (για dedup + εμφάνιση).</summary>
    private static string PrimaryField(ImportEntityType type) => type switch
    {
        ImportEntityType.RawMaterials => "Code",
        ImportEntityType.Formulas => "FormulaCode",
        _ => "Name"
    };

    private static int MaxPrimaryLength(ImportEntityType type) => type switch
    {
        ImportEntityType.INCIs => 200,
        _ => 100
    };

    public static string[] GetColumns(ImportEntityType type) => Schemas[type];

    public static string DisplayName(ImportEntityType type) => type switch
    {
        ImportEntityType.Clients => "Πελάτες",
        ImportEntityType.Suppliers => "Προμηθευτές",
        ImportEntityType.INCIs => "Συστατικά INCI",
        ImportEntityType.RawMaterials => "Πρώτες Ύλες",
        ImportEntityType.Formulas => "Φόρμουλες",
        _ => type.ToString()
    };

    // ============================================================
    // TEMPLATE GENERATION
    // ============================================================
    public byte[] GenerateTemplate(ImportEntityType type)
    {
        var columns = Schemas[type];
        var required = RequiredFields(type);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(DisplayName(type));

        for (int i = 0; i < columns.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = columns[i] + (required.Contains(columns[i]) ? " *" : "");
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#714B67");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var sample = GetSampleRow(type);
        for (int i = 0; i < columns.Length; i++)
        {
            ws.Cell(2, i + 1).Value = i < sample.Length ? sample[i] : "";
            ws.Cell(2, i + 1).Style.Font.Italic = true;
            ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string[] GetSampleRow(ImportEntityType type) => type switch
    {
        ImportEntityType.Clients => new[]
        {
            "Demo Cosmetics AE", "CL-001", "Demo Address 1, Athens",
            "client@example.com", "+30 0000000000", "000000000", "Demo Tax Office",
            "Demo Contact", "+30 0000000000", "contact@example.com", "Demo notes"
        },
        ImportEntityType.Suppliers => new[]
        {
            "Demo Ingredients Ltd", "Demo Industrial Area, Thessaloniki",
            "supplier@example.com", "+30 0000000000", "000000000", "Demo Tax Office", "Demo supplier notes"
        },
        ImportEntityType.INCIs => new[]
        {
            "Aqua", "7732-18-5", "231-791-2", "Solvent", "Water", "Καθαρό νερό / διαλύτης"
        },
        ImportEntityType.RawMaterials => new[]
        {
            "RM-001", "Glycerin 99.5%", "Ingredients Hellas ΕΠΕ", "Humectants", "3.50",
            "100", "031900001", "Acme Chemicals GmbH", "Liquid", "Colorless", "MOQ 25kg"
        },
        ImportEntityType.Formulas => new[]
        {
            "ACME-0001-T01", "Hydrating Face Cream", "Face Cream v1", "v1.0", "Leave on",
            "ACME Cosmetics AE", "Ενυδατική κρέμα προσώπου", "Beauty Lab", "Πιλοτικό δείγμα"
        },
        _ => Array.Empty<string>()
    };

    // ============================================================
    // PARSE + VALIDATE (PREVIEW)
    // ============================================================
    public async Task<ImportPreviewResult> ParseAndValidateAsync(
        ImportEntityType type, Stream fileStream, string fileName)
    {
        var result = new ImportPreviewResult { Type = type, FileName = fileName };

        List<Dictionary<string, string>> rows;
        try
        {
            rows = ReadRows(type, fileStream, out var missingRequiredHeader);
            if (missingRequiredHeader != null)
            {
                result.FatalError = $"Λείπει η υποχρεωτική στήλη '{missingRequiredHeader}' από το αρχείο.";
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Import parse failed for {Type} file {File}", type, fileName);
            result.FatalError = "Το αρχείο δεν είναι έγκυρο Excel (.xlsx) ή είναι κατεστραμμένο.";
            return result;
        }

        result.TotalRows = rows.Count;
        if (rows.Count == 0)
        {
            result.FatalError = "Το αρχείο δεν περιέχει γραμμές δεδομένων.";
            return result;
        }

        var ctx = await LoadImportContextAsync(type);
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var rowNum = int.Parse(row["__row"]);
            var preview = ValidateRow(type, row, rowNum, ctx, seenInFile, out var issues, out var status);
            result.Rows.Add(preview);
            result.Issues.AddRange(issues);

            switch (status)
            {
                case "ok": result.ValidRows++; break;
                case "duplicate": result.DuplicateRows++; break;
                default: result.ErrorRows++; break;
            }
        }

        return result;
    }

    // ============================================================
    // COMMIT (IMPORT)
    // ============================================================
    public async Task<ImportCommitResult> CommitAsync(
        ImportEntityType type, Stream fileStream, string fileName)
    {
        var result = new ImportCommitResult { Type = type, FileName = fileName };

        List<Dictionary<string, string>> rows;
        try
        {
            rows = ReadRows(type, fileStream, out var missingHeader);
            if (missingHeader != null)
            {
                result.FatalError = $"Λείπει η υποχρεωτική στήλη '{missingHeader}' από το αρχείο.";
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Import commit parse failed for {Type}", type);
            result.FatalError = "Το αρχείο δεν είναι έγκυρο Excel (.xlsx) ή είναι κατεστραμμένο.";
            return result;
        }

        result.TotalRows = rows.Count;
        var ctx = await LoadImportContextAsync(type);
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var toInsert = new List<object>();
        foreach (var row in rows)
        {
            var rowNum = int.Parse(row["__row"]);
            ValidateRow(type, row, rowNum, ctx, seenInFile, out var issues, out var status);
            result.Issues.AddRange(issues);

            if (status == "error") { result.SkippedErrors++; continue; }
            if (status == "duplicate") { result.SkippedDuplicates++; continue; }

            toInsert.Add(BuildEntity(type, row, ctx));
        }

        if (toInsert.Count == 0)
        {
            result.Imported = 0;
            return result;
        }

        // Ένα SaveChanges είναι ήδη ατομικό (EF το τυλίγει σε δική του transaction),
        // οπότε δεν χρειάζεται explicit transaction. Σε αποτυχία, τίποτα δεν αποθηκεύεται.
        try
        {
            await _context.AddRangeAsync(toInsert);
            await _context.SaveChangesAsync();
            result.Imported = toInsert.Count;
            _logger.LogInformation("Imported {Count} {Type} rows from {File}", result.Imported, type, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import commit failed for {Type}", type);
            result.FatalError = "Η αποθήκευση απέτυχε. Δεν εισήχθη καμία γραμμή.";
            result.Imported = 0;
        }

        return result;
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private List<Dictionary<string, string>> ReadRows(
        ImportEntityType type, Stream stream, out string? missingRequiredHeader)
    {
        missingRequiredHeader = null;
        var columns = Schemas[type];
        var required = RequiredFields(type);

        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault();
        var result = new List<Dictionary<string, string>>();
        if (ws == null) return result;

        var headerRow = ws.FirstRowUsed();
        if (headerRow == null) return result;

        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var name = NormalizeHeader(cell.GetString());
            if (!string.IsNullOrEmpty(name) && !headerMap.ContainsKey(name))
                headerMap[name] = cell.Address.ColumnNumber;
        }

        foreach (var req in required)
        {
            if (!headerMap.ContainsKey(req))
            {
                missingRequiredHeader = req;
                return result;
            }
        }

        var lastRow = ws.LastRowUsed()!.RowNumber();
        for (int r = headerRow.RowNumber() + 1; r <= lastRow; r++)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["__row"] = r.ToString()
            };
            bool anyValue = false;
            foreach (var col in columns)
            {
                var val = "";
                if (headerMap.TryGetValue(col, out var colNum))
                {
                    val = ws.Cell(r, colNum).GetString().Trim();
                    if (!string.IsNullOrEmpty(val)) anyValue = true;
                }
                dict[col] = val;
            }
            if (anyValue) result.Add(dict);
        }

        return result;
    }

    private static string NormalizeHeader(string raw) =>
        (raw ?? string.Empty).Replace("*", "").Trim();

    private ImportPreviewRow ValidateRow(
        ImportEntityType type, Dictionary<string, string> row, int rowNum,
        ImportContext ctx, HashSet<string> seenInFile,
        out List<ImportRowIssue> issues, out string status)
    {
        issues = new List<ImportRowIssue>();
        var columns = Schemas[type];
        var primaryField = PrimaryField(type);
        var primary = row[primaryField];

        var preview = new ImportPreviewRow { RowNumber = rowNum, Primary = primary };

        // 1) Υποχρεωτικά πεδία
        foreach (var req in RequiredFields(type))
        {
            if (string.IsNullOrWhiteSpace(row[req]))
            {
                issues.Add(Err(rowNum, req, $"Το πεδίο '{req}' είναι υποχρεωτικό."));
                status = "error"; preview.Status = "error";
                preview.Messages.Add($"Λείπει: {req}");
                return preview;
            }
        }

        // 2) Μήκος κύριου πεδίου
        var maxPrimary = MaxPrimaryLength(type);
        if (primary.Length > maxPrimary)
        {
            issues.Add(Err(rowNum, primaryField, $"Το '{primaryField}' υπερβαίνει τα {maxPrimary} χαρακτήρες."));
            status = "error"; preview.Status = "error";
            preview.Messages.Add($"{primaryField} > {maxPrimary} χαρ.");
            return preview;
        }

        // 3) Email format (warning)
        foreach (var emailField in new[] { "Email", "ContactEmail" })
        {
            if (columns.Contains(emailField))
            {
                var email = row[emailField];
                if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
                {
                    issues.Add(Warn(rowNum, emailField, $"Μη έγκυρη μορφή email '{email}' (θα εισαχθεί ως έχει)."));
                    preview.Messages.Add($"Ύποπτο {emailField}");
                }
            }
        }

        // 4) Numeric fields (warning αν μη αριθμός)
        foreach (var numField in new[] { "Price", "NaturalOriginPercentage" })
        {
            if (columns.Contains(numField) && !string.IsNullOrWhiteSpace(row[numField]) && ParseDecimal(row[numField]) == null)
            {
                issues.Add(Warn(rowNum, numField, $"Μη αριθμητική τιμή '{row[numField]}' (θα αγνοηθεί)."));
                preview.Messages.Add($"Ύποπτο {numField}");
            }
        }

        // 5) Foreign-key resolution (warning αν δεν βρεθεί)
        if (type == ImportEntityType.RawMaterials)
        {
            var sup = row["SupplierName"];
            if (!string.IsNullOrWhiteSpace(sup) && !ctx.SupplierByName.ContainsKey(sup.Trim()))
            {
                issues.Add(Warn(rowNum, "SupplierName", $"Ο προμηθευτής '{sup}' δεν βρέθηκε (θα εισαχθεί χωρίς προμηθευτή)."));
                preview.Messages.Add("Άγνωστος προμηθευτής");
            }
        }
        if (type == ImportEntityType.Formulas)
        {
            var cl = row["ClientName"];
            if (!string.IsNullOrWhiteSpace(cl) && !ctx.ClientByName.ContainsKey(cl.Trim()))
            {
                issues.Add(Warn(rowNum, "ClientName", $"Ο πελάτης '{cl}' δεν βρέθηκε (θα εισαχθεί χωρίς πελάτη)."));
                preview.Messages.Add("Άγνωστος πελάτης");
            }
        }

        preview.Secondary = SecondaryText(type, row);

        // 6) Duplicate detection
        var key = primary.Trim().ToLowerInvariant();
        if (!seenInFile.Add(key))
        {
            issues.Add(Err(rowNum, primaryField, "Διπλότυπη γραμμή μέσα στο ίδιο αρχείο."));
            status = "duplicate"; preview.Status = "duplicate";
            preview.Messages.Add("Διπλότυπο (αρχείο)");
            return preview;
        }

        if (ctx.ExistsInDb(type, row))
        {
            issues.Add(Warn(rowNum, primaryField, "Υπάρχει ήδη στη βάση — θα παραλειφθεί."));
            status = "duplicate"; preview.Status = "duplicate";
            preview.Messages.Add("Υπάρχει ήδη");
            return preview;
        }

        status = "ok";
        preview.Status = "ok";
        return preview;
    }

    private static string SecondaryText(ImportEntityType type, Dictionary<string, string> row) => type switch
    {
        ImportEntityType.Clients => row["Code"],
        ImportEntityType.Suppliers => row["AFM"],
        ImportEntityType.INCIs => row["CASNumber"],
        ImportEntityType.RawMaterials => row["ProductName"],
        ImportEntityType.Formulas => row["ProductName"],
        _ => ""
    };

    private object BuildEntity(ImportEntityType type, Dictionary<string, string> row, ImportContext ctx)
    {
        string? NullIfEmpty(string k) =>
            row.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

        return type switch
        {
            ImportEntityType.Clients => new Client
            {
                Name = row["Name"].Trim(),
                Code = NullIfEmpty("Code"),
                Address = NullIfEmpty("Address"),
                Email = NullIfEmpty("Email"),
                Phone = NullIfEmpty("Phone"),
                AFM = NullIfEmpty("AFM"),
                DOY = NullIfEmpty("DOY"),
                ContactPerson = NullIfEmpty("ContactPerson"),
                ContactPhone = NullIfEmpty("ContactPhone"),
                ContactEmail = NullIfEmpty("ContactEmail"),
                Comments = NullIfEmpty("Comments"),
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            ImportEntityType.Suppliers => new Supplier
            {
                Name = row["Name"].Trim(),
                Address = NullIfEmpty("Address"),
                Email = NullIfEmpty("Email"),
                Phone = NullIfEmpty("Phone"),
                AFM = NullIfEmpty("AFM"),
                DOY = NullIfEmpty("DOY"),
                Comments = NullIfEmpty("Comments"),
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            ImportEntityType.INCIs => new INCI
            {
                Name = row["Name"].Trim(),
                CASNumber = NullIfEmpty("CASNumber"),
                ECNumber = NullIfEmpty("ECNumber"),
                Function = NullIfEmpty("Function"),
                Synonyms = NullIfEmpty("Synonyms"),
                Description = NullIfEmpty("Description"),
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            ImportEntityType.RawMaterials => new RawMaterial
            {
                Code = row["Code"].Trim(),
                ProductName = row["ProductName"].Trim(),
                SupplierId = ResolveId(ctx.SupplierByName, NullIfEmpty("SupplierName")),
                Category = NullIfEmpty("Category"),
                Price = ParseDecimal(row.GetValueOrDefault("Price", "")),
                NaturalOriginPercentage = ParseDecimal(row.GetValueOrDefault("NaturalOriginPercentage", "")),
                PrismaCode = NullIfEmpty("PrismaCode"),
                ManufacturerName = NullIfEmpty("ManufacturerName"),
                Form = NullIfEmpty("Form"),
                Color = NullIfEmpty("Color"),
                Comments = NullIfEmpty("Comments"),
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            ImportEntityType.Formulas => new Formula
            {
                FormulaCode = row["FormulaCode"].Trim(),
                ProductName = NullIfEmpty("ProductName"),
                FormulaName = NullIfEmpty("FormulaName") ?? NullIfEmpty("ProductName"),
                Version = NullIfEmpty("Version"),
                ProductType = NullIfEmpty("ProductType"),
                ClientId = ResolveId(ctx.ClientByName, NullIfEmpty("ClientName")),
                Description = NullIfEmpty("Description"),
                ManufacturingSite = NullIfEmpty("ManufacturingSite"),
                Comments = NullIfEmpty("Comments"),
                CreationDate = DateTime.Now,
                LastModified = DateTime.Now,
                IsWIP = true
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static int? ResolveId(Dictionary<string, int> map, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return map.TryGetValue(name.Trim(), out var id) ? id : (int?)null;
    }

    private static decimal? ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().Replace(",", ".");
        return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null;
    }

    private async Task<ImportContext> LoadImportContextAsync(ImportEntityType type)
    {
        var primaryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var supplierByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var clientByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        switch (type)
        {
            case ImportEntityType.Clients:
                foreach (var n in await _context.Clients.Select(c => c.Name).ToListAsync())
                    if (!string.IsNullOrWhiteSpace(n)) primaryKeys.Add(n.Trim());
                break;
            case ImportEntityType.Suppliers:
                foreach (var n in await _context.Suppliers.Select(s => s.Name).ToListAsync())
                    if (!string.IsNullOrWhiteSpace(n)) primaryKeys.Add(n.Trim());
                break;
            case ImportEntityType.INCIs:
                foreach (var i in await _context.INCIs.Select(i => new { i.Name, i.CASNumber }).ToListAsync())
                {
                    if (!string.IsNullOrWhiteSpace(i.Name)) primaryKeys.Add(i.Name.Trim());
                    if (!string.IsNullOrWhiteSpace(i.CASNumber)) cas.Add(i.CASNumber!.Trim());
                }
                break;
            case ImportEntityType.RawMaterials:
                foreach (var c in await _context.RawMaterials.Select(r => r.Code).ToListAsync())
                    if (!string.IsNullOrWhiteSpace(c)) primaryKeys.Add(c.Trim());
                foreach (var s in await _context.Suppliers.Select(s => new { s.Id, s.Name }).ToListAsync())
                    if (!string.IsNullOrWhiteSpace(s.Name)) supplierByName[s.Name.Trim()] = s.Id;
                break;
            case ImportEntityType.Formulas:
                foreach (var fc in await _context.Formulas.Select(f => f.FormulaCode).ToListAsync())
                    if (!string.IsNullOrWhiteSpace(fc)) primaryKeys.Add(fc.Trim());
                foreach (var c in await _context.Clients.Select(c => new { c.Id, c.Name }).ToListAsync())
                    if (!string.IsNullOrWhiteSpace(c.Name)) clientByName[c.Name.Trim()] = c.Id;
                break;
        }

        return new ImportContext(primaryKeys, cas, supplierByName, clientByName);
    }

    private static ImportRowIssue Err(int row, string field, string msg) =>
        new() { RowNumber = row, Field = field, Message = msg, IsError = true };

    private static ImportRowIssue Warn(int row, string field, string msg) =>
        new() { RowNumber = row, Field = field, Message = msg, IsError = false };

    /// <summary>Προ-φορτωμένα δεδομένα βάσης για validation + FK resolution.</summary>
    private sealed class ImportContext
    {
        private readonly HashSet<string> _primaryKeys;
        private readonly HashSet<string> _cas;
        public Dictionary<string, int> SupplierByName { get; }
        public Dictionary<string, int> ClientByName { get; }

        public ImportContext(HashSet<string> primaryKeys, HashSet<string> cas,
            Dictionary<string, int> supplierByName, Dictionary<string, int> clientByName)
        {
            _primaryKeys = primaryKeys;
            _cas = cas;
            SupplierByName = supplierByName;
            ClientByName = clientByName;
        }

        public bool ExistsInDb(ImportEntityType type, Dictionary<string, string> row)
        {
            var primary = row[PrimaryField(type)].Trim();
            if (_primaryKeys.Contains(primary)) return true;
            if (type == ImportEntityType.INCIs)
            {
                var casVal = row.TryGetValue("CASNumber", out var c) ? c.Trim() : "";
                if (!string.IsNullOrWhiteSpace(casVal) && _cas.Contains(casVal)) return true;
            }
            return false;
        }
    }
}
