using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Προμηθεύς (εμπορικός αντιπρόσωπος στην Ελλάδα)
    /// </summary>
    public class Supplier : IOwnedEntity, ITenantOwned
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Χρήστης-δημιουργός (audit). Null = legacy.</summary>
        [MaxLength(450)]
        public string? OwnerId { get; set; }

        /// <summary>Tenant (εταιρεία) — multi-tenant isolation. Null = κοινό/legacy.</summary>
        public int? TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        /// <summary>
        /// ΑΦΜ προμηθευτή
        /// </summary>
        [MaxLength(20)]
        public string? AFM { get; set; }

        /// <summary>
        /// ΔΟΥ προμηθευτή
        /// </summary>
        [MaxLength(100)]
        public string? DOY { get; set; }

        /// <summary>
        /// Σχόλια / Παρατηρήσεις
        /// </summary>
        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Σχέσεις
        public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();
    }
}