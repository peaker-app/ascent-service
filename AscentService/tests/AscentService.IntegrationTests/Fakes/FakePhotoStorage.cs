using System.Collections.Concurrent;
using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Domain.Results;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class FakePhotoStorage : IPhotoStorage
{
    private int _uploadCount;

    public ConcurrentBag<string> DeletedPublicIds { get; } = [];

    public bool IsDown { get; set; }

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
        DeletedPublicIds.Add(publicId);

        return Task.CompletedTask;
    }
}
