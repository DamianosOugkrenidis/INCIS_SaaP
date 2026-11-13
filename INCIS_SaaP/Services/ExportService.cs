// Author: Damianos Pythagoras Ougkrenidis
//
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για εξαγωγή δεδομένων σε Excel και PDF
/// </summary>
public class ExportService
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ExportService> _logger;

    public ExportService(INCISDbContext context, ILogger<ExportService> logger)
    {
        _context = context;
        _logger = logger;
        
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Εξαγωγή φόρμουλας σε Excel
    /// </summary>
    public async Task<byte[]> ExportFormulasToExcelAsync()
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Formulas");

            // Headers
            var headers = new[] { "Κωδικός", "Όνομα", "Τύπος Προϊόντος", "Έκδοση", "Ημ/νία Δημιουργίας", "Τελευταία Τροποποίηση" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a237e");
                worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            // Data
            var formulas = await _context.Formulas.AsNoTracking().ToListAsync();
            int row = 2;
            foreach (var formula in formulas)
            {
                worksheet.Cell(row, 1).Value = formula.FormulaCode;
                worksheet.Cell(row, 2).Value = formula.FormulaName ?? "";
                worksheet.Cell(row, 3).Value = formula.ProductType ?? "";
                worksheet.Cell(row, 4).Value = formula.Version ?? "";
                worksheet.Cell(row, 5).Value = formula.CreationDate?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cell(row, 6).Value = formula.LastModified?.ToString("dd/MM/yyyy") ?? "";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to Excel");
            throw;
        }
    }

    /// <summary>
    /// Εξαγωγή φόρμουλας σε PDF
    /// </summary>
    public async Task<byte[]> ExportFormulaToPdfAsync(int formulaId)
    {
        try
        {
            var formula = await _context.Formulas
                .AsNoTracking()
                .Include(f => f.INCIIngredients)
                .Include(f => f.Allergens)
                .Include(f => f.SafetyData)
                .FirstOrDefaultAsync(f => f.Id == formulaId);

            if (formula == null)
                throw new ArgumentException("Formula not found");

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(c => ComposeContent(c, formula));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Σελίδα ");
                        x.CurrentPageNumber();
                        x.Span(" από ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to PDF");
            throw;
        }
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("INCIS SaaP").Bold().FontSize(20).FontColor(Colors.Blue.Darken3);
                col.Item().Text("Έκθεση Φόρμουλας Καλλυντικού").FontSize(12).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    private void ComposeContent(IContainer container, Formula formula)
    {
        container.PaddingVertical(20).Column(col =>
        {
            col.Item().Text($"Κωδικός: {formula.FormulaCode}").Bold();
            col.Item().Text($"Όνομα: {formula.FormulaName ?? "N/A"}");
            col.Item().Text($"Τύπος Προϊόντος: {formula.ProductType ?? "N/A"}");
            col.Item().Text($"Έκδοση: {formula.Version ?? "N/A"}");
            
            col.Item().PaddingTop(10).Text("Συστατικά INCI").Bold().FontSize(14);
            
            if (formula.INCIIngredients.Any())
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1);
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Όνομα INCI").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Λειτουργία").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("%").FontColor(Colors.White).Bold();
                    });

                    foreach (var ingredient in formula.INCIIngredients.Take(10))
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(ingredient.INCIName ?? "");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(ingredient.Function ?? "");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(ingredient.Percentage?.ToString() ?? "");
                    }
                });
            }
        });
    }

    private static Task<List<Formula>> ToListAsync<T>(IQueryable<Formula> query)
    {
        return Task.FromResult(query.ToList());
    }
}
