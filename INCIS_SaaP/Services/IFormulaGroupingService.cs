using System.Collections.Generic;
using INCIS_SaaP.Dtos;

namespace INCIS_SaaP.Services
{
    /// <summary>
    /// Service for grouping formulas by various patterns (formula type, project, date, etc.).
    /// </summary>
    public interface IFormulaGroupingService
    {
        /// <summary>
        /// Groups formulas by their formula type (e.g., Master, INCI, Safety).
        /// </summary>
        /// <param name="formulas">The formulas to group.</param>
        /// <returns>A collection of groups, each with a formula type key and the formulas in that group.</returns>
        IEnumerable<FormulaGroupResultDto<string>> GroupByFormulaType(IEnumerable<FormulaDto> formulas);

        /// <summary>
        /// Groups formulas by their project identifier.
        /// </summary>
        /// <param name="formulas">The formulas to group.</param>
        /// <returns>A collection of groups, each with a project id key and the formulas in that group.</returns>
        IEnumerable<FormulaGroupResultDto<long?>> GroupByProjectId(IEnumerable<FormulaDto> formulas);

        /// <summary>
        /// Groups formulas by their creation date (day, month, or year).
        /// </summary>
        /// <param name="formulas">The formulas to group.</param>
        /// <param name="unit">The date unit to group by (Day, Month, Year).</param>
        /// <returns>A collection of groups, each with a date key and the formulas in that group.</returns>
        IEnumerable<FormulaGroupResultDto<System.DateTime>> GroupByCreatedDate(IEnumerable<FormulaDto> formulas, DateTimeUnit unit);

        /// <summary>
        /// Groups formulas by a custom key selector.
        /// </summary>
        /// <typeparam name="TKey">The type of the group key.</typeparam>
        /// <param name="formulas">The formulas to group.</param>
        /// <param name="keySelector">A function to extract the group key from each formula.</param>
        /// <returns>A collection of groups, each with a key of type TKey and the formulas in that group.</returns>
        IEnumerable<FormulaGroupResultDto<TKey>> GroupBy<TKey>(IEnumerable<FormulaDto> formulas, System.Func<FormulaDto, TKey> keySelector);
    }

    /// <summary>
    /// Defines the unit for date grouping.
    /// </summary>
    public enum DateTimeUnit
    {
        Day,
        Month,
        Year
    }
}