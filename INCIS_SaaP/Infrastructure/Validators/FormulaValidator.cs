using FluentValidation;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Infrastructure.Validators;

public class FormulaValidator : AbstractValidator<Formula>
{
    public FormulaValidator()
    {
        RuleFor(x => x.FormulaCode)
            .NotEmpty().WithMessage("Ο κωδικός φόρμουλας είναι υποχρεωτικός.")
            .MaximumLength(100).WithMessage("Ο κωδικός δεν μπορεί να υπερβαίνει τους 100 χαρακτήρες.");

        RuleFor(x => x.ProductName)
            .MaximumLength(200).WithMessage("Το όνομα προϊόντος δεν μπορεί να υπερβαίνει τους 200 χαρακτήρες.");

        RuleFor(x => x.ProductType)
            .Must(x => x == null || x == "Leave on" || x == "Rinse off")
            .WithMessage("Ο τύπος προϊόντος πρέπει να είναι 'Leave on' ή 'Rinse off'.");

        RuleFor(x => x.TestNumber)
            .GreaterThanOrEqualTo(0).When(x => x.TestNumber.HasValue)
            .WithMessage("Ο αριθμός τεστ πρέπει να είναι θετικός.");
    }
}

public class ClientValidator : AbstractValidator<Client>
{
    public ClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Το όνομα πελάτη είναι υποχρεωτικό.")
            .MaximumLength(100).WithMessage("Το όνομα δεν μπορεί να υπερβαίνει τους 100 χαρακτήρες.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Μη έγκυρη μορφή email.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.Phone));
    }
}

public class SupplierValidator : AbstractValidator<Supplier>
{
    public SupplierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Το όνομα προμηθευτή είναι υποχρεωτικό.")
            .MaximumLength(100).WithMessage("Το όνομα δεν μπορεί να υπερβαίνει τους 100 χαρακτήρες.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Μη έγκυρη μορφή email.");
    }
}

public class RawMaterialValidator : AbstractValidator<RawMaterial>
{
    public RawMaterialValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Ο κωδικός α' ύλης είναι υποχρεωτικός.")
            .MaximumLength(100);

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Το όνομα προϊόντος είναι υποχρεωτικό.")
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).When(x => x.Price.HasValue)
            .WithMessage("Η τιμή δεν μπορεί να είναι αρνητική.");

        RuleFor(x => x.NaturalOriginPercentage)
            .InclusiveBetween(0, 100).When(x => x.NaturalOriginPercentage.HasValue)
            .WithMessage("Το ποσοστό φυσικής προέλευσης πρέπει να είναι μεταξύ 0 και 100.");
    }
}

public class INCIValidator : AbstractValidator<INCI>
{
    public INCIValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Το όνομα INCI είναι υποχρεωτικό.")
            .MaximumLength(200);

        RuleFor(x => x.CASNumber)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.CASNumber));

        RuleFor(x => x.ECNumber)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.ECNumber));
    }
}

public class ProductionBatchValidator : AbstractValidator<ProductionBatch>
{
    public ProductionBatchValidator()
    {
        RuleFor(x => x.BatchSize)
            .GreaterThan(0).WithMessage("Το μέγεθος παρτίδας πρέπει να είναι μεγαλύτερο από 0.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Η μονάδα μέτρησης είναι υποχρεωτική.")
            .Must(u => new[] { "kg", "g", "L", "ml" }.Contains(u))
            .WithMessage("Η μονάδα πρέπει να είναι kg, g, L ή ml.");

        RuleFor(x => x.FormulaId)
            .GreaterThan(0).WithMessage("Η φόρμουλα είναι υποχρεωτική.");
    }
}
