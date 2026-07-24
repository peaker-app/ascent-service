using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Application.Ascents.ListUserAscents;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using Common.Application.Pagination;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class ListUserAscentsQueryHandlerTests
{
    private static readonly PageRequest FirstPage = new(1, 20);

    private readonly IAscentReader _ascentReader = Substitute.For<IAscentReader>();
    private readonly IProfileDirectory _profileDirectory = Substitute.For<IProfileDirectory>();

    private readonly ListUserAscentsQueryHandler _handler;

    public ListUserAscentsQueryHandlerTests()
    {
        _ascentReader.ListPublicByUserAsync(Arg.Any<Guid>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AscentSummaryResponse>([], 1, 20, 0));

        _handler = new ListUserAscentsQueryHandler(_ascentReader, _profileDirectory);
    }

    [Fact]
    public async Task Handle_WhenTheProfileIsPublic_ReturnsOnlyThePublicAscents()
    {
        GivenTheProfileIs(isPublic: true);

        await _handler.Handle(QueryBy(AscentFactory.OtherUserId), CancellationToken.None);

        await _ascentReader.Received(1).ListPublicByUserAsync(
            AscentFactory.OwnerId, FirstPage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheProfileIsPrivateAndTheRequesterIsAnotherUser_ReturnsProfileNotVisible()
    {
        GivenTheProfileIs(isPublic: false);

        Result<PagedResult<AscentSummaryResponse>> result =
            await _handler.Handle(QueryBy(AscentFactory.OtherUserId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.ProfileNotVisible(AscentFactory.OwnerId));
    }

    [Fact]
    public async Task Handle_WhenTheProfileIsPrivateAndTheRequesterIsAnonymous_ReturnsProfileNotVisible()
    {
        GivenTheProfileIs(isPublic: false);

        Result<PagedResult<AscentSummaryResponse>> result =
            await _handler.Handle(QueryBy(null), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTheProfileIsPrivateButTheRequesterIsTheOwner_StillReturnsTheirPublicAscents()
    {
        GivenTheProfileIs(isPublic: false);

        Result<PagedResult<AscentSummaryResponse>> result =
            await _handler.Handle(QueryBy(AscentFactory.OwnerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTheRequesterIsTheOwner_NeverAsksTheProfileDirectory()
    {
        await _handler.Handle(QueryBy(AscentFactory.OwnerId), CancellationToken.None);

        await _profileDirectory.DidNotReceive()
            .IsProfilePublicAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAPageSizeOverTheLimit_ReturnsSizeOutOfRange()
    {
        ListUserAscentsQuery query = new(AscentFactory.OwnerId, AscentFactory.OwnerId, new PageRequest(1, 101));

        Result<PagedResult<AscentSummaryResponse>> result = await _handler.Handle(query, CancellationToken.None);

        result.Error.Should().Be(PaginationErrors.SizeOutOfRange);
    }

    [Fact]
    public async Task Handle_WhenTheProfileDirectoryIsDown_ReturnsProfileDirectoryUnavailable()
    {
        _profileDirectory.IsProfilePublicAsync(AscentFactory.OwnerId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(AscentErrors.ProfileDirectoryUnavailable));

        Result<PagedResult<AscentSummaryResponse>> result =
            await _handler.Handle(QueryBy(null), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    private static ListUserAscentsQuery QueryBy(Guid? requesterId) =>
        new(AscentFactory.OwnerId, requesterId, FirstPage);

    private void GivenTheProfileIs(bool isPublic) =>
        _profileDirectory.IsProfilePublicAsync(AscentFactory.OwnerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(isPublic));
}
