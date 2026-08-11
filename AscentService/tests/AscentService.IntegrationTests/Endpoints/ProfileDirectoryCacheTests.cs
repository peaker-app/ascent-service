using Common.Contracts.Profiles;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class ProfileDirectoryCacheTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task ListByUser_TwiceForTheSameOwner_AsksTheDirectoryOnlyOnce()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient visitor = factory.CreateClient();
        factory.ProfileDirectory.ResetLookups();

        await visitor.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));
        await visitor.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));

        factory.ProfileDirectory.Lookups.Should().Be(1);
    }

    [Fact]
    public async Task ListByUser_AfterTheProfileChanges_AsksTheDirectoryAgain()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient visitor = factory.CreateClient();

        await visitor.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));
        await factory.PublishProfileUpdatedAsync(NewMessage(ownerId));

        bool refreshed = await WaitForLookupAfterEvictionAsync(visitor, ownerId);

        refreshed.Should().BeTrue();
    }

    [Fact]
    public async Task ListByUser_AfterTheSameEventTwice_StaysConsistent()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient visitor = factory.CreateClient();
        ProfileUpdated message = NewMessage(ownerId);

        await factory.PublishProfileUpdatedAsync(message);
        await factory.PublishProfileUpdatedAsync(message);
        await visitor.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));

        HttpResponseMessage response = await visitor.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    private async Task<bool> WaitForLookupAfterEvictionAsync(HttpClient visitor, Guid ownerId)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            factory.ProfileDirectory.ResetLookups();
            await visitor.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));

            if (factory.ProfileDirectory.Lookups > 0)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    private static ProfileUpdated NewMessage(Guid userId) => new()
    {
        MessageId = Guid.CreateVersion7(),
        OccurredAtUtc = DateTime.UtcNow,
        ProfileId = Guid.CreateVersion7(),
        UserId = userId,
        DisplayName = "Montañero",
        Slug = "montanero",
        Visibility = "Public"
    };
}
