using Common.Application.Messaging;

namespace AscentService.Application.Ascents.ReplayUserAscents;

public sealed record ReplayUserAscentsCommand(Guid UserId) : ICommand<AscentReplayResponse>;
