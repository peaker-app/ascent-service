using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Domain.Ascents;
using Common.API.Responses;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class ListMyAscentsEndpointTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task ListMine_ReturnsTheAscentsInDescendingDateOrder()
    {
        using HttpClient client = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        await client.RegisterAscentAsync(Body("2024-05-10"));
        await client.RegisterAscentAsync(Body("2026-01-20"));
        await client.RegisterAscentAsync(Body("2025-08-03"));

        PagedResponse<AscentSummaryResponse>? page = await client
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>("/api/ascents");

        page!.Items.Select(item => item.AscentDate).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListMine_ReturnsBothPublicAndPrivateAscents()
    {
        using HttpClient client = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        await client.RegisterAscentAsync(Body(visibility: AscentVisibility.Public));
        await client.RegisterAscentAsync(Body(visibility: AscentVisibility.Private));

        PagedResponse<AscentSummaryResponse>? page = await client
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>("/api/ascents");

        page!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListMine_WhenTheProfileIsPrivate_StillReturnsEverythingOfTheOwner()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient client = factory.CreateAuthenticatedClient(ownerId);
        await client.RegisterAscentAsync(Body(visibility: AscentVisibility.Public));
        await client.RegisterAscentAsync(Body(visibility: AscentVisibility.Private));

        factory.ProfileDirectory.MakePrivate(ownerId);

        PagedResponse<AscentSummaryResponse>? page = await client
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>("/api/ascents");

        page!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListMine_NeverReturnsAnotherUserAscents()
    {
        using HttpClient stranger = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        await stranger.RegisterAscentAsync(Body());

        using HttpClient client = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        PagedResponse<AscentSummaryResponse>? page = await client
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>("/api/ascents");

        page!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListMine_WithAPageSizeOverTheLimit_Returns400()
    {
        using HttpClient client = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await client.GetAsync("/api/ascents?page=1&size=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListMine_WithAnExplicitPage_HonoursTheRequestedSize()
    {
        using HttpClient client = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        await client.RegisterAscentAsync(Body("2024-05-10"));
        await client.RegisterAscentAsync(Body("2025-05-10"));
        await client.RegisterAscentAsync(Body("2026-05-10"));

        PagedResponse<AscentSummaryResponse>? page = await client
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>("/api/ascents?page=2&size=2");

        page!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task ListMine_WithoutAToken_Returns401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/ascents");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private object Body(
        string ascentDate = "2026-07-01",
        AscentVisibility visibility = AscentVisibility.Public) => new
    {
        peakId = factory.PeakCatalog.Register().PeakId,
        ascentDate,
        visibility = visibility.ToString()
    };
}
