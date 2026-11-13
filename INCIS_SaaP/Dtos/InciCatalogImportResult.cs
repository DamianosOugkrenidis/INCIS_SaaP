// Author: Damianos Pythagoras Ougkrenidis
//
namespace INCIS_SaaP.Dtos;

/// <summary>
/// Αποτέλεσμα εισαγωγής του καταλόγου INCI από το αρχείο ALL_INCIS_NEW_FILE.
/// </summary>
public class InciCatalogImportResult
{
    public string FileName { get; set; } = string.Empty;
    public string? SheetName { get; set; }
    public int HeaderRowNumber { get; set; }

    public int TotalDataRows { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int SkippedEmptyName { get; set; }
    public int DuplicateRows { get; set; }
    public int TruncatedFields { get; set; }
    public int ErrorRows { get; set; }

    public string DataSource { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();

    /// <summary>Γενικό σφάλμα που εμποδίζει κάθε εισαγωγή.</summary>
    public string? FatalError { get; set; }

    public bool Success => FatalError == null;
    public int Applied => Inserted + Updated;
}
