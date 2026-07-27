using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.AddAscentPhoto;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Application.UnitTests.TestData;
using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class AddAscentPhotoCommandHandlerTests
{
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();
    private readonly IPhotoStorage _photoStorage = Substitute.For<IPhotoStorage>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly AddAscentPhotoCommandHandler _handler;

    public AddAscentPhotoCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc));
        _photoStorage.UploadAsync(Arg.Any<PhotoFile>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(AscentFactory.StoredPhoto()));

        _handler = new AddAscentPhotoCommandHandler(
            _ascentRepository, _photoStorage, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithAValidJpeg_ReturnsThePersistedPhoto()
    {
        Ascent ascent = GivenAnExistingAscent();

        Result<AscentPhotoResponse> result = await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        result.Value.SecureUrl.Should().Be(AscentFactory.StoredPhoto().SecureUrl);
    }

    [Fact]
    public async Task Handle_WithAValidJpeg_AttachesThePhotoToTheAscent()
    {
        Ascent ascent = GivenAnExistingAscent();

        await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        ascent.Photos.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithAnUnknownAscent_ReturnsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();
        _ascentRepository.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((Ascent?)null);

        Result<AscentPhotoResponse> result = await _handler.Handle(
            new AddAscentPhotoCommand(unknownId, AscentFactory.OwnerId, AscentFactory.PhotoFile()),
            CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotFound(unknownId));
    }

    [Fact]
    public async Task Handle_WhenAnotherUserAttachesThePhoto_ReturnsNotOwned()
    {
        Ascent ascent = GivenAnExistingAscent();

        Result<AscentPhotoResponse> result = await _handler.Handle(
            new AddAscentPhotoCommand(ascent.Id, AscentFactory.OtherUserId, AscentFactory.PhotoFile()),
            CancellationToken.None);

        result.Error.Should().Be(AscentErrors.NotOwned);
    }

    [Fact]
    public async Task Handle_OnAFullAscent_ReturnsPhotoLimitReached()
    {
        Ascent ascent = GivenAnExistingAscent(photoCount: Ascent.MaxPhotos);

        Result<AscentPhotoResponse> result = await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.PhotoLimitReached);
    }

    [Fact]
    public async Task Handle_OnAFullAscent_NeverUploadsToStorage()
    {
        Ascent ascent = GivenAnExistingAscent(photoCount: Ascent.MaxPhotos);

        await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        await _photoStorage.DidNotReceive().UploadAsync(Arg.Any<PhotoFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnsupportedFormat_ReturnsUnsupportedPhotoFormat()
    {
        Ascent ascent = GivenAnExistingAscent();
        PhotoFile pdf = AscentFactory.PhotoFile([0x25, 0x50, 0x44, 0x46]);

        Result<AscentPhotoResponse> result = await _handler.Handle(
            new AddAscentPhotoCommand(ascent.Id, AscentFactory.OwnerId, pdf), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.UnsupportedPhotoFormat);
    }

    [Fact]
    public async Task Handle_WithAnUnsupportedFormat_NeverUploadsToStorage()
    {
        Ascent ascent = GivenAnExistingAscent();
        PhotoFile pdf = AscentFactory.PhotoFile([0x25, 0x50, 0x44, 0x46]);

        await _handler.Handle(
            new AddAscentPhotoCommand(ascent.Id, AscentFactory.OwnerId, pdf), CancellationToken.None);

        await _photoStorage.DidNotReceive().UploadAsync(Arg.Any<PhotoFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAFileOverTheSizeLimit_ReturnsPhotoTooLarge()
    {
        Ascent ascent = GivenAnExistingAscent();
        PhotoFile oversized = AscentFactory.PhotoFile(new byte[AddAscentPhotoCommand.MaxSizeInBytes + 1]);

        Result<AscentPhotoResponse> result = await _handler.Handle(
            new AddAscentPhotoCommand(ascent.Id, AscentFactory.OwnerId, oversized), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.PhotoTooLarge);
    }

    [Fact]
    public async Task Handle_WhenStorageFails_ReturnsPhotoUploadFailed()
    {
        Ascent ascent = GivenAnExistingAscent();
        _photoStorage.UploadAsync(Arg.Any<PhotoFile>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<StoredPhoto>(AscentErrors.PhotoUploadFailed));

        Result<AscentPhotoResponse> result = await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        result.Error.Should().Be(AscentErrors.PhotoUploadFailed);
    }

    [Fact]
    public async Task Handle_WhenStorageFails_DoesNotCommit()
    {
        Ascent ascent = GivenAnExistingAscent();
        _photoStorage.UploadAsync(Arg.Any<PhotoFile>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<StoredPhoto>(AscentErrors.PhotoUploadFailed));

        await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPersistenceFails_RemovesTheOrphanedPhotoFromStorage()
    {
        Ascent ascent = GivenAnExistingAscent();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("ux_ascent_photo_position"));

        Func<Task> attach = () => _handler.Handle(CommandFor(ascent), CancellationToken.None);

        await attach.Should().ThrowAsync<InvalidOperationException>();
        await _photoStorage.Received(1)
            .TryDeleteAsync(AscentFactory.StoredPhoto().PublicId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheAggregateRejectsThePhoto_RemovesTheOrphanedPhotoFromStorage()
    {
        Ascent ascent = GivenAnExistingAscent();
        FillTheLastSlotBehindTheEligibilityCheck(ascent);

        await _handler.Handle(CommandFor(ascent), CancellationToken.None);

        await _photoStorage.Received(1)
            .TryDeleteAsync(AscentFactory.StoredPhoto().PublicId, Arg.Any<CancellationToken>());
    }

    private void FillTheLastSlotBehindTheEligibilityCheck(Ascent ascent) =>
        _photoStorage.UploadAsync(Arg.Any<PhotoFile>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                for (int index = 0; index < Ascent.MaxPhotos; index++)
                {
                    ascent.AddPhoto(
                        new PhotoUpload($"public-{index}", $"https://cdn/{index}.jpg", 800, 600), DateTime.UnixEpoch);
                }

                return Result.Success(AscentFactory.StoredPhoto());
            });

    private static AddAscentPhotoCommand CommandFor(Ascent ascent) =>
        new(ascent.Id, AscentFactory.OwnerId, AscentFactory.PhotoFile());

    private Ascent GivenAnExistingAscent(int photoCount = 0)
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
