// Author: Damianos Pythagoras Ougkrenidis
//
using System.Text;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services;

public interface IInciCatalogImportService
{
    /// <summary>
    /// Εισάγει/ενημερώνει τον κατάλογο INCI από το ALL_INCIS_NEW_FILE (.xls/.xlsx).
    /// Idempotent: γίνεται upsert με κλειδί το INCI Name· δεν διαγράφει άλλα δεδομένα.
    /// </summary>
    Task<InciCatalogImportResult> ImportAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>Εισαγωγή από διαδρομή αρχείου.</summary>
    Task<InciCatalogImportResult> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Importer για το αρχείο-πηγή <c>ALL_INCIS_NEW_FILE</c> (φύλλο "printing", legacy BIFF8 .xls).
/// Αντιστοιχίζει τις στήλες a/n, Name, Origin, Cas, Einecs, Tox. Profile, Restriction, Functions
/// στην οντότητα <see cref="INCI"/> (πίνακας INCIs).
/// </summary>
public class InciCatalogImportService : IInciCatalogImportService
{
    /// <summary>Τιμή προέλευσης που σφραγίζει τις εγγραφές που εισάγονται από αυτή την πηγή.</summary>
    public const string DataSourceName = "ALL_INCIS_NEW_FILE";

    private const int NameMaxLength = 500;
    private const int CasMaxLength = 200;
    private const int EcMaxLength = 200;
    private const int FunctionMaxLength = 200;
    private const int OriginMaxLength = 200;
    private const int RestrictionMaxLength = 500;
    private const int BatchSize = 500;

    private readonly INCISDbContext _context;
    private readonly ILogger<InciCatalogImportService> _logger;

    static InciCatalogImportService()
    {
        // Ο ExcelDataReader χρειάζεται το CodePages provider για legacy .xls (BIFF8).
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public InciCatalogImportService(INCISDbContext context, ILogger<InciCatalogImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InciCatalogImportResult> ImportFromFileAsync(
        string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new InciCatalogImportResult
            {
                FileName = filePath,
                DataSource = DataSourceName,
                FatalError = $"Το αρχείο πηγής δεν βρέθηκε: {filePath}"
            };
        }

        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await ImportAsync(stream, Path.GetFileName(filePath), cancellationToken);
    }

    public async Task<InciCatalogImportResult> ImportAsync(
        Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var result = new InciCatalogImportResult { FileName = fileName, DataSource = DataSourceName };

        List<ParsedInciRow> parsed;
        try
        {
            parsed = ParseWorkbook(stream, result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InciCatalog import: αδυναμία ανάγνωσης του αρχείου {File}", fileName);
            result.FatalError = "Το αρχείο δεν είναι έγκυρο Excel (.xls/.xlsx) ή είναι κατεστραμμένο.";
            return result;
        }

        if (result.FatalError != null)
            return result;

        result.TotalDataRows = parsed.Count;
        if (parsed.Count == 0)
        {
            result.FatalError = "Δεν βρέθηκαν γραμμές δεδομένων με στήλη 'Name'.";
            return result;
        }

        // Dedup στο ΠΛΗΡΕΣ όνομα πηγής (χωρίς απώλεια) και upsert κλειδί στο πλήρες όνομα,
        // ώστε η εισαγωγή να είναι lossless και idempotent ανεξάρτητα από τυχόν περικοπή
        // του ευρετηριοποιημένου πεδίου Name.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<ParsedInciRow>(parsed.Count);
        foreach (var row in parsed)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                result.SkippedEmptyName++;
                continue;
            }

            var key = NormalizeKey(row.Name!.Trim());
            if (key.Length == 0)
            {
                result.SkippedEmptyName++;
                continue;
            }
            if (!seen.Add(key))
            {
                result.DuplicateRows++;
                continue;
            }
            unique.Add(row);
        }

        // Υπάρχοντα INCIs (case-insensitive). Κλειδί: πλήρες όνομα· fallback στο Name
        // για εγγραφές που δημιουργήθηκαν πριν την προσθήκη του FullName.
        var existing = await _context.INCIs
            .Select(i => new { i.Id, i.Name, i.FullName })
            .ToListAsync(cancellationToken);

        var existingIdsByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in existing)
        {
            var basis = !string.IsNullOrWhiteSpace(e.FullName) ? e.FullName : e.Name;
            if (string.IsNullOrWhiteSpace(basis)) continue;
            var k = NormalizeKey(basis);
            if (!existingIdsByKey.ContainsKey(k))
                existingIdsByKey[k] = e.Id;
        }

        // Φόρτωση πλήρων entities για τα υπάρχοντα που θα ενημερωθούν.
        var idsToUpdate = unique
            .Select(r => NormalizeKey(r.Name!.Trim()))
            .Where(existingIdsByKey.ContainsKey)
            .Select(k => existingIdsByKey[k])
            .Distinct()
            .ToList();

