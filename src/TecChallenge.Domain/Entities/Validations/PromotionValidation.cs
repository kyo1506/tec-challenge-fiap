using FluentValidation;

namespace TecChallenge.Domain.Entities.Validations;

public class PromotionValidation : AbstractValidator<Promotion>
{
    public PromotionValidation()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("The {PropertyName} field needs to be supplied")
            .Length(2, 100)
            .WithMessage(
                "The {PropertyName} field needs to have between {MinLength} and {MaxLength} characters"
            );

        RuleFor(c => c.StartDate)
            .NotEmpty()
            .WithMessage("The {PropertyName} field needs to be supplied")
            .LessThanOrEqualTo(c => c.EndDate)
            .WithMessage("The start date must be less than or equal to the end date");

        RuleFor(c => c.EndDate)
            .NotEmpty()
            .WithMessage("The {PropertyName} field needs to be supplied")
            .GreaterThan(DateTime.Now)
            .WithMessage("Promotions that are closed cannot be edited");
    }
}