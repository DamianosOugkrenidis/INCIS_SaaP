using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// EU CosIng Database Entry - Παραρτήματα II-VI Κανονισμού (EC) 1223/2009
    /// Πηγή: https://ec.europa.eu/growth/tools-databases/cosing/
    /// </summary>
    public class CosIngEntry
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Αριθμός αναφοράς στο Παράρτημα (π.χ. "1", "2a", "1000")
        /// </summary>
        [MaxLength(20)]
        public string? EntryNumber { get; set; }

        /// <summary>
        /// Παράρτημα: II, III, IV, V, VI
        /// </summary>
        [Required]
        [MaxLength(5)]
        public string Annex { get; set; } = string.Empty;

        /// <summary>
        /// Χημική ονομασία / INN
        /// </summary>
        [MaxLength(1000)]
        public string? ChemicalName { get; set; }

        /// <summary>
        /// INCI Name (Common Ingredients Glossary)
        /// </summary>
        [MaxLength(500)]
        public string? InciName { get; set; }

        /// <summary>
        /// CAS Number
        /// </summary>
        [MaxLength(200)]
        public string? CASNumber { get; set; }

        /// <summary>
        /// EC Number (EINECS/ELINCS)
        /// </summary>
        [MaxLength(200)]
        public string? ECNumber { get; set; }

        /// <summary>
        /// Τύπος προϊόντος / Μέρη σώματος
        /// </summary>
        [MaxLength(2000)]
        public string? ProductType { get; set; }

        /// <summary>
        /// Μέγιστη συγκέντρωση σε έτοιμο παρασκεύασμα
        /// </summary>
        [MaxLength(1000)]
        public string? MaxConcentration { get; set; }

        /// <summary>
        /// Λοιποί περιορισμοί
        /// </summary>
        [MaxLength(2000)]
        public string? OtherRestrictions { get; set; }

        /// <summary>
        /// Προειδοποιήσεις / Όροι χρήσης
        /// </summary>
        [MaxLength(2000)]
        public string? Warnings { get; set; }

        /// <summary>
        /// Κανονισμός (π.χ. "(EC) No 1223/2009")
        /// </summary>
        [MaxLength(200)]
        public string? Regulation { get; set; }

        /// <summary>
        /// CMR Classification (Carcinogenic/Mutagenic/Reprotoxic)
        /// </summary>
        [MaxLength(500)]
        public string? CMR { get; set; }

        /// <summary>
        /// Colour Index Number (Annex IV only)
        /// </summary>
        [MaxLength(50)]
        public string? ColourIndex { get; set; }

        /// <summary>
        /// Χρώμα (Annex IV only)
        /// </summary>
        [MaxLength(50)]
        public string? Colour { get; set; }

        /// <summary>
        /// SCCS Opinions (γνωμοδοτήσεις επιστημονικής επιτροπής)
        /// </summary>
        [MaxLength(2000)]
        public string? SCCSOpinions { get; set; }

        /// <summary>
        /// Ημερομηνία τελευταίας ενημέρωσης στο CosIng
        /// </summary>
        [MaxLength(20)]
        public string? UpdateDate { get; set; }

        /// <summary>
        /// Slug URL (για API reference)
        /// </summary>
        [MaxLength(300)]
        public string? Slug { get; set; }

        /// <summary>
        /// Ημερομηνία εισαγωγής στη βάση μας
        /// </summary>
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Πηγή δεδομένων (π.χ. "cosingchecker_api_2026", "github_csv_2016")
        /// </summary>
        [MaxLength(100)]
        public string? DataSource { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
