using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.RegisterAscent;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class RegisterAscentCommandHandlerTests
{
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();
    private readonly IPeakCatalog _peakCatalog = Substitute.For<IPeakCatalog>();

    private readonly IConfirmedUserDirectory _confirmedUserDirectory =
        Substitute.For<IConfirmedUserDirectory>();

    private readonly IDeletedUserDirectory _deletedUserDirectory =
        Substitute.For<IDeletedUserDirectory>();

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly RegisterAscentCommandHandler _handler;

    public RegisterAscentCommandHandlerTests()
    {
        _dateTimeProvider.Today.Returns(AscentFactory.Today);
        _confirmedUserDirectory.IsConfirmedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _deletedUserDirectory.IsDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _handler = new RegisterAscentCommandHandler(
            _ascentRepository,
            _peakCatalog,
            _confirmedUserDirectory,
            _deletedUserDirectory,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithADeletedAccount_ReturnsAccountDeleted()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);
        _deletedUserDirectory.IsDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        Result<Guid> result = await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.AccountDeleted);
    }

    [Fact]
    public async Task Handle_WithADeletedAccount_DoesNotPersistAnything()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);
        _deletedUserDirectory.IsDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnconfirmedEmail_ReturnsEmailNotConfirmed()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);
        _confirmedUserDirectory.IsConfirmedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        Result<Guid> result = await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.EmailNotConfirmed);
    }

    [Fact]
    public async Task Handle_WithAnUnconfirmedEmail_DoesNotPersistAnything()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);
        _confirmedUserDirectory.IsConfirmedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAKnownPeak_ReturnsTheNewAscentId()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);

        Result<Guid> result = await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithAKnownPeak_PersistsTheAscent()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);

        await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAKnownPeak_DenormalisesTheCatalogSnapshot()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);

        await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        _ascentRepository.Received(1).Add(Arg.Is<Ascent>(ascent => ascent!.Peak == AscentFactory.Aneto));
    }

    [Fact]
    public async Task Handle_WithAnUnknownPeak_ReturnsPeakNotFound()
    {
        Error notFound = AscentErrors.PeakNotFound(AscentFactory.Aneto.PeakId);
        _peakCatalog.GetSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PeakSnapshot>(notFound));

        Result<Guid> result = await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        result.Error.Should().Be(notFound);
    }

    [Fact]
    public async Task Handle_WhenTheCatalogIsDown_ReturnsPeakCatalogUnavailable()
    {
        _peakCatalog.GetSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PeakSnapshot>(AscentErrors.PeakCatalogUnavailable));

        Result<Guid> result = await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public async Task Handle_WhenTheCatalogIsDown_DoesNotPersistAnything()
    {
        _peakCatalog.GetSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PeakSnapshot>(AscentErrors.PeakCatalogUnavailable));

        await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAFutureDate_ReturnsDateInFuture()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);
        RegisterAscentCommand command = AscentFactory.RegisterCommand(AscentFactory.Today.AddDays(1));

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.Error.Should().Be(AscentErrors.DateInFuture);
    }

    [Fact]
    public async Task Handle_WithADateBefore1900_ReturnsDateTooOld()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);
        RegisterAscentCommand command = AscentFactory.RegisterCommand(new DateOnly(1899, 12, 31));

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.Error.Should().Be(AscentErrors.DateTooOld);
    }

    [Fact]
    public async Task Handle_WithAClientAscentId_StampsItOnTheAscent()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);
        Guid clientAscentId = Guid.CreateVersion7();

        await _handler.Handle(
            AscentFactory.RegisterCommand(clientAscentId: clientAscentId), CancellationToken.None);

        _ascentRepository.Received(1)
            .Add(Arg.Is<Ascent>(ascent => ascent!.ClientAscentId == clientAscentId));
    }

    [Fact]
    public async Task Handle_WithAClientAscentIdAlreadyRegistered_ReturnsTheExistingId()
    {
        Guid clientAscentId = Guid.CreateVersion7();
        Ascent existing = GivenTheKeyWasAlreadyUsed(clientAscentId);

        Result<Guid> result = await _handler.Handle(
            AscentFactory.RegisterCommand(clientAscentId: clientAscentId), CancellationToken.None);

        result.Value.Should().Be(existing.Id);
    }

    [Fact]
    public async Task Handle_WithAClientAscentIdAlreadyRegistered_DoesNotRegisterAnotherAscent()
    {
        Guid clientAscentId = Guid.CreateVersion7();
        GivenTheKeyWasAlreadyUsed(clientAscentId);

        await _handler.Handle(
            AscentFactory.RegisterCommand(clientAscentId: clientAscentId), CancellationToken.None);

        _ascentRepository.DidNotReceive().Add(Arg.Any<Ascent>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAClientAscentIdAlreadyRegistered_DoesNotAskTheCatalogAgain()
    {
        Guid clientAscentId = Guid.CreateVersion7();
        GivenTheKeyWasAlreadyUsed(clientAscentId);

        await _handler.Handle(
            AscentFactory.RegisterCommand(clientAscentId: clientAscentId), CancellationToken.None);

        await _peakCatalog.DidNotReceive()
            .GetSnapshotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutAClientAscentId_NeverLooksForADuplicate()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);

        await _handler.Handle(AscentFactory.RegisterCommand(), CancellationToken.None);

        await _ascentRepository.DidNotReceive().GetByClientAscentIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnEmptyClientAscentId_RegistersWithoutDeduplicating()
    {
        GivenTheCatalogResolves(AscentFactory.Aneto);

        await _handler.Handle(
            AscentFactory.RegisterCommand(clientAscentId: Guid.Empty), CancellationToken.None);

        await _ascentRepository.DidNotReceive().GetByClientAscentIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _ascentRepository.Received(1).Add(Arg.Is<Ascent>(ascent => ascent!.ClientAscentId == null));
    }

    private Ascent GivenTheKeyWasAlreadyUsed(Guid clientAscentId)
    {
        Ascent existing = AscentFactory.Registered(clientAscentId: clientAscentId);

        _ascentRepository
            .GetByClientAscentIdAsync(AscentFactory.OwnerId, clientAscentId, Arg.Any<CancellationToken>())
            .Returns(existing);

        return existing;
    }

    private void GivenTheCatalogResolves(PeakSnapshot peak) =>
        _peakCatalog.GetSnapshotAsync(peak.PeakId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(peak));
}
