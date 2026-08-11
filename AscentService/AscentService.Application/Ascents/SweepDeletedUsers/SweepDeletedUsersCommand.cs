using Common.Application.Messaging;

namespace AscentService.Application.Ascents.SweepDeletedUsers;

public sealed record SweepDeletedUsersCommand(int MaxUsers) : ICommand<DeletedUserSweepResponse>;
