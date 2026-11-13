using System.Collections.Generic;

namespace INCIS_SaaP.Dtos
{
    /// <summary>
    /// Represents a group of formulas with a common key.
    /// </summary>
    /// <typeparam name="TKey">The type of the group key.</typeparam>
    public class FormulaGroupResultDto<TKey>
    {
        /// <summary>
        /// The key of the group (e.g., formula type, project id, date).
        /// </summary>
        public TKey Key { get; set; } = default!;

        /// <summary>
        /// The formulas in this group.
        /// </summary>
        public IEnumerable<FormulaDto> Items { get; set; } = Enumerable.Empty<FormulaDto>();

        /// <summary>
        /// The number of formulas in this group.
        /// </summary>
        public int Count => Items?.Count() ?? 0;
    }
}