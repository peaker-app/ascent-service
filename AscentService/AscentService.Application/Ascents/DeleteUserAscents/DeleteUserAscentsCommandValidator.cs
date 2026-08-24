using FluentValidation;

namespace AscentService.Application.Ascents.DeleteUserAscents;

internal sealed class DeleteUserAscentsCommandValidator : AbstractValidator<DeleteUserAscentsCommand>
{
    public DeleteUserAscentsCommandValidator() => RuleFor(command => command.UserId).NotEmpty();
}
