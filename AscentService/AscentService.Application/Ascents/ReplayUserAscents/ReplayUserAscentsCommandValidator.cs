using FluentValidation;

namespace AscentService.Application.Ascents.ReplayUserAscents;

internal sealed class ReplayUserAscentsCommandValidator : AbstractValidator<ReplayUserAscentsCommand>
{
    public ReplayUserAscentsCommandValidator() =>
        RuleFor(command => command.UserId).NotEmpty();
}
