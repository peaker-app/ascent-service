using Common.Application.Messaging;

namespace AscentService.Application.ConfirmedUsers.ConfirmUser;

public sealed record ConfirmUserCommand(Guid UserId, DateTime ConfirmedAtUtc) : ICommand;
