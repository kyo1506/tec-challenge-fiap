using FluentValidation;
using FluentValidation.Results;

namespace TecChallenge.Domain.Services;

public abstract class BaseService(INotifier notifier)
{
    private void Notify(ValidationResult validationResult)
    {
        foreach (var error in validationResult.Errors) Notify(error.ErrorMessage);
    }

    protected void Notify(string message)
    {
        notifier.Handle(new Notification(message));
    }

    protected bool ExecuteValidation<TV, TE>(TV validation, TE entity)
        where TV : AbstractValidator<TE>
        where TE : Entity
    {
        var validator = validation.Validate(entity);

        if (validator.IsValid)
            return true;

        Notify(validator);

        return false;
    }
}