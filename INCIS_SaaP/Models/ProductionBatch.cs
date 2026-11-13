using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Παρτίδα παραγωγής - Αντιπροσωπεύει μία παραγωγή φόρμουλας με συγκεκριμένο μέγεθος batch
    /// </summary>
    public class ProductionBatch
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Κωδικός παρτίδας παραγωγής (μοναδικός)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string BatchCode { get; set; } = string.Empty;

        /// <summary>
        /// Αναφορά στη φόρμουλα που παράγεται
        /// </summary>
        public int FormulaId { get; set; }
        [ForeignKey("FormulaId")]
        public virtual Formula? Formula { get; set; }

        /// <summary>
        /// Συνολικό μέγεθος παρτίδας σε kg (ή ml)
        /// </summary>
        [Required]
        public decimal BatchSize { get; set; }

        /// <summary>
        /// Μονάδα μέτρησης (kg, ml, L)
        /// </summary>
        [MaxLength(20)]
        public string Unit { get; set; } = "kg";

        /// <summary>
        /// Ημερομηνία παραγωγής
        /// </summary>
        public DateTime ProductionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Κατάσταση παραγωγής
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Planned"; // Planned, InProgress, Completed, Cancelled

        /// <summary>
        /// Χειριστής παραγωγής
        /// </summary>
        [MaxLength(100)]
        public string? Operator { get; set; }

        /// <summary>
        /// Τοποθεσία παραγωγής (Manufacturing Site)
        /// </summary>
        [MaxLength(100)]
        public string? ManufacturingSite { get; set; }

        /// <summary>
        /// Σημειώσεις παραγωγής
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Ημερομηνία δημιουργίας εγγραφής
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Ημερομηνία ολοκλήρωσης
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Ενεργή εγγραφή
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Αριθμός LOT παρτίδας παραγωγής
        /// </summary>
        [MaxLength(100)]
        public string? LotNumber { get; set; }

        /// <summary>
        /// Γραμμές παρτίδας - ποσότητες κάθε α' ύλης
        /// </summary>
        public virtual ICollection<ProductionBatchLine> Lines { get; set; } = new List<ProductionBatchLine>();
    }

    /// <summary>
    /// Γραμμή παρτίδας παραγωγής - Ποσότητα κάθε α' ύλης βάσει batch size
    /// </summary>
    public class ProductionBatchLine
    {
        [Key]
        public int Id { get; set; }

        public int ProductionBatchId { get; set; }
        [ForeignKey("ProductionBatchId")]
        public virtual ProductionBatch? ProductionBatch { get; set; }

        /// <summary>
        /// ID α' ύλης (null = νερό/AQUA)
        /// </summary>
        public int? RawMaterialId { get; set; }
        [ForeignKey("RawMaterialId")]
        public virtual RawMaterial? RawMaterial { get; set; }

        /// <summary>
        /// Ποσοστό (%) από τη φόρμουλα
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Υπολογισμένη ποσότητα βάσει batch size (π.χ. 5% x 50kg = 2.5kg)
        /// </summary>
        public decimal CalculatedQuantity { get; set; }

        /// <summary>
        /// Πραγματική ποσότητα που ζυγίστηκε
        /// </summary>
        public decimal? ActualQuantity { get; set; }

        /// <summary>
        /// Φάση προσθήκης στην παραγωγική διαδικασία
        /// </summary>
        [MaxLength(100)]
        public string? Phase { get; set; }

        /// <summary>
        /// Σειρά προσθήκης
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// Σημειώσεις
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
