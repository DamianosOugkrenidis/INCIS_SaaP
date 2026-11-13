using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Α' ύλη (Raw Material) - Βασική οντότητα για διαχείριση αποθήκης
    /// </summary>
    public class RawMaterial : IOwnedEntity, ITenantOwned
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
        public string Code { get; set; } = string.Empty; // ID ή εσωτερικός κωδικός

        /// <summary>
        /// Εξωτερικός κωδικός αναφοράς της α' ύλης στο σύστημα "Prisma"
        /// (π.χ. 031900001). Διαφέρει από τον εσωτερικό Code (L-code).
        /// </summary>
        [MaxLength(100)]
        public string? PrismaCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty; // Όπως στο TDS

        // Manufacturer information (from MSDS)
        [MaxLength(200)]
        public string? ManufacturerName { get; set; }

        [MaxLength(200)]
        public string? ManufacturerAddress { get; set; }

        [MaxLength(100)]
        public string? ManufacturerPhone { get; set; }

        [MaxLength(100)]
        public string? ManufacturerEmail { get; set; }

        // Allergens (for fragrances, essential oils, etc.)
        [MaxLength(500)]
        public string? Allergens { get; set; }

        // % Natural origin content (based on ISO 16128)
        public decimal? NaturalOriginPercentage { get; set; }

        // Physicochemistry data
        [MaxLength(100)]
        public string? ShelfLife { get; set; } // shelf life

        [MaxLength(200)]
        public string? StorageConditions { get; set; } // συνθήκες αποθήκευσης

        [MaxLength(100)]
        public string? Form { get; set; } // μορφή

        [MaxLength(100)]
        public string? Color { get; set; } // χρώμα

        [MaxLength(100)]
        public string? Odor { get; set; } // ιξώδες

        public decimal? Density { get; set; } // πυκνότητα

        [MaxLength(200)]
        public string? Solubility { get; set; } // διαλυτότητα

        [MaxLength(500)]
        public string? Compatibilities { get; set; } // συμβατότητες

        // Viscosity (ιξώδες)
        [MaxLength(100)]
        public string? Viscosity { get; set; }

        // Price (υψηλότερη τιμή/ανασυσκευασία)
        public decimal? Price { get; set; }

        /// <summary>
        /// Ελάχιστη ποσότητα παραγγελίας (MOQ)
        /// </summary>
        [MaxLength(100)]
        public string? MOQ { get; set; }

        /// <summary>
        /// Χρόνος παράδοσης
        /// </summary>
        [MaxLength(100)]
        public string? DeliveryTime { get; set; }

        /// <summary>
        /// Περιεχόμενο συσκευασίας (π.χ. 25kg drum)
        /// </summary>
        [MaxLength(100)]
        public string? PackagingContent { get; set; }

        // H&S (Οδηγίες ασφαλείς χειρισμού & αποθήκευσης από MSDS)
        [MaxLength(1000)]
        public string? HealthAndSafety { get; set; }

        // Comments (MOQ, χρόνος παράδοσης, συσκευασία)
        [MaxLength(500)]
        public string? Comments { get; set; }

        // Product Category (επιλογή από λίστα: Preservatives, UV Filters, Emollients, κλπ)
        [MaxLength(100)]
        public string? Category { get; set; }

        // Document tracking (as per manual requirements)
        public bool HasTDS { get; set; } = false; // Technical Data Sheet
        public bool HasMSDS { get; set; } = false; // Material Safety Data Sheet
        public bool HasISO16128 { get; set; } = false; // ISO 16128 statement for natural origin
        public bool HasBSE_TSE { get; set; } = false; // BSE/TSE certificate
        public bool HasVegan { get; set; } = false; // Vegan certificate
        public bool HasRMI { get; set; } = false; // RMI (impurities, heavy metals)
        public bool HasAllergenCertificate { get; set; } = false; // For fragrances (after 30/6/2021)
        public bool HasPhthalates_BHT_Statement { get; set; } = false; // For fragrances
        public bool HasSolventVehicleStatement { get; set; } = false; // For fragrances
        public bool HasMarketingInfo { get; set; } = false; // Marketing information

        // Σχέσεις
        public int? SupplierId { get; set; } // Διανομέας στην Ελλάδα
        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        public int? INCIId { get; set; } // Основное вещество (для простых материалов)
        [ForeignKey("INCIId")]
        public virtual INCI? MainINCI { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Υποδεικτικό !: Εμφανίζεται στο Product Name αν η α' ύλη δεν είναι πλήρως καταχωρημένη ή αξιολογημένη
        public bool IsFullyRegistered { get; set; } = false;
        public bool IsEvaluated { get; set; } = false;

        // Σύνθεση (Composition): Καταχώρηση συμμετεχόντων ουσιών (INCI) με ελάχιστο/μέγιστο ποσοστό
        public virtual ICollection<RawMaterialComposition> Composition { get; set; } = new List<RawMaterialComposition>();

        // Ένδειξη για δειγματα
        public virtual ICollection<RawMaterialSample> Samples { get; set; } = new List<RawMaterialSample>();

        // Claims per raw material (Vegan, Gluten-Free, etc.)
        public virtual RawMaterialClaim? Claims { get; set; }

        // Reverse relationship for FormulaRawMaterial
        public virtual ICollection<FormulaRawMaterial> FormulaRawMaterials { get; set; } = new List<FormulaRawMaterial>();
    }

    /// <summary>
    /// Σύνθεση α' ύλης: Συμμετέχουσες ουσίες (INCI) με min/max %
    /// </summary>
    public class RawMaterialComposition
    {
        [Key]
        public int Id { get; set; }

        public int RawMaterialId { get; set; }
        [ForeignKey("RawMaterialId")]
        public virtual RawMaterial? RawMaterial { get; set; }

        public int INCIId { get; set; }
        [ForeignKey("INCIId")]
        public virtual INCI? INCI { get; set; }

        public decimal? MinPercentage { get; set; } // Ελάχιστο ποσοστό
        public decimal? MaxPercentage { get; set; } // Μέγιστο ποσοστό

        public bool IsImpurity { get; set; } = false; // Εξαιρούνται οι impurities από την σύνθεση
    }

    /// <summary>
    /// Δείγμα α' ύλης για αποθήκη εργαστηρίου
    /// </summary>
    public class RawMaterialSample
    {
        [Key]
        public int Id { get; set; }

        public int RawMaterialId { get; set; }
        [ForeignKey("RawMaterialId")]
        public virtual RawMaterial? RawMaterial { get; set; }

        [Required]
        [MaxLength(50)]
        public string LotNumber { get; set; } = string.Empty; // Αριθμό παρτίδας (Lot No)

        public DateTime ReceivedDate { get; set; } // Ημερομηνία παραλαβής
        public DateTime ExpiryDate { get; set; } // Ημερομηνία λήξης

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}