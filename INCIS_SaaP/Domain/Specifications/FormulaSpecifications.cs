// Author: Damianos Pythagoras Ougkrenidis
// Domain Specifications - Reusable business rules (Specification Pattern)

using System.Linq.Expressions;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Domain.Specifications;

/// <summary>
/// Base specification interface for composable business rules
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
    ISpecification<T> And(ISpecification<T> other);
    ISpecification<T> Or(ISpecification<T> other);
    ISpecification<T> Not();
}

/// <summary>
/// Base specification implementation
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    public ISpecification<T> And(ISpecification<T> other)
        => new AndSpecification<T>(this, other);

    public ISpecification<T> Or(ISpecification<T> other)
        => new OrSpecification<T>(this, other);

    public ISpecification<T> Not()
        => new NotSpecification<T>(this);
}

// Combinators
internal class AndSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

internal class OrSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

internal class NotSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _spec;

    public NotSpecification(ISpecification<T> spec)
    {
        _spec = spec;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = _spec.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var body = Expression.Not(Expression.Invoke(expr, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

// ============================================================
// FORMULA SPECIFICATIONS
// ============================================================

/// <summary>
/// Formula is ready for validation by RA Head:
/// - Not WIP (stability &amp; challenge tests passed)
/// - Has raw materials
/// - Total percentage is valid (≤ 100%)
/// - Not already validated
/// </summary>
public class FormulaReadyForValidationSpec : Specification<Formula>
{
    public override Expression<Func<Formula, bool>> ToExpression()
    {
        return f => !f.IsWIP
                    && f.FormulaRawMaterials.Any()
                    && !f.ValidationDate.HasValue;
    }
}

/// <summary>
/// Formula is validated and ready for production
/// </summary>
public class FormulaValidatedSpec : Specification<Formula>
{
    public override Expression<Func<Formula, bool>> ToExpression()
    {
        return f => f.ValidationDate.HasValue && !f.IsWIP;
    }
}

/// <summary>
/// Formula is Work In Progress (WIP)
/// </summary>
public class FormulaIsWIPSpec : Specification<Formula>
{
    public override Expression<Func<Formula, bool>> ToExpression()
    {
        return f => f.IsWIP;
    }
}

/// <summary>
/// Formula has valid percentage sum (≤ 100%)
/// </summary>
public class FormulaPercentageValidSpec : Specification<Formula>
{
    public override Expression<Func<Formula, bool>> ToExpression()
    {
        return f => f.FormulaRawMaterials
            .Where(frm => frm.IsActive)
            .Sum(frm => frm.Percentage ?? 0) <= 100;
    }
}

/// <summary>
/// Formula belongs to a specific client
/// </summary>
public class FormulaBelongsToClientSpec : Specification<Formula>
{
    private readonly int _clientId;

    public FormulaBelongsToClientSpec(int clientId)
    {
        _clientId = clientId;
    }

    public override Expression<Func<Formula, bool>> ToExpression()
    {
        return f => f.ClientId == _clientId;
    }
}

/// <summary>
/// Formula is anhydrous (no water needed)
/// </summary>
public class FormulaIsAnhydrousSpec : Specification<Formula>
{
    public override Expression<Func<Formula, bool>> ToExpression()
    {
        return f => f.FormulaRawMaterials
            .Where(frm => frm.IsActive)
            .Sum(frm => frm.Percentage ?? 0) == 100;
    }
}

// ============================================================
// RAW MATERIAL SPECIFICATIONS
// ============================================================

/// <summary>
/// Raw material is fully registered with all required documents
/// </summary>
public class RawMaterialCompleteSpec : Specification<RawMaterial>
{
    public override Expression<Func<RawMaterial, bool>> ToExpression()
    {
        return rm => rm.IsFullyRegistered
                     && rm.IsEvaluated
                     && rm.HasTDS
                     && rm.HasMSDS
                     && rm.Composition.Any();
    }
}

/// <summary>
/// Raw material has expired samples in the lab
/// </summary>
public class RawMaterialHasExpiredSamplesSpec : Specification<RawMaterial>
{
    public override Expression<Func<RawMaterial, bool>> ToExpression()
    {
        var now = DateTime.Now;
        return rm => rm.Samples.Any(s => s.ExpiryDate < now && s.IsActive);
    }
}

/// <summary>
/// Raw material is used in at least one formula
/// </summary>
public class RawMaterialIsUsedSpec : Specification<RawMaterial>
{
    public override Expression<Func<RawMaterial, bool>> ToExpression()
    {
        return rm => rm.FormulaRawMaterials.Any(frm => frm.IsActive);
    }
}

/// <summary>
/// Raw material is missing required documents (shows ! indicator)
/// </summary>
public class RawMaterialIncompleteSpec : Specification<RawMaterial>
{
    public override Expression<Func<RawMaterial, bool>> ToExpression()
    {
        return rm => !rm.IsFullyRegistered || !rm.IsEvaluated;
    }
}

/// <summary>
/// Raw material contains a specific INCI substance
/// </summary>
public class RawMaterialContainsINCISpec : Specification<RawMaterial>
{
    private readonly int _inciId;

    public RawMaterialContainsINCISpec(int inciId)
    {
        _inciId = inciId;
    }

    public override Expression<Func<RawMaterial, bool>> ToExpression()
    {
        return rm => rm.Composition.Any(c => c.INCIId == _inciId && !c.IsImpurity);
    }
}
