using AscentService.Domain.Ascents;
using AscentService.Domain.Ascents.Events;
using AscentService.Domain.UnitTests.TestData;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AscentService.Domain.UnitTests.Ascents;

public sealed class AscentTests
{
    [Fact]
    public void Create_WithValidDraft_ReturnsAscent()
    {
        Result<Ascent> result = Ascent.Create(AscentMother.Draft(), AscentMother.Today);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithValidDraft_RaisesRegisteredEventCarryingTheDenormalisedPeak()
    {
        Ascent ascent = AscentMother.Registered();

        ascent.DomainEvents.OfType<AscentRegisteredDomainEvent>().Single()
            .Should().BeEquivalentTo(new
            {
                AscentMother.Aneto.PeakId,
                PeakName = AscentMother.Aneto.Name,
                PeakAltitudeMeters = AscentMother.Aneto.AltitudeMeters
            });
    }

    [Fact]
    public void Create_WithPrivateVisibility_RaisesRegisteredEventCarryingTheVisibility()
    {
        AscentDetails details = new(
            AscentMother.Today, null, null, AscentConditions.Unreported, AscentVisibility.Private);

        Ascent ascent = Ascent.Create(AscentMother.Draft(details), AscentMother.Today).Value;

        ascent.DomainEvents.OfType<AscentRegisteredDomainEvent>().Single()
            .Visibility.Should().Be(nameof(AscentVisibility.Private));
    }

    [Fact]
    public void Create_WithoutVisibility_DefaultsToPublic()
    {
        AscentDetails details = new(AscentMother.Today, null, null, AscentConditions.Unreported, default);

        Ascent ascent = Ascent.Create(AscentMother.Draft(details), AscentMother.Today).Value;

        ascent.Visibility.Should().Be(AscentVisibility.Public);
    }

    [Fact]
    public void Create_WithFutureDate_ReturnsDateInFuture()
    {
        AscentDetails details = AscentMother.Details(AscentMother.Today.AddDays(1));

        Result<Ascent> result = Ascent.Create(AscentMother.Draft(details), AscentMother.Today);

        result.Error.Should().Be(AscentErrors.DateInFuture);
    }

    [Fact]
    public void Create_WithDateBefore1900_ReturnsDateTooOld()
    {
        AscentDetails details = AscentMother.Details(new DateOnly(1899, 12, 31));

        Result<Ascent> result = Ascent.Create(AscentMother.Draft(details), AscentMother.Today);

        result.Error.Should().Be(AscentErrors.DateTooOld);
    }

    [Fact]
    public void Create_WithCompanionsOverTheLimit_ReturnsCompanionsTooLong()
    {
        AscentDetails details = AscentMother.Details(companions: new string('a', Ascent.MaxCompanionsLength + 1));

        Result<Ascent> result = Ascent.Create(AscentMother.Draft(details), AscentMother.Today);

        result.Error.Should().Be(AscentErrors.CompanionsTooLong);
    }

    [Fact]
    public void Create_WithRouteNotesOverTheLimit_ReturnsRouteNotesTooLong()
    {
        AscentDetails details = AscentMother.Details(routeNotes: new string('a', Ascent.MaxRouteNotesLength + 1));

        Result<Ascent> result = Ascent.Create(AscentMother.Draft(details), AscentMother.Today);

        result.Error.Should().Be(AscentErrors.RouteNotesTooLong);
    }

    [Fact]
    public void Amend_WithValidDetails_AppliesTheNewValues()
    {
        Ascent ascent = AscentMother.Registered();
        AscentDetails details = AscentMother.Details(
            ascentDate: AscentMother.Today.AddDays(-10),
            companions: "Marta y Julio",
            visibility: AscentVisibility.Private);

        ascent.Amend(details, AscentMother.Today);

        ascent.Should().BeEquivalentTo(new
        {
            AscentDate = AscentMother.Today.AddDays(-10),
            Companions = "Marta y Julio",
            Visibility = AscentVisibility.Private
        });
    }

    [Fact]
    public void Amend_WithFutureDate_ReturnsDateInFutureAndKeepsTheOriginalDate()
    {
        Ascent ascent = AscentMother.Registered();
        AscentDetails details = AscentMother.Details(AscentMother.Today.AddDays(1));

        Result result = ascent.Amend(details, AscentMother.Today);

        result.Error.Should().Be(AscentErrors.DateInFuture);
    }

    [Fact]
    public void Amend_WithValidDetails_RaisesUpdatedEvent()
    {
        Ascent ascent = AscentMother.Registered();

        ascent.Amend(AscentMother.Details(visibility: AscentVisibility.Private), AscentMother.Today);

        ascent.DomainEvents.OfType<AscentUpdatedDomainEvent>().Single()
            .Visibility.Should().Be(nameof(AscentVisibility.Private));
    }

    [Fact]
    public void SyncPeak_WithANewName_UpdatesTheDenormalisedSnapshot()
    {
        Ascent ascent = AscentMother.Registered();

        ascent.SyncPeak("Pico de Aneto", 3404);

        ascent.Peak.Name.Should().Be("Pico de Aneto");
    }

    [Fact]
    public void SyncPeak_WithTheSameData_LeavesTheSnapshotUntouched()
    {
        Ascent ascent = AscentMother.Registered();
        PeakSnapshot original = ascent.Peak;

        ascent.SyncPeak(AscentMother.Aneto.Name, AscentMother.Aneto.AltitudeMeters);

        ascent.Peak.Should().BeSameAs(original);
    }

    [Fact]
    public void IsVisibleTo_WhenPublicAndRequesterIsAnonymous_ReturnsTrue()
    {
        Ascent ascent = AscentMother.Registered();

        ascent.IsVisibleTo(null).Should().BeTrue();
    }

    [Fact]
    public void IsVisibleTo_WhenPrivateAndRequesterIsAnonymous_ReturnsFalse()
    {
        Ascent ascent = AscentMother.Registered(AscentMother.Details(visibility: AscentVisibility.Private));

        ascent.IsVisibleTo(null).Should().BeFalse();
    }

    [Fact]
    public void IsVisibleTo_WhenPrivateAndRequesterIsTheOwner_ReturnsTrue()
    {
        Ascent ascent = AscentMother.Registered(AscentMother.Details(visibility: AscentVisibility.Private));

        ascent.IsVisibleTo(AscentMother.OwnerId).Should().BeTrue();
    }

    [Fact]
    public void IsVisibleTo_WhenPrivateAndRequesterIsAnotherUser_ReturnsFalse()
    {
        Ascent ascent = AscentMother.Registered(AscentMother.Details(visibility: AscentVisibility.Private));

        ascent.IsVisibleTo(Guid.CreateVersion7()).Should().BeFalse();
    }

    [Fact]
    public void MarkDeleted_WithPhotos_RaisesOnePhotoRemovedEventPerPhoto()
    {
        Ascent ascent = AscentMother.WithPhotos(3);

        ascent.MarkDeleted();

        ascent.DomainEvents.OfType<AscentPhotoRemovedDomainEvent>().Should().HaveCount(3);
    }

    [Fact]
    public void MarkDeleted_RaisesDeletedEvent()
    {
        Ascent ascent = AscentMother.Registered();

        ascent.MarkDeleted();

        ascent.DomainEvents.OfType<AscentDeletedDomainEvent>().Should().ContainSingle();
    }
}
