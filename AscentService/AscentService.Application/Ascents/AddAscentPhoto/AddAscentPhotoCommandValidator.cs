using FluentValidation;

namespace AscentService.Application.Ascents.AddAscentPhoto;

internal sealed class AddAscentPhotoCommandValidator : AbstractValidator<AddAscentPhotoCommand>
{
    public AddAscentPhotoCommandValidator()
    {
        RuleFor(command => command.AscentId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.File).NotNull();

        When(command => command.File is not null, () =>
        {
            RuleFor(command => command.File.Content.Length).GreaterThan(0);
            RuleFor(command => command.File.FileName).NotEmpty();
        });
    }
}
