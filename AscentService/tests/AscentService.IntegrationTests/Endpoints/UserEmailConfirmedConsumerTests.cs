using System.Net;
using System.Net.Http.Json;
using AscentService.Domain.Ascents;
using Common.Contracts.Users;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class UserEmailConfirmedConsumerTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task RegisterAscent_WithAnUnconfirmedEmail_ReturnsForbidden()
    {
        using HttpClient hiker = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());

        using HttpResponseMessage response = await hiker.PostAsJsonAsync("/api/ascents", NewAscentBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterAscent_OnceTheEmailIsConfirmed_ReturnsCreated()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient hiker = factory.CreateAuthenticatedClient(userId);

        await factory.PublishUserEmailConfirmedAsync(NewMessage(userId));
        bool projected = await factory.WaitForUserConfirmationAsync(userId);

        projected.Should().BeTrue();
        using HttpResponseMessage response = await hiker.PostAsJsonAsync("/api/ascents", NewAscentBody());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Consume_SameMessageTwice_KeepsTheUserConfirmedOnce()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        UserEmailConfirmed message = NewMessage(userId);

        await factory.PublishUserEmailConfirmedAsync(message);
        await factory.WaitForUserConfirmationAsync(userId);
        await factory.PublishUserEmailConfirmedAsync(message);
        await Task.Delay(TimeSpan.FromSeconds(2));

        (await factory.IsUserConfirmedAsync(userId)).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_UserDeleted_DropsTheConfirmationProjection()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        await factory.ConfirmUserAsync(userId);

        await factory.PublishUserDeletedAsync(new UserDeleted { UserId = userId, OccurredAtUtc = DateTime.UtcNow });
        await Task.Delay(TimeSpan.FromSeconds(2));

        (await factory.IsUserConfirmedAsync(userId)).Should().BeFalse();
    }

    private object NewAscentBody()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register("Aneto", 3404);

        return new { peakId = peak.PeakId, ascentDate = "2026-07-01" };
    }

    private static UserEmailConfirmed NewMessage(Guid userId) => new()
    {
        UserId = userId,
        OccurredAtUtc = DateTime.UtcNow
    };
}