        var tracked = await _context.INCIs
            .Where(i => idsToUpdate.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var now = DateTime.UtcNow;
        var processed = 0;

        foreach (var row in unique)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullName = row.Name!.Trim();
            var storedName = Truncate(fullName, NameMaxLength, result)!;
            var function = Truncate(TrimOrNull(row.Functions), FunctionMaxLength, result);
            var origin = Truncate(NullIfPlaceholder(row.Origin), OriginMaxLength, result);
            var restriction = Truncate(TrimOrNull(row.Restriction), RestrictionMaxLength, result);
            var cas = Truncate(NullIfPlaceholder(row.Cas), CasMaxLength, result);
            var ec = Truncate(NullIfPlaceholder(row.Einecs), EcMaxLength, result);
            var tox = TrimOrNull(row.ToxProfile);

            var key = NormalizeKey(fullName);
            if (existingIdsByKey.TryGetValue(key, out var id) && tracked.TryGetValue(id, out var entity))
            {
                entity.Name = storedName;
                entity.FullName = fullName;
                entity.CASNumber = cas;
                entity.ECNumber = ec;
                entity.Function = function;
                entity.Origin = origin;
                entity.Restriction = restriction;
                entity.ToxicologicalProfile = tox;
                entity.DataSource = DataSourceName;
                entity.ImportedAt = now;
                entity.IsActive = true;
                result.Updated++;
            }
            else
            {
                var created = new INCI
                {
                    Name = storedName,
                    FullName = fullName,
                    CASNumber = cas,
                    ECNumber = ec,
                    Function = function,
                    Origin = origin,
                    Restriction = restriction,
                    ToxicologicalProfile = tox,
                    DataSource = DataSourceName,
                    ImportedAt = now,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                _context.INCIs.Add(created);
                existingIdsByKey[key] = created.Id;
                result.Inserted++;
            }

            if (++processed % BatchSize == 0)
                await _context.SaveChangesAsync(cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "InciCatalog import από {File}: inserted={Inserted}, updated={Updated}, dup={Dup}, empty={Empty}",
            fileName, result.Inserted, result.Updated, result.DuplicateRows, result.SkippedEmptyName);

        return result;
    }

    // =========================================================
    // PARSING
    // =========================================================
    private List<ParsedInciRow> ParseWorkbook(Stream stream, InciCatalogImportResult result)
    {
        var rows = new List<ParsedInciRow>();

        using var reader = ExcelReaderFactory.CreateReader(stream);
        do
        {
            var sheetName = reader.Name;
            var headerRowNumber = 0;
            Dictionary<string, int>? header = null;
            var dataRowNumber = 0;

            while (reader.Read())
            {
                dataRowNumber++;

                if (header == null)
                {
                    var candidate = BuildHeaderMap(reader);
                    if (candidate.ContainsKey("name"))
                    {
                        header = candidate;
                        headerRowNumber = dataRowNumber;
                        result.SheetName = sheetName;
                        result.HeaderRowNumber = headerRowNumber;
                    }
                    continue;
                }

                var row = MapRow(reader, header, dataRowNumber);
                if (row != null)
                    rows.Add(row);
            }

            // Χρησιμοποιούμε το πρώτο φύλλο που περιέχει στήλη 'Name'.
            if (header != null)
                return rows;
        } while (reader.NextResult());

        result.FatalError = "Δεν βρέθηκε φύλλο με στήλη 'Name' (αναμενόταν ο κατάλογος INCI).";
        return rows;
    }

    private static Dictionary<string, int> BuildHeaderMap(IExcelDataReader reader)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var c = 0; c < reader.FieldCount; c++)
        {
            var raw = reader.GetValue(c)?.ToString();
            var key = NormalizeKey(raw);
            if (key.Length == 0) continue;

            switch (key)
            {
                case "name": map["name"] = c; break;
                case "an": map["an"] = c; break;
                case "origin": map["origin"] = c; break;
                case "cas": map["cas"] = c; break;
                case "einecs": case "ec": case "ecnumber": map["einecs"] = c; break;
                case "toxprofile": case "toxicologicalprofile": case "tox": map["toxprofile"] = c; break;
                case "restriction": map["restriction"] = c; break;
                case "functions": case "function": map["functions"] = c; break;
            }
        }
        return map;
    }

    private static ParsedInciRow? MapRow(IExcelDataReader reader, Dictionary<string, int> header, int rowNumber)
    {
        string? Get(string key)
        {
            if (!header.TryGetValue(key, out var idx)) return null;
            if (idx >= reader.FieldCount) return null;
            return reader.GetValue(idx)?.ToString();
        }

        var row = new ParsedInciRow
        {
            RowNumber = rowNumber,
            Name = Get("name"),
            Origin = Get("origin"),
            Cas = Get("cas"),
            Einecs = Get("einecs"),
            ToxProfile = Get("toxprofile"),
            Restriction = Get("restriction"),
            Functions = Get("functions")
        };

        var any = !string.IsNullOrWhiteSpace(row.Name)
                  || !string.IsNullOrWhiteSpace(row.Cas)
                  || !string.IsNullOrWhiteSpace(row.Functions);
        return any ? row : null;
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static string NormalizeKey(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    private static string? TrimOrNull(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Θεωρεί "-", "0", "None", "N/A" ως κενά (placeholder του workbook).</summary>
    private static string? NullIfPlaceholder(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        if (t == "-" || t == "—" || t == "0"
            || t.Equals("none", StringComparison.OrdinalIgnoreCase)
            || t.Equals("n/a", StringComparison.OrdinalIgnoreCase))
            return null;
        return t;
    }

    private static string? Truncate(string? value, int maxLength, InciCatalogImportResult result)
    {
        if (value == null) return null;
        if (value.Length <= maxLength) return value;
        result.TruncatedFields++;
        return value[..maxLength];
    }

    private sealed class ParsedInciRow
    {
        public int RowNumber { get; set; }
        public string? Name { get; set; }
        public string? Origin { get; set; }
        public string? Cas { get; set; }
        public string? Einecs { get; set; }
        public string? ToxProfile { get; set; }
        public string? Restriction { get; set; }
        public string? Functions { get; set; }
    }
}
