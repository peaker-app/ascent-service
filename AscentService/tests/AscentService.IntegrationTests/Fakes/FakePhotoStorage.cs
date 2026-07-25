using System.Collections.Concurrent;
using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using AscentService.Infrastructure.ExternalServices;
using Common.Domain.Results;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class FakePhotoStorage : IPhotoStorage
{
    private readonly ConcurrentDictionary<string, int> _deletionFailures = new(StringComparer.Ordinal);

    private int _uploadCount;

    public ConcurrentBag<string> DeletedPublicIds { get; } = [];

    public bool IsDown { get; set; }

    public void FailNextDeletions(string publicIdSuffix, int attempts) =>
        _deletionFailures[publicIdSuffix] = attempts;

    public Task<Result<StoredPhoto>> UploadAsync(PhotoFile file, CancellationToken cancellationToken)
    {
        if (IsDown)
        {
            return Task.FromResult(Result.Failure<StoredPhoto>(AscentErrors.PhotoUploadFailed));
        }

        int index = Interlocked.Increment(ref _uploadCount);
        StoredPhoto stored = new(
            $"peaker/test/ascents/photo-{index}", $"https://res.cloudinary.test/photo-{index}.jpg", 1600, 1200);

        return Task.FromResult(Result.Success(stored));
    }

    public Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        if (ShouldFail(publicId))
        {
            throw new PhotoStorageException($"Simulated deletion failure for '{publicId}'.");
        }

        DeletedPublicIds.Add(publicId);

        return Task.CompletedTask;
    }

    private bool ShouldFail(string publicId)
    {
        foreach (KeyValuePair<string, int> failure in _deletionFailures)
        {
            if (failure.Value > 0 && publicId.EndsWith(failure.Key, StringComparison.Ordinal))
            {
                _deletionFailures[failure.Key] = failure.Value - 1;

                return true;
            }
        }

        return false;
    }
}
