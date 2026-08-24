using AscentService.Domain.Ascents;
using FluentValidation;

namespace AscentService.Application.Ascents.SyncPeakName;

internal sealed class SyncPeakNameCommandValidator : AbstractValidator<SyncPeakNameCommand>
{
    public SyncPeakNameCommandValidator()
    {
        RuleFor(command => command.PeakId).NotEmpty();
        RuleFor(command => command.PeakName).NotEmpty().MaximumLength(PeakSnapshot.MaxNameLength);
    }
}
