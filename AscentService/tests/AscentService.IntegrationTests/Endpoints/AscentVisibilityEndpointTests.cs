using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Domain.Ascents;
using Common.API.Responses;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class AscentVisibilityEndpointTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task GetById_WhenTheOwnerReadsTheirOwnPrivateAscent_Returns200()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ownerId);
        Guid ascentId = await owner.RegisterAscentAsync(Body(AscentVisibility.Private));

        HttpResponseMessage response = await owner.GetAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_WhenAnAnonymousVisitorReadsAPrivateAscent_Returns404()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(Body(AscentVisibility.Private));

        using HttpClient visitor = factory.CreateClient();
        HttpResponseMessage response = await visitor.GetAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenAnotherUserReadsAPrivateAscent_Returns404()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(Body(AscentVisibility.Private));

        using HttpClient stranger = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.GetAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenAnAnonymousVisitorReadsAPublicAscentOfAPublicProfile_Returns200()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(Body(AscentVisibility.Public));

        using HttpClient visitor = factory.CreateClient();
        HttpResponseMessage response = await visitor.GetAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_WhenAnAnonymousVisitorReadsAPublicAscentOfAPrivateProfile_Returns404()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ownerId);
        Guid ascentId = await owner.RegisterAscentAsync(Body(AscentVisibility.Public));

        factory.ProfileDirectory.MakePrivate(ownerId);

        using HttpClient visitor = factory.CreateClient();
        HttpResponseMessage response = await visitor.GetAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithAnUnknownAscent_Returns404()
    {
        using HttpClient visitor = factory.CreateClient();

        HttpResponseMessage response = await visitor.GetAsync(ApiTestHelpers.AscentRoute(Guid.CreateVersion7()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListByUser_ForAnAnonymousVisitor_ReturnsOnlyThePublicAscents()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ownerId);
        await owner.RegisterAscentAsync(Body(AscentVisibility.Public));
        await owner.RegisterAscentAsync(Body(AscentVisibility.Private));

        using HttpClient visitor = factory.CreateClient();
        PagedResponse<AscentSummaryResponse>? page = await visitor
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>(ApiTestHelpers.ByUserRoute(ownerId));

        page!.Items.Should().OnlyContain(item => item.Visibility == nameof(AscentVisibility.Public));
    }

    [Fact]
    public async Task ListByUser_ForTheOwner_StillReturnsOnlyTheirPublicAscents()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ownerId);
        await owner.RegisterAscentAsync(Body(AscentVisibility.Public));
        await owner.RegisterAscentAsync(Body(AscentVisibility.Private));

        PagedResponse<AscentSummaryResponse>? page = await owner
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>(ApiTestHelpers.ByUserRoute(ownerId));

        page!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListByUser_WhenTheProfileIsPrivateAndTheRequesterIsAStranger_Returns404()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ownerId);
        await owner.RegisterAscentAsync(Body(AscentVisibility.Public));

        factory.ProfileDirectory.MakePrivate(ownerId);

        using HttpClient stranger = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListByUser_WhenTheProfileIsPrivateButTheRequesterIsTheOwner_Returns200()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ownerId);
        await owner.RegisterAscentAsync(Body(AscentVisibility.Public));

        factory.ProfileDirectory.MakePrivate(ownerId);

        HttpResponseMessage response = await owner.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListByUser_ForAUserWithoutPublicAscents_Returns200WithAnEmptyList()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();

        using HttpClient visitor = factory.CreateClient();
        PagedResponse<AscentSummaryResponse>? page = await visitor
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>(ApiTestHelpers.ByUserRoute(ownerId));

        page!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListByUser_WhenTheProfileDirectoryIsDown_Returns404()
    {
        Guid ownerId = ApiTestHelpers.NewUserId();
        using HttpClient visitor = factory.CreateClient();

        factory.ProfileDirectory.IsDown = true;

        try
        {
            HttpResponseMessage response = await visitor.GetAsync(ApiTestHelpers.ByUserRoute(ownerId));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            factory.ProfileDirectory.IsDown = false;
        }
    }

    private object Body(AscentVisibility visibility) => new
    {
        peakId = factory.PeakCatalog.Register().PeakId,
        ascentDate = "2026-07-01",
        visibility = visibility.ToString()
    };
}
