namespace AscentService.Domain.Ascents;

public sealed record AscentConditions(SnowCondition? Snow, WindCondition? Wind, TrailCondition? Trail)
{
    public static readonly AscentConditions Unreported = new(null, null, null);
}
