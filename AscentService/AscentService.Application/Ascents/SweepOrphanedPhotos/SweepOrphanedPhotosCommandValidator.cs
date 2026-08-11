using FluentValidation;

namespace AscentService.Application.Ascents.SweepOrphanedPhotos;

internal sealed class SweepOrphanedPhotosCommandValidator : AbstractValidator<SweepOrphanedPhotosCommand>
{
    public SweepOrphanedPhotosCommandValidator() =>
        RuleFor(command => command.UploadedBeforeUtc).NotEmpty();
}
