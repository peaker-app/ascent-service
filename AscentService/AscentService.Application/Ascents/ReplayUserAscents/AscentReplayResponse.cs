namespace AscentService.Application.Ascents.ReplayUserAscents;

public sealed record AscentReplayResponse(Guid UserId, int AscentsRepublished);
