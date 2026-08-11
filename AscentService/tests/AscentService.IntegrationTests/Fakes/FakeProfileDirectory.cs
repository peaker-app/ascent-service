using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Domain.Results;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class FakeProfileDirectory : IProfileDirectory
{
    private readonly HashSet<Guid> _privateProfiles = [];

    private int _lookups;

    public bool IsDown { get; set; }

    public int Lookups => _lookups;

    public void MakePrivate(Guid userId) => _privateProfiles.Add(userId);

    public void ResetLookups() => Interlocked.Exchange(ref _lookups, 0);

    public void Reset()
    {
        _privateProfiles.Clear();
        IsDown = false;
        ResetLookups();
    }

    public Task<Result<bool>> IsProfilePublicAsync(Guid userId, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _lookups);

        return Task.FromResult(IsDown
            ? Result.Failure<bool>(AscentErrors.ProfileDirectoryUnavailable)
            : Result.Success(!_privateProfiles.Contains(userId)));
    }
}
