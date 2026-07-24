using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Domain.Results;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class FakeProfileDirectory : IProfileDirectory
{
    private readonly HashSet<Guid> _privateProfiles = [];

    public bool IsDown { get; set; }

    public void MakePrivate(Guid userId) => _privateProfiles.Add(userId);

    public void Reset()
    {
        _privateProfiles.Clear();
        IsDown = false;
    }

    public Task<Result<bool>> IsProfilePublicAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(IsDown
            ? Result.Failure<bool>(AscentErrors.ProfileDirectoryUnavailable)
            : Result.Success(!_privateProfiles.Contains(userId)));
}
