using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Ουσία INCI (Διεθνής ονομασία καλλυντικών συστατικών)
    /// </summary>
    public class INCI
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty; // INCI name

        [MaxLength(200)]
        public string? CASNumber { get; set; }

        [MaxLength(200)]
        public string? ECNumber { get; set; }

        // Additional fields for detailed view
        [MaxLength(500)]
        public string? Description { get; set; } // Περιγραφή ουσίας

        [MaxLength(200)]
        public string? Function { get; set; } // Λειτουργία (π.χ. preservative, emulsifier)

        [MaxLength(200)]
        public string? Synonyms { get; set; } // Συωνύμια

        [MaxLength(200)]
        public string? Origin { get; set; } // Προέλευση ουσίας

        [MaxLength(500)]
        public string? Restriction { get; set; } // Αναφορές περιορισμών (π.χ. "III/226")

        public string? ToxicologicalProfile { get; set; } // Συγκεντρωτικό τοξικολογικό προφίλ

        /// <summary>
        /// Το πλήρες, μη περικομμένο INCI όνομα από την πηγή (ALL_INCIS_NEW_FILE).
        /// Το <see cref="Name"/> παραμένει ευρετηριοποιήσιμο/αναζητήσιμο (έως 500 χαρ.),
        /// ενώ εδώ διατηρείται η πηγή αυτούσια (χωρίς απώλεια).
        /// </summary>
        public string? FullName { get; set; } // Πλήρες όνομα πηγής (nvarchar(max))

        [MaxLength(100)]
        public string? DataSource { get; set; } // Πηγή εισαγωγής δεδομένων (π.χ. "ALL_INCIS_NEW_FILE")

        public DateTime? ImportedAt { get; set; } // Πότε εισήχθη από την πηγή

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Σχέσεις
        public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();
        public virtual ICollection<Restriction> Restrictions { get; set; } = new List<Restriction>();
    }
}