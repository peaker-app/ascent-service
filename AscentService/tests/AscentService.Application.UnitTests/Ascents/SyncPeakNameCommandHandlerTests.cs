using AscentService.Application.Ascents.SyncPeakName;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class SyncPeakNameCommandHandlerTests
{
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SyncPeakNameCommandHandler _handler;

    public SyncPeakNameCommandHandlerTests() =>
        _handler = new SyncPeakNameCommandHandler(_ascentRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WithARenamedPeak_UpdatesEveryAffectedAscent()
    {
        IReadOnlyList<Ascent> ascents = GivenAscentsOnAneto(3);

        await _handler.Handle(NewNameCommand(), CancellationToken.None);

        ascents.Should().OnlyContain(ascent => ascent.Peak.Name == "Pico de Aneto");
    }

    [Fact]
    public async Task Handle_WithARenamedPeak_AlsoRefreshesTheDenormalisedAltitude()
    {
        IReadOnlyList<Ascent> ascents = GivenAscentsOnAneto(1);

        await _handler.Handle(NewNameCommand(), CancellationToken.None);

        ascents.Single().Peak.AltitudeMeters.Should().Be(3405);
    }

    [Fact]
    public async Task Handle_WithARenamedPeak_CommitsOnce()
    {
        GivenAscentsOnAneto(2);

        await _handler.Handle(NewNameCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoAscentReferencesThePeak_CommitsWithoutTouchingAnything()
    {
        _ascentRepository.GetByPeakIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _handler.Handle(NewNameCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTheSamePeakData_LeavesTheSnapshotUntouched()
    {
        IReadOnlyList<Ascent> ascents = GivenAscentsOnAneto(1);
        PeakSnapshot original = ascents.Single().Peak;

        await _handler.Handle(
            new SyncPeakNameCommand(AscentFactory.Aneto.PeakId, AscentFactory.Aneto.Name, 3404),
            CancellationToken.None);

        ascents.Single().Peak.Should().BeSameAs(original);
    }

    private static SyncPeakNameCommand NewNameCommand() =>
        new(AscentFactory.Aneto.PeakId, "Pico de Aneto", 3405);

    private List<Ascent> GivenAscentsOnAneto(int count)
    {
        List<Ascent> ascents = [.. Enumerable.Range(0, count).Select(_ => AscentFactory.Registered())];

        _ascentRepository.GetByPeakIdAsync(AscentFactory.Aneto.PeakId, Arg.Any<CancellationToken>())
            .Returns(ascents);

        return ascents;
    }
}
