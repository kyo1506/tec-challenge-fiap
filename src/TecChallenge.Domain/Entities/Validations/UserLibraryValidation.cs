using FluentValidation;

namespace TecChallenge.Domain.Entities.Validations;

public class UserLibraryValidation : AbstractValidator<UserLibrary>
{
    public UserLibraryValidation()
    {
        RuleFor(c => c.UserId)
            .NotEmpty()
            .WithMessage("O campo {PropertyName} precisa ser fornecido");
    }
}