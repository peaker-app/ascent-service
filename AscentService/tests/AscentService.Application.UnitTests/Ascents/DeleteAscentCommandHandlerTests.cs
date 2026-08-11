using AscentService.Application.Ascents.DeleteAscent;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class DeleteAscentCommandHandlerTests
{
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteAscentCommandHandler _handler;

    public DeleteAscentCommandHandlerTests() =>
        _handler = new DeleteAscentCommandHandler(_ascentRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WhenTheOwnerDeletesTheirOwnAscent_RemovesItFromTheRepository()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(new DeleteAscentCommand(ascent.Id, AscentFactory.OwnerId), CancellationToken.None);

        _ascentRepository.Received(1).Remove(ascent);
    }

    [Fact]
    public async Task Handle_WhenTheOwnerDeletesTheirOwnAscent_RaisesTheDeletedEventForTheOutbox()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(new DeleteAscentCommand(ascent.Id, AscentFactory.OwnerId), CancellationToken.None);

        ascent.DomainEvents.OfType<AscentDeletedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithAnUnknownAscent_ReturnsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();
        _ascentRepository.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((Ascent?)null);

        Result result = await _handler.Handle(
            new DeleteAscentCommand(unknownId, AscentFactory.OwnerId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(unknownId));
    }

    [Fact]
    public async Task Handle_WhenAnotherUserDeletesTheAscent_ReturnsNotFound()
    {
        Ascent ascent = GivenAnExistingAscent();

        Result result = await _handler.Handle(
            new DeleteAscentCommand(ascent.Id, AscentFactory.OtherUserId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(ascent.Id));
    }

    [Fact]
    public async Task Handle_WhenAnotherUserDeletesTheAscent_DoesNotRemoveAnything()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(new DeleteAscentCommand(ascent.Id, AscentFactory.OtherUserId), CancellationToken.None);

        _ascentRepository.DidNotReceive().Remove(Arg.Any<Ascent>());
    }

    private Ascent GivenAnExistingAscent()
    {
        Ascent ascent = AscentFactory.Registered();
        _ascentRepository.GetByIdAsync(ascent.Id, Arg.Any<CancellationToken>()).Returns(ascent);

        return ascent;
    }
}
