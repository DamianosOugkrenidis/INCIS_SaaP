// Author: Damianos Pythagoras Ougkrenidis
//
namespace INCIS_SaaP.Dtos;

/// <summary>
/// Στοιχείο λίστας φόρμουλας στην αρχική της Διαχείρισης Προϊόντος.
/// </summary>
public class ProductFormulaListItem
{
    public int Id { get; set; }
    public string FormulaCode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? ProductType { get; set; }
    public string? ClientName { get; set; }
    public string? Version { get; set; }
    public DateTime? LastModified { get; set; }
    public int DataPointsCount { get; set; }
    /// <summary>Ποσοστό πληρότητας δεδομένων (0-100).</summary>
    public int CompletenessPercent { get; set; }
}

/// <summary>
/// Hub διαχείρισης μιας φόρμουλας: εμφανίζει κάρτες ανά κατηγορία δεδομένων
/// με τα αντίστοιχα πλήθη εγγραφών και ενέργειες CRUD.
/// </summary>
public class ProductManageViewModel
{
    public int FormulaId { get; set; }
    public string FormulaCode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? ProductType { get; set; }
    public string? ClientName { get; set; }
    public string? Version { get; set; }
    public List<ProductDataCategory> Categories { get; set; } = new();

    /// <summary>Κατηγορίες που μετρούν για την πληρότητα (εξαιρείται το masterinfo).</summary>
    private IEnumerable<ProductDataCategory> ScoredCategories =>
        Categories.Where(c => c.Key != "masterinfo");

    public int CompletenessPercent
    {
        get
        {
            var scored = ScoredCategories.ToList();
            if (scored.Count == 0) return 0;
            return (int)Math.Round(100.0 * scored.Count(c => c.HasData) / scored.Count);
        }
    }

    public List<string> MissingCategories =>
        ScoredCategories.Where(c => !c.HasData).Select(c => c.Title).ToList();
}

/// <summary>
/// Κατηγορία δεδομένων προϊόντος (αντιστοιχεί σε μία φόρμα/πίνακα).
/// </summary>
public class ProductDataCategory
{
    public string Key { get; set; } = string.Empty;
    public string FormNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
    /// <summary>Action που ανοίγει τη λίστα CRUD της κατηγορίας.</summary>
    public string ActionName { get; set; } = string.Empty;
    /// <summary>Πλήθος καταχωρημένων εγγραφών για τη φόρμουλα.</summary>
    public int Count { get; set; }
    /// <summary>True αν η κατηγορία είναι single-record (1:1 με τη φόρμουλα).</summary>
    public bool IsSingleRecord { get; set; }
    public bool HasData => Count > 0;
}
