using FluentValidation;

namespace AscentService.Application.ConfirmedUsers.ConfirmUser;

internal sealed class ConfirmUserCommandValidator : AbstractValidator<ConfirmUserCommand>
{
    public ConfirmUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.ConfirmedAtUtc).NotEmpty();
    }
}
