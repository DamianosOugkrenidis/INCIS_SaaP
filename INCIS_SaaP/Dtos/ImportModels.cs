// Author: Damianos Pythagoras Ougkrenidis
//
namespace INCIS_SaaP.Dtos;

/// <summary>
/// Υποστηριζόμενοι τύποι μαζικής εισαγωγής (bulk import) από Excel.
/// </summary>
public enum ImportEntityType
{
    Clients,
    Suppliers,
    INCIs,
    RawMaterials,
    Formulas
}

/// <summary>
/// Σφάλμα/προειδοποίηση επιπέδου γραμμής κατά την εισαγωγή.
/// </summary>
public class ImportRowIssue
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>true = σφάλμα (η γραμμή αγνοείται), false = προειδοποίηση.</summary>
    public bool IsError { get; set; } = true;
}

/// <summary>
/// Μία γραμμή προεπισκόπησης με τα βασικά της πεδία (για εμφάνιση στο UI).
/// </summary>
public class ImportPreviewRow
{
    public int RowNumber { get; set; }
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public string Status { get; set; } = "ok"; // ok | duplicate | error
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Αποτέλεσμα parse + validation (προεπισκόπηση).
/// </summary>
public class ImportPreviewResult
{
    public ImportEntityType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int DuplicateRows { get; set; }
    public int ErrorRows { get; set; }
    public List<ImportPreviewRow> Rows { get; set; } = new();
    public List<ImportRowIssue> Issues { get; set; } = new();
    /// <summary>Γενικό σφάλμα (π.χ. μη έγκυρο αρχείο) που εμποδίζει κάθε εισαγωγή.</summary>
    public string? FatalError { get; set; }
    public bool CanImport => FatalError == null && ValidRows > 0;
}

/// <summary>
/// Αποτέλεσμα οριστικής εισαγωγής (commit).
/// </summary>
public class ImportCommitResult
{
    public ImportEntityType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int SkippedDuplicates { get; set; }
    public int SkippedErrors { get; set; }
    public List<ImportRowIssue> Issues { get; set; } = new();
    public string? FatalError { get; set; }
    public bool Success => FatalError == null;
}
