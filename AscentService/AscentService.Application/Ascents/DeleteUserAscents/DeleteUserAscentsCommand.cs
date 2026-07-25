using Common.Application.Messaging;

namespace AscentService.Application.Ascents.DeleteUserAscents;

public sealed record DeleteUserAscentsCommand(Guid UserId) : ICommand;
