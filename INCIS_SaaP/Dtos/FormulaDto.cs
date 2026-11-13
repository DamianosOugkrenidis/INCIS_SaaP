// Author: Damianos Pythagoras Ougkrenidis
//
using System;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Dtos
{
    /// <summary>
    /// Data Transfer Object for Formula entity, used for grouping operations.
    /// </summary>
    public class FormulaDto
    {
        public int Id { get; set; }
        public string FormulaCode { get; set; } = string.Empty;
        public string? FormulaName { get; set; }
        public string? ProductType { get; set; }
        public string? Version { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? LastModified { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// Static method to create a FormulaDto from a Formula entity
        /// </summary>
        /// <param name="formula">The Formula entity to convert</param>
        /// <returns>A FormulaDto object representing the Formula entity</returns>
        public static FormulaDto FromEntity(Formula formula)
        {
            if (formula == null) return null!;

            return new FormulaDto
            {
                Id = formula.Id,
                FormulaCode = formula.FormulaCode,
                FormulaName = formula.FormulaName,
                ProductType = formula.ProductType,
                Version = formula.Version,
                CreationDate = formula.CreationDate,
                LastModified = formula.LastModified,
                Description = formula.Description
            };
        }
    }
}