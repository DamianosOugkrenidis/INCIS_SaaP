using System;
using System.Collections.Generic;
using System.Linq;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services
{
    /// <summary>
    /// Service for grouping formulas by various patterns (formula type, project, date, etc.).
    /// </summary>
    public class FormulaGroupingService : IFormulaGroupingService
    {
        /// <summary>
        /// Groups formulas by their formula type (e.g., Master, INCI, Safety).
        /// Note: Since the Formula entity does not have a FormulaType property, 
        /// we are using the ProductType as a substitute for demonstration.
        /// In a real scenario, you might have a separate property or a lookup.
        /// </summary>
        /// <param name="formulas">The formulas to group.</param>
        /// <returns>A collection of groups, each with a formula type key and the formulas in that group.</returns>
        public IEnumerable<FormulaGroupResultDto<string>> GroupByFormulaType(IEnumerable<FormulaDto> formulas)
        {
            return GroupBy(formulas, f => f.ProductType ?? "Unknown");
        }

        /// <summary>
        /// Groups formulas by their project identifier.
        /// Note: The Formula entity does not have a ProjectId property. 
        /// For demonstration, we are using the Id as a placeholder.
        /// In a real scenario, you would have a ProjectId foreign key.
        /// </summary>
        /// <param name="formulas">The formulas to group.</param>
        /// <returns>A collection of groups, each with a project id key and the formulas in that group.</returns>
        public IEnumerable<FormulaGroupResultDto<long?>> GroupByProjectId(IEnumerable<FormulaDto> formulas)
        {
            return GroupBy(formulas, f => (long?)f.Id);
        }

        /// <summary>
        /// Groups formulas by their creation date (day, month, or year).
        /// </summary>
        /// <param name="formulas">The formulas to group.</param>
        /// <param name="unit">The date unit to group by (Day, Month, Year).</param>
        /// <returns>A collection of groups, each with a date key and the formulas in that group.</returns>
        public IEnumerable<FormulaGroupResultDto<DateTime>> GroupByCreatedDate(IEnumerable<FormulaDto> formulas, DateTimeUnit unit)
        {
            Func<FormulaDto, DateTime> selector = unit switch
            {
                DateTimeUnit.Day => f => f.CreationDate.GetValueOrDefault(DateTime.MinValue).Date,
                DateTimeUnit.Month => f => new DateTime(f.CreationDate.GetValueOrDefault(DateTime.MinValue).Year, f.CreationDate.GetValueOrDefault(DateTime.MinValue).Month, 1),
                DateTimeUnit.Year => f => new DateTime(f.CreationDate.GetValueOrDefault(DateTime.MinValue).Year, 1, 1),
                _ => f => f.CreationDate.GetValueOrDefault(DateTime.MinValue)
            };

            return GroupBy(formulas, selector);
        }

        /// <summary>
        /// Groups formulas by a custom key selector.
        /// </summary>
        /// <typeparam name="TKey">The type of the group key.</typeparam>
        /// <param name="formulas">The formulas to group.</param>
        /// <param name="keySelector">A function to extract the group key from each formula.</param>
        /// <returns>A collection of groups, each with a key of type TKey and the formulas in that group.</returns>
        public IEnumerable<FormulaGroupResultDto<TKey>> GroupBy<TKey>(IEnumerable<FormulaDto> formulas, Func<FormulaDto, TKey> keySelector)
        {
            if (formulas == null) throw new ArgumentNullException(nameof(formulas));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return formulas
                .GroupBy(keySelector)
                .Select(g => new FormulaGroupResultDto<TKey>
                {
                    Key = g.Key,
                    Items = g // g is already IEnumerable<FormulaDto>
                });
        }
    }
}