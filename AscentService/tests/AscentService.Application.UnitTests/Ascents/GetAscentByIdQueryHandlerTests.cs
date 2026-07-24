using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class GetAscentByIdQueryHandlerTests
{
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();
    private readonly IProfileDirectory _profileDirectory = Substitute.For<IProfileDirectory>();

    private readonly GetAscentByIdQueryHandler _handler;

    public GetAscentByIdQueryHandlerTests() =>
        _handler = new GetAscentByIdQueryHandler(_ascentRepository, _profileDirectory);

    [Fact]
    public async Task Handle_WhenTheOwnerReadsTheirOwnPrivateAscent_ReturnsIt()
    {
        Ascent ascent = GivenAnExistingAscent(AscentVisibility.Private);

        Result<AscentResponse> result = await _handler.Handle(
            new GetAscentByIdQuery(ascent.Id, AscentFactory.OwnerId), CancellationToken.None);

        result.Value.Id.Should().Be(ascent.Id);
    }

    [Fact]
    public async Task Handle_WhenTheOwnerReadsTheirOwnAscent_NeverAsksTheProfileDirectory()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(new GetAscentByIdQuery(ascent.Id, AscentFactory.OwnerId), CancellationToken.None);

        await _profileDirectory.DidNotReceive()
            .IsProfilePublicAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnknownAscent_ReturnsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();
        _ascentRepository.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((Ascent?)null);

        Result<AscentResponse> result = await _handler.Handle(
            new GetAscentByIdQuery(unknownId, AscentFactory.OwnerId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(unknownId));
    }

    [Fact]
    public async Task Handle_WhenAnAnonymousVisitorReadsAPrivateAscent_ReturnsNotFound()
    {
        Ascent ascent = GivenAnExistingAscent(AscentVisibility.Private);

        Result<AscentResponse> result = await _handler.Handle(
            new GetAscentByIdQuery(ascent.Id, null), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(ascent.Id));
    }

    [Fact]
    public async Task Handle_WhenAnAnonymousVisitorReadsAPublicAscentOfAPublicProfile_ReturnsIt()
    {
        Ascent ascent = GivenAnExistingAscent();
        GivenTheOwnerProfileIs(isPublic: true);

        Result<AscentResponse> result = await _handler.Handle(
            new GetAscentByIdQuery(ascent.Id, null), CancellationToken.None);

        result.Value.Id.Should().Be(ascent.Id);
    }

    [Fact]
    public async Task Handle_WhenAnAnonymousVisitorReadsAPublicAscentOfAPrivateProfile_ReturnsNotFound()
    {
        Ascent ascent = GivenAnExistingAscent();
        GivenTheOwnerProfileIs(isPublic: false);

        Result<AscentResponse> result = await _handler.Handle(
            new GetAscentByIdQuery(ascent.Id, null), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(ascent.Id));
    }

    [Fact]
    public async Task Handle_WhenAnotherUserReadsAPublicAscentOfAPublicProfile_ReturnsIt()
    {
        Ascent ascent = GivenAnExistingAscent();
        GivenTheOwnerProfileIs(isPublic: true);

        Result<AscentResponse> result = await _handler.Handle(
            new GetAscentByIdQuery(ascent.Id, AscentFactory.OtherUserId), CancellationToken.None);

        result.Value.UserId.Should().Be(AscentFactory.OwnerId);
    }

    [Fact]
    public async Task Handle_WhenTheProfileDirectoryIsDown_ReturnsProfileDirectoryUnavailable()
    {
        Ascent ascent = GivenAnExistingAscent();
        _profileDirectory.IsProfilePublicAsync(AscentFactory.OwnerId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(AscentErrors.ProfileDirectoryUnavailable));

        Result<AscentResponse> result = await _handler.Handle(
            new GetAscentByIdQuery(ascent.Id, null), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    private Ascent GivenAnExistingAscent(AscentVisibility visibility = AscentVisibility.Public)
    {
        Ascent ascent = AscentFactory.Registered(visibility);
        _ascentRepository.GetByIdAsync(ascent.Id, Arg.Any<CancellationToken>()).Returns(ascent);

        return ascent;
    }

    private void GivenTheOwnerProfileIs(bool isPublic) =>
        _profileDirectory.IsProfilePublicAsync(AscentFactory.OwnerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(isPublic));
}
