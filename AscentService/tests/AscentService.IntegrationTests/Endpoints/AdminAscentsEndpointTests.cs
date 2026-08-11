using System.Globalization;
using System.Net;
using AscentService.Application.Ascents.ReplayUserAscents;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class AdminAscentsEndpointTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task Replay_WithoutAToken_Returns401()
    {
        using HttpClient anonymous = factory.CreateClient();

        HttpResponseMessage response = await anonymous.PostAsync(ReplayRoute(Guid.CreateVersion7()), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Replay_WithoutTheAdminRole_Returns403()
    {
        using HttpClient hiker = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await hiker.PostAsync(ReplayRoute(Guid.CreateVersion7()), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Replay_AsAdmin_ReportsEveryAscentOfTheUser()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);
        await owner.RegisterAscentAsync(NewAscentBody());
        await owner.RegisterAscentAsync(NewAscentBody());

        using HttpClient admin = factory.CreateAdminClient(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await admin.PostAsync(ReplayRoute(userId), null);

        AscentReplayResponse? replay = await response.Content.ReadFromJsonAsync<AscentReplayResponse>();

        replay!.AscentsRepublished.Should().Be(2);
    }

    [Fact]
    public async Task Replay_AsAdmin_ForAUserWithoutAscents_ReportsNothing()
    {
        using HttpClient admin = factory.CreateAdminClient(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await admin.PostAsync(ReplayRoute(ApiTestHelpers.NewUserId()), null);

        AscentReplayResponse? replay = await response.Content.ReadFromJsonAsync<AscentReplayResponse>();

        replay!.AscentsRepublished.Should().Be(0);
    }

    private static string ReplayRoute(Guid userId) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/admin/ascents/replay/{userId}");

    private object NewAscentBody() => new
    {
        peakId = factory.PeakCatalog.Register("Aneto", 3404).PeakId,
        ascentDate = "2026-07-01"
    };
}
