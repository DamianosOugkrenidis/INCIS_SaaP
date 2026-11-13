using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Νομοθετικός περιορισμός για ουσία INCI
    /// </summary>
    public class Restriction
    {
        [Key]
        public int Id { get; set; }

        public int INCIId { get; set; }

        [ForeignKey("INCIId")]
        public virtual INCI? INCI { get; set; }

        [MaxLength(200)]
        public string? Regulation { get; set; } // π.χ. Κανονισμός 1223/2009

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal? MaxPercentage { get; set; } // Μέγιστο επιτρεπόμενο ποσοστό

        public bool IsRinseOffOnly { get; set; } // Μόνο γιαinsi off προϊόντα

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}