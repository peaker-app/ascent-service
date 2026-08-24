namespace AscentService.Application.Ascents.SweepOrphanedPhotos;

public sealed record PhotoSweepResponse(int QuarantinedRemoved, int UnreferencedRemoved);
