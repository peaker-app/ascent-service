using AscentService.Domain.Ascents;
using AscentService.Domain.Ascents.Events;
using AscentService.Domain.UnitTests.TestData;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AscentService.Domain.UnitTests.Ascents;

public sealed class AscentPhotoTests
{
    [Fact]
    public void AddPhoto_OnAnAscentWithoutPhotos_AssignsPositionZero()
    {
        Ascent ascent = AscentMother.Registered();

        Result<AscentPhoto> result = ascent.AddPhoto(AscentMother.Upload(), DateTime.UnixEpoch);

        result.Value.Position.Should().Be(0);
    }

    [Fact]
    public void AddPhoto_UpToTheLimit_KeepsEveryPhoto()
    {
        Ascent ascent = AscentMother.WithPhotos(Ascent.MaxPhotos);

        ascent.Photos.Should().HaveCount(Ascent.MaxPhotos);
    }

    [Fact]
    public void AddPhoto_BeyondTheLimit_ReturnsPhotoLimitReached()
    {
        Ascent ascent = AscentMother.WithPhotos(Ascent.MaxPhotos);

        Result<AscentPhoto> result = ascent.AddPhoto(AscentMother.Upload(9), DateTime.UnixEpoch);

        result.Error.Should().Be(AscentErrors.PhotoLimitReached);
    }

    [Fact]
    public void AddPhoto_BeyondTheLimit_DoesNotAddAFourthPhoto()
    {
        Ascent ascent = AscentMother.WithPhotos(Ascent.MaxPhotos);

        ascent.AddPhoto(AscentMother.Upload(9), DateTime.UnixEpoch);

        ascent.Photos.Should().HaveCount(Ascent.MaxPhotos);
    }

    [Fact]
    public void RemovePhoto_InTheMiddle_ReindexesTheRemainingPositionsWithoutGaps()
    {
        Ascent ascent = AscentMother.WithPhotos(3);
        Guid middlePhotoId = ascent.Photos.ElementAt(1).Id;

        ascent.RemovePhoto(middlePhotoId);

        ascent.Photos.Select(photo => photo.Position).Should().Equal((short)0, (short)1);
    }

    [Fact]
    public void RemovePhoto_InTheMiddle_KeepsTheSurvivorsInTheirOriginalOrder()
    {
        Ascent ascent = AscentMother.WithPhotos(3);
        string firstPublicId = ascent.Photos.ElementAt(0).CloudinaryPublicId;
        string lastPublicId = ascent.Photos.ElementAt(2).CloudinaryPublicId;

        ascent.RemovePhoto(ascent.Photos.ElementAt(1).Id);

        ascent.Photos.Select(photo => photo.CloudinaryPublicId).Should().Equal(firstPublicId, lastPublicId);
    }

    [Fact]
    public void RemovePhoto_RaisesPhotoRemovedEventWithThePublicIdToCompensate()
    {
        Ascent ascent = AscentMother.WithPhotos(1);
        AscentPhoto photo = ascent.Photos.Single();

        ascent.RemovePhoto(photo.Id);

        ascent.DomainEvents.OfType<AscentPhotoRemovedDomainEvent>().Single()
            .CloudinaryPublicId.Should().Be(photo.CloudinaryPublicId);
    }

    [Fact]
    public void RemovePhoto_WithAnUnknownId_ReturnsPhotoNotFound()
    {
        Ascent ascent = AscentMother.WithPhotos(1);
        Guid unknownPhotoId = Guid.CreateVersion7();

        Result result = ascent.RemovePhoto(unknownPhotoId);

        result.Error.Should().Be(AscentErrors.PhotoNotFound(unknownPhotoId));
    }

    [Fact]
    public void AddPhoto_AfterRemovingOne_ReusesTheFreedPosition()
    {
        Ascent ascent = AscentMother.WithPhotos(3);
        ascent.RemovePhoto(ascent.Photos.ElementAt(0).Id);

        Result<AscentPhoto> result = ascent.AddPhoto(AscentMother.Upload(9), DateTime.UnixEpoch);

        result.Value.Position.Should().Be(2);
    }
}
