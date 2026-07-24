namespace AscentService.Domain.Ascents;

public sealed record PeakSnapshot(Guid PeakId, string Name, int AltitudeMeters)
{
    public const int MaxNameLength = 200;
}
