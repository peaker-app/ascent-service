using AscentService.Domain.Ascents;
using FluentValidation;

namespace AscentService.Application.Ascents.RegisterAscent;

internal sealed class RegisterAscentCommandValidator : AbstractValidator<RegisterAscentCommand>
{
    public RegisterAscentCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.PeakId).NotEmpty();
        RuleFor(command => command.Companions).MaximumLength(Ascent.MaxCompanionsLength);
        RuleFor(command => command.RouteNotes).MaximumLength(Ascent.MaxRouteNotesLength);
        RuleFor(command => command.Visibility).IsInEnum();
        RuleFor(command => command.Conditions).NotNull();

        When(command => command.Conditions is not null, () =>
        {
            RuleFor(command => command.Conditions.Snow).IsInEnum();
            RuleFor(command => command.Conditions.Wind).IsInEnum();
            RuleFor(command => command.Conditions.Trail).IsInEnum();
        });
    }
}
