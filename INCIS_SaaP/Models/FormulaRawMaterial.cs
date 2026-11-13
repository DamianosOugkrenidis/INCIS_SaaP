using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Σύνδεση φόρμουλας και α' ύλης με ποσοστό συμμετοχής και φάση προσθήκης
    /// </summary>
    public class FormulaRawMaterial
    {
        [Key]
        public int Id { get; set; }

        public int FormulaId { get; set; }
        [ForeignKey("FormulaId")]
        public virtual Formula? Formula { get; set; }

        public int RawMaterialId { get; set; }
        [ForeignKey("RawMaterialId")]
        public virtual RawMaterial? RawMaterial { get; set; }

        /// <summary>
        /// % Συμμετοχής στη φόρμουλα
        /// </summary>
        public decimal? Percentage { get; set; }

        /// <summary>
        /// Φάση προσθήκης στην παραγωγική διαδικασία
        /// </summary>
        [MaxLength(100)]
        public string? Phase { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}