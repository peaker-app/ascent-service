using FluentValidation;

namespace AscentService.Application.Ascents.RemoveAscentPhoto;

internal sealed class RemoveAscentPhotoCommandValidator : AbstractValidator<RemoveAscentPhotoCommand>
{
    public RemoveAscentPhotoCommandValidator()
    {
        RuleFor(command => command.AscentId).NotEmpty();
        RuleFor(command => command.PhotoId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}
