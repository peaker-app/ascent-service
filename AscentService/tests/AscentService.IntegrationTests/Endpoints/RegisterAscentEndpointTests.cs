using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Domain.Ascents;
using Common.API.Responses;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class RegisterAscentEndpointTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task Register_WithAKnownPeak_Returns201()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/ascents", Body(peak.PeakId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithAKnownPeak_DenormalisesTheCatalogData()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register("Mont Blanc", 4808);
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        Guid ascentId = await client.RegisterAscentAsync(Body(peak.PeakId));

        AscentResponse? ascent = await client.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent.Should().BeEquivalentTo(new { PeakName = "Mont Blanc", PeakAltitudeMeters = 4808 });
    }

    [Fact]
    public async Task Register_WithoutVisibility_PersistsThePublicDefault()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        Guid ascentId = await client.RegisterAscentAsync(new { peakId = peak.PeakId, ascentDate = "2026-07-01" });

        AscentResponse? ascent = await client.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent!.Visibility.Should().Be(nameof(AscentVisibility.Public));
    }

    [Fact]
    public async Task Register_WithoutVisibility_MakesTheAscentReachableByAnAnonymousVisitor()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);

        Guid ascentId = await owner.RegisterAscentAsync(new { peakId = peak.PeakId, ascentDate = "2026-07-01" });

        using HttpClient visitor = factory.CreateClient();
        HttpResponseMessage response = await visitor.GetAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithConditions_PersistsThem()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        Guid ascentId = await client.RegisterAscentAsync(new
        {
            peakId = peak.PeakId,
            ascentDate = "2026-07-01",
            snow = nameof(SnowCondition.Deep),
            wind = nameof(WindCondition.Storm),
            trail = nameof(TrailCondition.Icy)
        });

        AscentResponse? ascent = await client.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent!.Conditions.Should().BeEquivalentTo(
            new AscentConditionsResponse(nameof(SnowCondition.Deep), nameof(WindCondition.Storm), nameof(TrailCondition.Icy)));
    }

    [Fact]
    public async Task Register_WithAFutureDate_Returns400()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/ascents", Body(peak.PeakId, ascentDate: "2099-01-01"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithADateBefore1900_Returns400()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/ascents", Body(peak.PeakId, ascentDate: "1899-12-31"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithoutAToken_Returns401()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/ascents", Body(peak.PeakId));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithAnUnknownPeak_Returns404()
    {
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/ascents", Body(Guid.CreateVersion7()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_WhenThePeakCatalogIsDown_Returns503()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        factory.PeakCatalog.IsDown = true;

        try
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/ascents", Body(peak.PeakId));

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            factory.PeakCatalog.IsDown = false;
        }
    }

    [Fact]
    public async Task Register_TheSamePeakTwice_IsAllowed()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        await client.RegisterAscentAsync(Body(peak.PeakId));
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/ascents", Body(peak.PeakId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_TwiceWithTheSameClientAscentId_ReturnsTheSameId()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid clientAscentId = Guid.CreateVersion7();

        Guid first = await client.RegisterAscentAsync(Body(peak.PeakId, clientAscentId: clientAscentId));
        Guid second = await client.RegisterAscentAsync(Body(peak.PeakId, clientAscentId: clientAscentId));

        second.Should().Be(first);
    }

    [Fact]
    public async Task Register_TwiceWithTheSameClientAscentId_CreatesASingleAscent()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid clientAscentId = Guid.CreateVersion7();

        await client.RegisterAscentAsync(Body(peak.PeakId, clientAscentId: clientAscentId));
        await client.RegisterAscentAsync(Body(peak.PeakId, clientAscentId: clientAscentId));

        PagedResponse<AscentSummaryResponse>? page = await client
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>("/api/ascents");

        page!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Register_WithAClientAscentIdUsedByAnotherUser_CreatesItsOwnAscent()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        Guid clientAscentId = Guid.CreateVersion7();

        using HttpClient first = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        using HttpClient second = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        Guid mine = await first.RegisterAscentAsync(Body(peak.PeakId, clientAscentId: clientAscentId));
        Guid theirs = await second.RegisterAscentAsync(Body(peak.PeakId, clientAscentId: clientAscentId));

        theirs.Should().NotBe(mine);
    }

    [Fact]
    public async Task Register_TwiceWithoutAClientAscentId_CreatesTwoAscents()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register();
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        await client.RegisterAscentAsync(Body(peak.PeakId));
        await client.RegisterAscentAsync(Body(peak.PeakId));

        PagedResponse<AscentSummaryResponse>? page = await client
            .GetFromJsonAsync<PagedResponse<AscentSummaryResponse>>("/api/ascents");

        page!.TotalCount.Should().Be(2);
    }

    private static object Body(
        Guid peakId,
        string ascentDate = "2026-07-01",
        Guid? clientAscentId = null) => new
    {
        peakId,
        ascentDate,
        companions = "Marta y Julio",
        routeNotes = "Vía normal por el glaciar",
        visibility = nameof(AscentVisibility.Public),
        clientAscentId
    };
}
