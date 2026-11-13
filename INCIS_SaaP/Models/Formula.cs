using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models
{
    /// <summary>
    /// Κύρια οντότητα φόρμουλας καλλυντικού
    /// </summary>
    public class Formula : IOwnedEntity, ITenantOwned
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
        public string FormulaCode { get; set; } = string.Empty; // Customer code-abcd-T
         
        [MaxLength(200)]
        public string? ProductName { get; set; } // Product name

        /// <summary>
        /// Φιλικό όνομα φόρμουλας (alias του ProductName για συμβατότητα views/db).
        /// </summary>
        [MaxLength(200)]
        public string? FormulaName { get; set; }

        /// <summary>
        /// Έκδοση φόρμουλας (π.χ. v1.0, v2.1)
        /// </summary>
        [MaxLength(50)]
        public string? Version { get; set; }

        /// <summary>
        /// Περιγραφή φόρμουλας
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? ProductType { get; set; } // Leave on or Rinse off (SOS)
         
        [MaxLength(100)]
        public string? ManufacturingSite { get; set; } // Manufacturing site
         
        [MaxLength(100)]
        public string? ManufacturingMethod { get; set; } // Μέθοδος παραγωγής
         
        [MaxLength(50)]
        public string? PPIdCode { get; set; } // Id code (γνωστός από PP)
         
        public DateTime? IssueDate { get; set; } // Issue date (SOS)
        public DateTime? CreationDate { get; set; }
        public DateTime? LastModified { get; set; }
         
        [MaxLength(200)]
        public string? PackagingInfo { get; set; } // Packaging info
         
        // Physicochemical and microbiological properties
        [MaxLength(500)]
        public string? PhysicochemicalProperties { get; set; }
         
        [MaxLength(500)]
        public string? MicrobiologicalProperties { get; set; }
         
        [MaxLength(500)]
        public string? Comments { get; set; }
         
        // Client relationship
        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }
         
        // Φόρμουλα properties based on manual
        /// <summary>
        /// Αριθμός τεστ από δελτίο εργαστηριακού/πιλοτικού δείγματος
        /// </summary>
        public int? TestNumber { get; set; }
         
        /// <summary>
        /// Υποδεικνύει αν η ανάπτυξη γίνεται εκτός Beauty lab (T00)
        /// </summary>
        public bool IsExternalDevelopment { get; set; }
         
        /// <summary>
        /// Υποδεικνύει αν η φόρμουλα είναι σε εξέλιξη (WIP - Work In Progress)
        /// Το '?' προστίθεται στον κωδικό όταν είναι true
        /// </summary>
        public bool IsWIP { get; set; }
         
        /// <summary>
        /// Ημερομηνία όταν η φόρμουλα γίνεται valid από τον RA Head
        /// </summary>
        public DateTime? ValidationDate { get; set; }
         
        /// <summary>
        /// Υποδεικνύει αν η φόρμουλα είναι επικυρωμένη (valid) από τον RA Head
        /// </summary>
        [NotMapped]
        public bool IsValidated => ValidationDate.HasValue;
         
        // Σχέσεις με άλλες οντότητες
        public virtual ICollection<INCIIngredient> INCIIngredients { get; set; } = new List<INCIIngredient>();
        public virtual SafetyData? SafetyData { get; set; }
        public virtual PriceInfo? PriceInfo { get; set; }
        public virtual ICollection<Allergen> Allergens { get; set; } = new List<Allergen>();
        public virtual ICollection<HeavyMetal> HeavyMetals { get; set; } = new List<HeavyMetal>();
        public virtual MarketingInfo? MarketingInfo { get; set; }
        public virtual ICollection<FormulaRawMaterial> FormulaRawMaterials { get; set; } = new List<FormulaRawMaterial>();

        /// <summary>
        /// Συντόμευση των FormulaRawMaterials για συμβατότητα με παλαιότερο κώδικα/views.
        /// </summary>
        [NotMapped]
        public ICollection<FormulaRawMaterial> RawMaterials
        {
            get => FormulaRawMaterials;
            set => FormulaRawMaterials = value;
        }

        public virtual NaturalCertification? NaturalCertification { get; set; }
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public virtual ICollection<ToxicologicalData> ToxicologicalData { get; set; } = new List<ToxicologicalData>();
        
        /// <summary>
        /// Υπολογίζει και επιστρέφει τον κωδικό φόρμουλας βάσει των ιδιοτήτων
        /// Customer code-abcd-T
        /// T = Αριθμός τεστ από δελτίο εργαστηριακού/πιλοτικού δείγματος
        /// T00 = Αν η ανάπτυξη γίνεται εκτός Beauty lab
        /// ? = Προστίθεται αν η φórmουλα είναι WIP (Work In Progress)
        /// </summary>
        /// <param name="customerCode">Ο κωδικός πελάτη από το PP σύστημα</param>
        /// <param name="abcdCode">Κωδικός που προκύπτει από το PP (π.χ. 0001)</param>
        /// <returns>Ο γεννημένος κωδικός φόρμουλας</returns>
        public string GenerateFormulaCode(string customerCode, string abcdCode)
        {
            // Βάσει του manual: Customer code-abcd-T
            string testPart;
            
            if (IsExternalDevelopment)
            {
                testPart = "T00"; // Αν η ανάπτυξη γίνεται εκτός Beauty lab
            }
            else if (TestNumber.HasValue)
            {
                testPart = $"T{TestNumber.Value:D2}"; // Αριθμός τεστ με δύο ψηφία (T01, T02, κλπ)
            }
            else
            {
                testPart = "T00"; // Default value
            }
            
            string baseCode = $"{customerCode}-{abcdCode}-{testPart}";
            
            // Προσθήκη ? αν η φόρμουλα είναι WIP
            if (IsWIP)
            {
                baseCode += "?";
            }
            
             return baseCode;
         }
         
         /// <summary>
         /// Υπολογίζει το συνολικό ποσοστό όλων των α' υλών στη φόρμουλα
         /// </summary>
         /// <returns>Το συνολικό ποσοστό των α' υλών (χωρίς νερό)</returns>
         public decimal CalculateTotalRawMaterialPercentage()
         {
             return FormulaRawMaterials.Where(frm => frm.IsActive && frm.RawMaterial != null && frm.RawMaterial.IsActive)
                                     .Sum(frm => frm.Percentage ?? 0);
         }
         
         /// <summary>
         /// Ελέγχει αν το άθροισμα των ποσοστών των α' υλών είναι έγκυρο (≤ 100%)
         /// </summary>
         /// <returns>True αν το άθροισμα είναι ≤ 100%, False αλλιώς</returns>
         public bool IsRawMaterialPercentageValid()
         {
             return CalculateTotalRawMaterialPercentage() <= 100;
         }
         
         /// <summary>
         /// Υπολογίζει το ποσοστό νερού που χρειάζεται για να φτάσει το συνολικό στο 100%
         /// </summary>
         /// <returns>Το ποσοστό νερού που χρειάζεται (може да е отрицателно ако το άθροισμα > 100)</returns>
         public decimal CalculateRequiredWaterPercentage()
         {
             return 100 - CalculateTotalRawMaterialPercentage();
         }
         
         /// <summary>
         /// Ελέγχει αν η φόρμουλα μπορεί να είναι ανύδρη (0% νερό)
         /// </summary>
         /// <returns>True αν το άθροισμα των α' υλών είναι ακριβώς 100%</returns>
         public bool CanBeAnhydrous()
         {
             return CalculateTotalRawMaterialPercentage() == 100;
         }
         
         /// <summary>
         /// Επιστρέφει μια περιγραφή της κατάστασης του ποσοστού νερού
         /// </summary>
         /// <returns>Περιγραφή του ποσοστού νερού</returns>
         public string GetWaterPercentageStatus()
         {
             decimal totalRawMaterial = CalculateTotalRawMaterialPercentage();
             decimal waterNeeded = CalculateRequiredWaterPercentage();
             
             if (totalRawMaterial > 100)
             {
                 return $"Υπερβαίνει το 100% ({totalRawMaterial:F2}%). Χρειάζεται {-waterNeeded:F2}% μειωσης.";
             }
             else if (totalRawMaterial == 100)
             {
                 return "Τέλειο άθροισμα 100% (ανύδρο φόρμουλα)";
             }
             else
             {
                 return $"Χρειάζεται {waterNeeded:F2}% νερό για να φτάσει στο 100%";
             }
         }
     }
}
