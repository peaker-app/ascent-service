using FluentValidation;

namespace AscentService.Application.Ascents.SweepDeletedUsers;

internal sealed class SweepDeletedUsersCommandValidator : AbstractValidator<SweepDeletedUsersCommand>
{
    public SweepDeletedUsersCommandValidator() =>
        RuleFor(command => command.MaxUsers).GreaterThan(0);
}
