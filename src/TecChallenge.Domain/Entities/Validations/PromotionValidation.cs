using FluentValidation;

namespace TecChallenge.Domain.Entities.Validations;

public class PromotionValidation : AbstractValidator<Promotion>
{
    public PromotionValidation()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("O campo {PropertyName} precisa ser fornecido")
            .Length(2, 100)
            .WithMessage(
                "O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres"
            );

        RuleFor(c => c.StartDate)
            .NotEmpty()
            .WithMessage("O campo {PropertyName} precisa ser fornecido")
            .GreaterThanOrEqualTo(c => c.EndDate)
            .WithMessage("A data inicial deve ser menor ou igual a data final");

        RuleFor(c => c.EndDate)
            .NotEmpty()
            .WithMessage("O campo {PropertyName} precisa ser fornecido");

        RuleFor(c => c.GamesOnSale)
            .NotEmpty()
            .WithMessage("O campo {PropertyName} precisa ser fornecido")
            .Must(g => g.Count >= 1)
            .WithMessage("A lista deve conter pelo menos um item");
    }
}