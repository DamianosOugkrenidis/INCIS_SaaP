using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Πελάτης του συστήματος
    /// </summary>
    public class Client : IOwnedEntity, ITenantOwned
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Χρήστης-δημιουργός (audit). Null = legacy.
        /// </summary>
        [MaxLength(450)]
        public string? OwnerId { get; set; }

        /// <summary>
        /// Tenant (εταιρεία) — multi-tenant isolation. Null = κοινό/legacy.
        /// </summary>
        public int? TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; } // Κωδικός από το PP σύστημα

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        // Φορολογικά στοιχεία
        /// <summary>
        /// ΑΦΜ πελάτη
        /// </summary>
        [MaxLength(20)]
        public string? AFM { get; set; }

        /// <summary>
        /// ΔΟΥ πελάτη
        /// </summary>
        [MaxLength(100)]
        public string? DOY { get; set; }

        /// <summary>
        /// Σχόλια / Παρατηρήσεις
        /// </summary>
        [MaxLength(1000)]
        public string? Comments { get; set; }

        // Πληροφορίες επαφής
        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Σχέσεις
        public virtual ICollection<Formula> Formulas { get; set; } = new List<Formula>();
    }
}