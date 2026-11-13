using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// EU CosIng Ingredients and Fragrance Inventory.
    /// Πλήρης κατάλογος INCI ονομάτων με λειτουργίες.
    /// Πηγή: CosIng Database (EC) 1223/2009 - Inventory.
    /// </summary>
    public class CosIngIngredient
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// CosIng Reference Number
        /// </summary>
        public int? CosIngRefNo { get; set; }

        /// <summary>
        /// INCI Name (International Nomenclature of Cosmetic Ingredients)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string InciName { get; set; } = string.Empty;

        /// <summary>
        /// INN Name (International Non-proprietary Name)
        /// </summary>
        [MaxLength(300)]
        public string? INNName { get; set; }

        /// <summary>
        /// Ph. Eur. Name (European Pharmacopoeia)
        /// </summary>
        [MaxLength(300)]
        public string? PhEurName { get; set; }

        /// <summary>
        /// CAS Number
        /// </summary>
        [MaxLength(200)]
        public string? CASNumber { get; set; }

        /// <summary>
        /// EINECS/ELINCS Number
        /// </summary>
        [MaxLength(200)]
        public string? ECNumber { get; set; }

        /// <summary>
        /// Chemical / IUPAC Name / Description
        /// </summary>
        [MaxLength(2000)]
        public string? ChemicalDescription { get; set; }

        /// <summary>
        /// Restriction reference (link to Annex)
        /// </summary>
        [MaxLength(500)]
        public string? Restriction { get; set; }

        /// <summary>
        /// Cosmetic Function(s) - π.χ. "SKIN CONDITIONING, EMOLLIENT"
        /// </summary>
        [MaxLength(1000)]
        public string? Function { get; set; }

        /// <summary>
        /// Ημερομηνία ενημέρωσης CosIng
        /// </summary>
        [MaxLength(20)]
        public string? UpdateDate { get; set; }

        /// <summary>
        /// Ημερομηνία εισαγωγής στη βάση μας
        /// </summary>
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Πηγή δεδομένων
        /// </summary>
        [MaxLength(100)]
        public string? DataSource { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
