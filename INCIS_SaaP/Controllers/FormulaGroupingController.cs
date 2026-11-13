using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Data;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormulaGroupingController : ControllerBase
    {
        private readonly INCISDbContext _context;
        private readonly IFormulaGroupingService _groupingService;

        public FormulaGroupingController(INCISDbContext context, IFormulaGroupingService groupingService)
        {
            _context = context;
            _groupingService = groupingService;
        }

        /// <summary>
        /// Get all formulas grouped by formula type (using ProductType as a substitute for FormulaType).
        /// </summary>
        /// <returns>A list of groups, each with a formula type and the formulas in that group.</returns>
        [HttpGet("group-by-formula-type")]
        public ActionResult<IEnumerable<FormulaGroupResultDto<string>>> GetGroupedByFormulaType()
        {
            var formulas = _context.Formulas
                .Select(f => new FormulaDto
                {
                    Id = f.Id,
                    FormulaCode = f.FormulaCode,
                    FormulaName = f.FormulaName,
                    ProductType = f.ProductType,
                    Version = f.Version,
                    CreationDate = f.CreationDate,
                    LastModified = f.LastModified,
                    Description = f.Description
                })
                .ToList();

            var grouped = _groupingService.GroupByFormulaType(formulas);
            return Ok(grouped);
        }

        /// <summary>
        /// Get all formulas grouped by project identifier (using Id as a placeholder for ProjectId).
        /// </summary>
        /// <returns>A list of groups, each with a project id and the formulas in that group.</returns>
        [HttpGet("group-by-project-id")]
        public ActionResult<IEnumerable<FormulaGroupResultDto<long?>>> GetGroupedByProjectId()
        {
            var formulas = _context.Formulas
                .Select(f => new FormulaDto
                {
                    Id = f.Id,
                    FormulaCode = f.FormulaCode,
                    FormulaName = f.FormulaName,
                    ProductType = f.ProductType,
                    Version = f.Version,
                    CreationDate = f.CreationDate,
                    LastModified = f.LastModified,
                    Description = f.Description
                })
                .ToList();

            var grouped = _groupingService.GroupByProjectId(formulas);
            return Ok(grouped);
        }

        /// <summary>
        /// Get all formulas grouped by creation date (day, month, or year).
        /// </summary>
        /// <param name="unit">The date unit to group by (Day, Month, Year).</param>
        /// <returns>A list of groups, each with a date key and the formulas in that group.</returns>
        [HttpGet("group-by-created-date/{unit}")]
        public ActionResult<IEnumerable<FormulaGroupResultDto<System.DateTime>>> GetGroupedByCreatedDate(DateTimeUnit unit)
        {
            var formulas = _context.Formulas
                .Select(f => new FormulaDto
                {
                    Id = f.Id,
                    FormulaCode = f.FormulaCode,
                    FormulaName = f.FormulaName,
                    ProductType = f.ProductType,
                    Version = f.Version,
                    CreationDate = f.CreationDate,
                    LastModified = f.LastModified,
                    Description = f.Description
                })
                .ToList();

            var grouped = _groupingService.GroupByCreatedDate(formulas, unit);
            return Ok(grouped);
        }

        /// <summary>
        /// Get all formulas grouped by a custom key selector (example: grouping by the first letter of FormulaCode).
        /// </summary>
        /// <returns>A list of groups, each with a key and the formulas in that group.</returns>
        [HttpGet("group-by-custom")]
        public ActionResult<IEnumerable<FormulaGroupResultDto<string>>> GetGroupedByCustom()
        {
            var formulas = _context.Formulas
                .Select(f => new FormulaDto
                {
                    Id = f.Id,
                    FormulaCode = f.FormulaCode,
                    FormulaName = f.FormulaName,
                    ProductType = f.ProductType,
                    Version = f.Version,
                    CreationDate = f.CreationDate,
                    LastModified = f.LastModified,
                    Description = f.Description
                })
                .ToList();

            // Example custom key selector: group by the first letter of the FormulaCode (or 'Unknown' if empty)
            var grouped = _groupingService.GroupBy(formulas, f => string.IsNullOrEmpty(f.FormulaCode) ? "Unknown" : f.FormulaCode[0].ToString().ToUpper());
            return Ok(grouped);
        }
    }
}