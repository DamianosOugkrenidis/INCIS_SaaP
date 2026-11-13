// CosIng Database Seeder Service
// Διαβάζει JSON (Annexes II-VI) και CSV (Ingredients Inventory)
// και τα εισάγει στους πίνακες CosIngEntries & CosIngIngredients
// Χρήση: Καλείται από Program.cs ή ως Background Job
//
// Author: INCIS SaaP System
// Database: INCIS_SaaP (SQL Server - PYTHAGORAS)

using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services
{
    public interface ICosIngSeederService
    {
        Task<int> SeedAnnexesFromJsonAsync(string jsonFolderPath);
        Task<int> SeedIngredientsFromCsvAsync(string csvFilePath);
        Task<(int annexCount, int ingredientCount)> SeedAllAsync(string cosIngDatabaseFolder);
    }

    public class CosIngSeederService : ICosIngSeederService
    {
        private readonly INCISDbContext _context;
        private readonly ILogger<CosIngSeederService> _logger;

        public CosIngSeederService(INCISDbContext context, ILogger<CosIngSeederService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Εισαγωγή ΟΛΩΝ των δεδομένων CosIng (JSON Annexes + CSV Ingredients)
        /// </summary>
        public async Task<(int annexCount, int ingredientCount)> SeedAllAsync(string cosIngDatabaseFolder)
        {
            var jsonFolder = Path.Combine(cosIngDatabaseFolder, "updated_2026");
            var csvFile = Path.Combine(cosIngDatabaseFolder, "COSING_Ingredients-Fragrance_Inventory_v2.csv");

            _logger.LogInformation("=== CosIng Database Seeding Started ===");
            _logger.LogInformation("JSON folder: {JsonFolder}", jsonFolder);
            _logger.LogInformation("CSV file: {CsvFile}", csvFile);

            var annexCount = await SeedAnnexesFromJsonAsync(jsonFolder);
            var ingredientCount = await SeedIngredientsFromCsvAsync(csvFile);

            _logger.LogInformation("=== CosIng Database Seeding Complete ===");
            _logger.LogInformation("Total Annex entries: {AnnexCount}", annexCount);
            _logger.LogInformation("Total Ingredient entries: {IngredientCount}", ingredientCount);

            return (annexCount, ingredientCount);
        }

        /// <summary>
        /// Εισαγωγή Annexes II-VI από JSON αρχεία (ενημερωμένα 2026)
        /// </summary>
        public async Task<int> SeedAnnexesFromJsonAsync(string jsonFolderPath)
        {
            var totalInserted = 0;

            var annexFiles = new Dictionary<string, string>
            {
                { "II", "Annex_II_Prohibited.json" },
                { "III", "Annex_III_Restricted.json" },
                { "IV", "Annex_IV_Colorants.json" },
                { "V", "Annex_V_Preservatives.json" },
                { "VI", "Annex_VI_UV_Filters.json" }
            };

            foreach (var (annex, fileName) in annexFiles)
            {
                var filePath = Path.Combine(jsonFolderPath, fileName);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    continue;
                }

                _logger.LogInformation("Processing Annex {Annex}: {FileName}...", annex, fileName);

                var json = await File.ReadAllTextAsync(filePath);
                var entries = JsonSerializer.Deserialize<List<CosIngJsonEntry>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (entries == null || entries.Count == 0)
                {
                    _logger.LogWarning("No entries found in {FileName}", fileName);
                    continue;
                }

                // Διαγραφή παλαιών εγγραφών αυτού του Annex
                var existingCount = await _context.CosIngEntries.CountAsync(e => e.Annex == annex);
                if (existingCount > 0)
                {
                    _logger.LogInformation("Removing {Count} existing Annex {Annex} entries...", existingCount, annex);
                    _context.CosIngEntries.RemoveRange(
                        _context.CosIngEntries.Where(e => e.Annex == annex));
                    await _context.SaveChangesAsync();
                }

                // Εισαγωγή νέων εγγραφών σε batches
                var batch = new List<CosIngEntry>();
                foreach (var entry in entries)
                {
                    batch.Add(new CosIngEntry
                    {
                        EntryNumber = entry.EntryNumber,
                        Annex = annex,
                        ChemicalName = TruncateString(entry.DisplayName ?? entry.ChemicalName, 1000),
                        InciName = TruncateString(entry.InciGlossaryName, 500),
                        CASNumber = TruncateString(entry.CasNumber, 200),
                        ECNumber = TruncateString(entry.EcNumber, 200),
                        ProductType = TruncateString(entry.ProductType, 2000),
                        MaxConcentration = TruncateString(entry.MaxConcentration, 1000),
                        OtherRestrictions = TruncateString(entry.OtherRestrictions, 2000),
                        Warnings = TruncateString(entry.Warnings, 2000),
                        CMR = TruncateString(entry.Cmr, 500),
                        UpdateDate = entry.UpdateDate,
                        Slug = TruncateString(entry.Slug, 300),
                        DataSource = "cosingchecker_api_2026",
                        ImportedAt = DateTime.UtcNow,
                        IsActive = true
                    });

                    if (batch.Count >= 500)
                    {
                        await _context.CosIngEntries.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                        totalInserted += batch.Count;
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    await _context.CosIngEntries.AddRangeAsync(batch);
                    await _context.SaveChangesAsync();
                    totalInserted += batch.Count;
                }

                _logger.LogInformation("Annex {Annex}: {Count} entries imported", annex, entries.Count);
            }

            return totalInserted;
        }

        /// <summary>
        /// Εισαγωγή Ingredients Inventory από CSV (25.780 εγγραφές)
        /// </summary>
        public async Task<int> SeedIngredientsFromCsvAsync(string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                _logger.LogWarning("CSV file not found: {CsvFile}", csvFilePath);
                return 0;
            }

            _logger.LogInformation("Processing Ingredients CSV: {CsvFile}...", csvFilePath);

            // Διαγραφή παλαιών εγγραφών
            var existingCount = await _context.CosIngIngredients.CountAsync();
            if (existingCount > 0)
            {
                _logger.LogInformation("Removing {Count} existing ingredient entries...", existingCount);
                // Bulk delete for performance
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE CosIngIngredients");
            }

            var lines = await File.ReadAllLinesAsync(csvFilePath);
            var totalInserted = 0;
            var batch = new List<CosIngIngredient>();
            var headerFound = false;
            var headerIndex = new Dictionary<string, int>();

            foreach (var line in lines)
            {
                // Βρες τη γραμμή header
                if (!headerFound)
                {
                    if (line.StartsWith("COSING Ref No"))
                    {
                        headerFound = true;
                        var headers = ParseCsvLine(line);
                        for (int i = 0; i < headers.Length; i++)
                        {
                            headerIndex[headers[i].Trim()] = i;
                        }
                    }
                    continue;
                }

                // Skip κενές γραμμές
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var fields = ParseCsvLine(line);
                    if (fields.Length < 3) continue;

                    var ingredient = new CosIngIngredient
                    {
                        CosIngRefNo = ParseIntSafe(GetField(fields, headerIndex, "COSING Ref No")),
                        InciName = TruncateString(GetField(fields, headerIndex, "INCI name")?.Trim(), 500) ?? "UNKNOWN",
                        INNName = TruncateString(GetField(fields, headerIndex, "INN name"), 300),
                        PhEurName = TruncateString(GetField(fields, headerIndex, "Ph. Eur. Name"), 300),
                        CASNumber = TruncateString(GetField(fields, headerIndex, "CAS No"), 200),
                        ECNumber = TruncateString(GetField(fields, headerIndex, "EINECS/ELINCS No"), 200),
                        ChemicalDescription = TruncateString(GetField(fields, headerIndex, "Chem/IUPAC Name / Description"), 2000),
                        Restriction = TruncateString(GetField(fields, headerIndex, "Restriction"), 500),
                        Function = TruncateString(GetField(fields, headerIndex, "Function"), 1000),
                        UpdateDate = GetField(fields, headerIndex, "Update Date"),
                        DataSource = "github_csv_2016",
                        ImportedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    // Skip αν δεν έχει INCI name
                    if (string.IsNullOrWhiteSpace(ingredient.InciName) || ingredient.InciName == "UNKNOWN")
                        continue;

                    batch.Add(ingredient);

                    if (batch.Count >= 1000)
                    {
                        await _context.CosIngIngredients.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                        totalInserted += batch.Count;
                        _logger.LogInformation("  Imported {Count} ingredients so far...", totalInserted);
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error parsing CSV line: {Error}", ex.Message);
                }
            }

            if (batch.Count > 0)
            {
                await _context.CosIngIngredients.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                totalInserted += batch.Count;
            }

            _logger.LogInformation("Ingredients Inventory: {Count} entries imported", totalInserted);
            return totalInserted;
        }

        #region Helper Methods

        private static string? TruncateString(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private static int? ParseIntSafe(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value.Trim(), out var result) ? result : null;
        }

        private static string? GetField(string[] fields, Dictionary<string, int> headerIndex, string columnName)
        {
            if (!headerIndex.TryGetValue(columnName, out var index)) return null;
            if (index >= fields.Length) return null;
            var val = fields[index].Trim().Trim('"');
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }

        /// <summary>
        /// Parse CSV line respecting quoted fields with commas inside
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            fields.Add(current);
            return fields.ToArray();
        }

        #endregion

        #region JSON DTOs

        private class CosIngJsonEntry
        {
            public int? Id { get; set; }
            public string? Slug { get; set; }
            public string? Url { get; set; }
            public string? Annex { get; set; }
            public string? EntryNumber { get; set; }
            public string? DisplayName { get; set; }
            public string? ChemicalName { get; set; }
            public string? InciGlossaryName { get; set; }
            public string? CasNumber { get; set; }
            public string? EcNumber { get; set; }
            public string? ProductType { get; set; }
            public string? MaxConcentration { get; set; }
            public string? Warnings { get; set; }
            public string? OtherRestrictions { get; set; }
            public string? Cmr { get; set; }
            public string? UpdateDate { get; set; }
        }

        #endregion
    }
}
