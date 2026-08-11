namespace AscentService.Infrastructure.Maintenance;

public sealed class DeletedUserSweepOptions
{
    public const string SectionName = "DeletedUserSweep";

    public bool Enabled { get; init; } = true;

    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    public int BatchSize { get; init; } = 100;
}
