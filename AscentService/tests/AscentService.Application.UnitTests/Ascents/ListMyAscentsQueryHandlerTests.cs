using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Application.UnitTests.TestData;
using Common.Application.Pagination;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class ListMyAscentsQueryHandlerTests
{
    private readonly IAscentReader _ascentReader = Substitute.For<IAscentReader>();

    private readonly ListMyAscentsQueryHandler _handler;

    public ListMyAscentsQueryHandlerTests()
    {
        _ascentReader.ListByUserAsync(Arg.Any<Guid>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AscentSummaryRow>([], 1, 20, 0));

        _handler = new ListMyAscentsQueryHandler(_ascentReader, AscentFactory.PhotoUrlSigner());
    }

    [Fact]
    public async Task Handle_WithAValidPage_ReadsEveryAscentOfTheAuthenticatedUser()
    {
        PageRequest page = new(1, 20);

        await _handler.Handle(new ListMyAscentsQuery(AscentFactory.OwnerId, page), CancellationToken.None);

        await _ascentReader.Received(1).ListByUserAsync(
            AscentFactory.OwnerId, page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAPageSizeOverTheLimit_ReturnsSizeOutOfRange()
    {
        ListMyAscentsQuery query = new(AscentFactory.OwnerId, new PageRequest(1, 101));

        Result<PagedResult<AscentSummaryResponse>> result = await _handler.Handle(query, CancellationToken.None);

        result.Error.Should().Be(PaginationErrors.SizeOutOfRange);
    }

    [Fact]
    public async Task Handle_WithAPageBelowOne_ReturnsPageOutOfRange()
    {
        ListMyAscentsQuery query = new(AscentFactory.OwnerId, new PageRequest(0, 20));

        Result<PagedResult<AscentSummaryResponse>> result = await _handler.Handle(query, CancellationToken.None);

        result.Error.Should().Be(PaginationErrors.PageOutOfRange);
    }
}
