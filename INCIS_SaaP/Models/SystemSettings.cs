using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Συστημικές ρυθμίσεις (π.χ. κωδικοί πρόσβασης)
    /// </summary>
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Κωδικός πρόσβασης για την ενότητα Α' Υλών
        /// </summary>
        [MaxLength(100)]
        public string? RawMaterialPassword { get; set; }

        /// <summary>
        /// Κωδικός πρόσβασης για την ενότητα Φορμουλών
        /// </summary>
        [MaxLength(100)]
        public string? FormulasPassword { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }
        public bool IsActive { get; set; } = true;
    }
}