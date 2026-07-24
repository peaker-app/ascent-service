namespace AscentService.Domain.Ascents;

public sealed record AscentDraft(Guid UserId, PeakSnapshot Peak, AscentDetails Details);
