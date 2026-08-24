using AscentService.Application.Ascents.UpdateAscent;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class UpdateAscentCommandHandlerTests
{
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly UpdateAscentCommandHandler _handler;

    public UpdateAscentCommandHandlerTests()
    {
        _dateTimeProvider.Today.Returns(AscentFactory.Today);
        _handler = new UpdateAscentCommandHandler(_ascentRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenTheOwnerEditsTheirOwnAscent_AppliesTheChange()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(CommandFor(ascent, visibility: AscentVisibility.Private), CancellationToken.None);

        ascent.Visibility.Should().Be(AscentVisibility.Private);
    }

    [Fact]
    public async Task Handle_WhenTheOwnerEditsTheirOwnAscent_CommitsTheTransaction()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnknownAscent_ReturnsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();
        _ascentRepository.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((Ascent?)null);

        Result result = await _handler.Handle(CommandFor(unknownId, AscentFactory.OwnerId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(unknownId));
    }

    [Fact]
    public async Task Handle_WhenAnotherUserEditsTheAscent_ReturnsNotFound()
    {
        Ascent ascent = GivenAnExistingAscent();

        Result result = await _handler.Handle(
            CommandFor(ascent.Id, AscentFactory.OtherUserId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(ascent.Id));
    }

    [Fact]
    public async Task Handle_WhenAnotherUserEditsTheAscent_DoesNotCommit()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(CommandFor(ascent.Id, AscentFactory.OtherUserId), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAFutureDate_ReturnsDateInFuture()
    {
        Ascent ascent = GivenAnExistingAscent();

        Result result = await _handler.Handle(
            CommandFor(ascent, ascentDate: AscentFactory.Today.AddDays(1)), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.DateInFuture);
    }

    [Fact]
    public async Task Handle_WithADateBefore1900_ReturnsDateTooOld()
    {
        Ascent ascent = GivenAnExistingAscent();

        Result result = await _handler.Handle(
            CommandFor(ascent, ascentDate: new DateOnly(1899, 12, 31)), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.DateTooOld);
    }

    private Ascent GivenAnExistingAscent()
    {
        Ascent ascent = AscentFactory.Registered();
        _ascentRepository.GetByIdAsync(ascent.Id, Arg.Any<CancellationToken>()).Returns(ascent);

        return ascent;
    }

    private static UpdateAscentCommand CommandFor(
        Ascent ascent,
        DateOnly? ascentDate = null,
        AscentVisibility visibility = AscentVisibility.Public) =>
        new(
            ascent.Id,
            AscentFactory.OwnerId,
            ascentDate ?? AscentFactory.Today,
            null,
            null,
            AscentConditions.Unreported,
            visibility);

    private static UpdateAscentCommand CommandFor(Guid ascentId, Guid userId) => new(
        ascentId,
        userId,
        AscentFactory.Today,
        null,
        null,
        AscentConditions.Unreported,
        AscentVisibility.Public);
}
