using FluentValidation;

namespace TecChallenge.Domain.Entities.Validations;

public class GameValidation : AbstractValidator<Game>
{
    public GameValidation()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("The {PropertyName} field must be supplied")
            .Length(2, 100)
            .WithMessage(
                "The {PropertyName} field needs to have between {MinLength} and {MaxLength} characters"
            );

        RuleFor(c => c.Price)
            .NotEmpty()
            .WithMessage("The {PropertyName} field needs to be supplied");
    }
}