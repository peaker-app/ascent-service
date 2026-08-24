namespace AscentService.Infrastructure.Maintenance;

public sealed class PhotoSweepOptions
{
    public const string SectionName = "PhotoSweep";

    public bool Enabled { get; init; } = true;

    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan Retention { get; init; } = TimeSpan.FromHours(24);
}
