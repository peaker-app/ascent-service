using AscentService.Domain.ConfirmedUsers;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.ConfirmedUsers.ConfirmUser;

internal sealed class ConfirmUserCommandHandler(
    IConfirmedUserRepository confirmedUserRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmUserCommand>
{
    public async Task<Result> Handle(ConfirmUserCommand command, CancellationToken cancellationToken)
    {
        ConfirmedUser? existing = await confirmedUserRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (existing is not null)
        {
            return Result.Success();
        }

        confirmedUserRepository.Add(ConfirmedUser.Project(command.UserId, command.ConfirmedAtUtc));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
