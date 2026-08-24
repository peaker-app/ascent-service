using Common.Application.Messaging;

namespace AscentService.Application.Ascents.DeleteAscent;

public sealed record DeleteAscentCommand(Guid AscentId, Guid UserId) : ICommand;
