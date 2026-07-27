using AscentService.Domain.Ascents;
using Common.Contracts.Users;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class UserDeletedConsumerTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task Consume_UserDeleted_RemovesEveryAscentOfThatUser()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);
        await owner.RegisterAscentAsync(NewAscentBody());
        await owner.RegisterAscentAsync(NewAscentBody());

        await factory.PublishUserDeletedAsync(NewMessage(userId));

        bool removed = await factory.WaitForAscentRemovalAsync(userId);
        removed.Should().BeTrue();
    }

    [Fact]
    public async Task Consume_UserDeleted_LeavesOtherHikersAscentsUntouched()
    {
        Guid deletedUser = ApiTestHelpers.NewUserId();
        Guid survivor = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(deletedUser);
        using HttpClient other = await factory.CreateConfirmedClientAsync(survivor);
        await owner.RegisterAscentAsync(NewAscentBody());
        await other.RegisterAscentAsync(NewAscentBody());

        await factory.PublishUserDeletedAsync(NewMessage(deletedUser));
        await factory.WaitForAscentRemovalAsync(deletedUser);

        int survivors = await factory.CountAscentsAsync(survivor);
        survivors.Should().Be(1);
    }

    [Fact]
    public async Task Consume_UserDeleted_DeletesTheAttachedPhotosFromStorage()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);
        Guid ascentId = await owner.RegisterAscentAsync(NewAscentBody());
        await owner.AddPhotoAsync(ascentId);
        IReadOnlyList<string> publicIds = await factory.ReadPhotoPublicIdsAsync(ascentId);

        await factory.PublishUserDeletedAsync(NewMessage(userId));
        await factory.WaitForAscentRemovalAsync(userId);
        await Task.Delay(TimeSpan.FromSeconds(2));

        factory.PhotoStorage.DeletedPublicIds.Should().Contain(publicIds);
    }

    [Fact]
    public async Task Consume_SameMessageTwice_LeavesTheAscentsRemoved()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);
        await owner.RegisterAscentAsync(NewAscentBody());
        UserDeleted message = NewMessage(userId);

        await factory.PublishUserDeletedAsync(message);
        await factory.WaitForAscentRemovalAsync(userId);
        await factory.PublishUserDeletedAsync(message);
        await Task.Delay(TimeSpan.FromSeconds(2));

        int remaining = await factory.CountAscentsAsync(userId);
        remaining.Should().Be(0);
    }

    private object NewAscentBody()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register("Aneto", 3404);

        return new { peakId = peak.PeakId, ascentDate = "2026-07-01" };
    }

    private static UserDeleted NewMessage(Guid userId) => new()
    {
        MessageId = Guid.CreateVersion7(),
        UserId = userId,
        OccurredAtUtc = DateTime.UtcNow
    };
}
