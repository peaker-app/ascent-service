using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Ascents.SweepDeletedUsers;
using AscentService.Domain.Ascents;
using Common.Contracts.Users;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class DeletedAccountTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task Consume_UserDeleted_LeavesATombstone()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);
        await owner.RegisterAscentAsync(NewAscentBody());

        await factory.PublishUserDeletedAsync(NewMessage(userId));

        bool tombstoned = await factory.WaitForUserTombstoneAsync(userId);
        tombstoned.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithTheStillValidTokenOfADeletedAccount_Returns403()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);
        await owner.RegisterAscentAsync(NewAscentBody());

        await factory.PublishUserDeletedAsync(NewMessage(userId));
        await factory.WaitForUserTombstoneAsync(userId);

        HttpResponseMessage response = await owner.PostAsJsonAsync("/api/ascents", NewAscentBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_WithTheStillValidTokenOfADeletedAccount_LeavesNoAscentBehind()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);
        await owner.RegisterAscentAsync(NewAscentBody());

        await factory.PublishUserDeletedAsync(NewMessage(userId));
        await factory.WaitForUserTombstoneAsync(userId);
        await owner.PostAsJsonAsync("/api/ascents", NewAscentBody());

        int remaining = await factory.CountAscentsAsync(userId);
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task Sweep_AfterAnAscentSlippedThrough_ReportsItAsRemoved()
    {
        Guid userId = await GivenADeletedUserWithALeftoverAscentAsync();

        DeletedUserSweepResponse sweep = await factory.SweepDeletedUsersAsync();

        sweep.AscentsRemoved.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Sweep_AfterAnAscentSlippedThrough_LeavesTheUserWithNothing()
    {
        Guid userId = await GivenADeletedUserWithALeftoverAscentAsync();

        await factory.SweepDeletedUsersAsync();

        int remaining = await factory.CountAscentsAsync(userId);
        remaining.Should().Be(0);
    }

    private async Task<Guid> GivenADeletedUserWithALeftoverAscentAsync()
    {
        Guid userId = ApiTestHelpers.NewUserId();
        using HttpClient owner = await factory.CreateConfirmedClientAsync(userId);

        await factory.PublishUserDeletedAsync(NewMessage(userId));
        await factory.WaitForUserTombstoneAsync(userId);
        await factory.InsertAscentDirectlyAsync(userId);

        return userId;
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
