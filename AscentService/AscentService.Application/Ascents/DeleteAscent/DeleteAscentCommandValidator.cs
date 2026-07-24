using FluentValidation;

namespace AscentService.Application.Ascents.DeleteAscent;

internal sealed class DeleteAscentCommandValidator : AbstractValidator<DeleteAscentCommand>
{
    public DeleteAscentCommandValidator()
    {
        RuleFor(command => command.AscentId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}
