using AscentService.Application.Ascents.RemoveAscentPhoto;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class RemoveAscentPhotoCommandHandlerTests
{
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RemoveAscentPhotoCommandHandler _handler;

    public RemoveAscentPhotoCommandHandlerTests() =>
        _handler = new RemoveAscentPhotoCommandHandler(_ascentRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WhenTheOwnerRemovesTheirPhoto_DetachesItFromTheAscent()
    {
        Ascent ascent = GivenAnAscentWithPhotos(2);
        Guid photoId = ascent.Photos.First().Id;

        await _handler.Handle(
            new RemoveAscentPhotoCommand(ascent.Id, photoId, AscentFactory.OwnerId), CancellationToken.None);

        ascent.Photos.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenTheOwnerRemovesTheirPhoto_RaisesTheCompensationEvent()
    {
        Ascent ascent = GivenAnAscentWithPhotos(1);
        Guid photoId = ascent.Photos.Single().Id;

        await _handler.Handle(
            new RemoveAscentPhotoCommand(ascent.Id, photoId, AscentFactory.OwnerId), CancellationToken.None);

        ascent.DomainEvents.OfType<AscentPhotoRemovedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithAnUnknownAscent_ReturnsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();
        _ascentRepository.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((Ascent?)null);

        Result result = await _handler.Handle(
            new RemoveAscentPhotoCommand(unknownId, Guid.CreateVersion7(), AscentFactory.OwnerId),
            CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(unknownId));
    }

    [Fact]
    public async Task Handle_WhenAnotherUserRemovesThePhoto_ReturnsNotOwned()
    {
        Ascent ascent = GivenAnAscentWithPhotos(1);
        Guid photoId = ascent.Photos.Single().Id;

        Result result = await _handler.Handle(
            new RemoveAscentPhotoCommand(ascent.Id, photoId, AscentFactory.OtherUserId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotOwned);
    }

    [Fact]
    public async Task Handle_WithAnUnknownPhoto_ReturnsPhotoNotFound()
    {
        Ascent ascent = GivenAnAscentWithPhotos(1);
        Guid unknownPhotoId = Guid.CreateVersion7();

        Result result = await _handler.Handle(
            new RemoveAscentPhotoCommand(ascent.Id, unknownPhotoId, AscentFactory.OwnerId), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.PhotoNotFound(unknownPhotoId));
    }

    private Ascent GivenAnAscentWithPhotos(int photoCount)
    {
        Ascent ascent = AscentFactory.Registered();

        for (int index = 0; index < photoCount; index++)
        {
            ascent.AddPhoto(
                new PhotoUpload($"public-{index}", $"https://cdn/{index}.jpg", 800, 600), DateTime.UnixEpoch);
        }

        _ascentRepository.GetByIdAsync(ascent.Id, Arg.Any<CancellationToken>()).Returns(ascent);

        return ascent;
    }
}
