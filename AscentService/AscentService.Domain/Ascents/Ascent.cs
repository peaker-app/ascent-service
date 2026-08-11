using AscentService.Domain.Ascents.Events;
using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace AscentService.Domain.Ascents;

public sealed class Ascent : AggregateRoot
{
    public const int MaxPhotos = 3;
    public const int MaxCompanionsLength = 500;
    public const int MaxRouteNotesLength = 2000;

    private static readonly DateOnly MinimumAscentDate = new(1900, 1, 1);

    private readonly List<AscentPhoto> _photos = [];

    private Ascent()
    {
    }

    private Ascent(Guid id, AscentDraft draft) : base(id)
    {
        UserId = draft.UserId;
        Peak = draft.Peak;
        ClientAscentId = draft.ClientAscentId;
        Apply(draft.Details);
    }

    public Guid UserId { get; private set; }

    public Guid? ClientAscentId { get; private set; }

    public PeakSnapshot Peak { get; private set; } = null!;

    public DateOnly AscentDate { get; private set; }

    public string? Companions { get; private set; }

    public string? RouteNotes { get; private set; }

    public AscentConditions Conditions { get; private set; } = AscentConditions.Unreported;

    public AscentVisibility Visibility { get; private set; }

    public IReadOnlyCollection<AscentPhoto> Photos => [.. _photos.OrderBy(photo => photo.Position)];

    public bool HasRoomForPhotos => _photos.Count < MaxPhotos;

    public static Result<Ascent> Create(AscentDraft draft, DateOnly today)
    {
        Result validation = Validate(draft.Details, today);

        if (validation.IsFailure)
        {
            return Result.Failure<Ascent>(validation.Error);
        }

        Ascent ascent = new(Guid.CreateVersion7(), draft);

        ascent.Raise(new AscentRegisteredDomainEvent(
            ascent.Id,
            ascent.UserId,
            ascent.Peak.PeakId,
            ascent.Peak.Name,
            ascent.Peak.AltitudeMeters,
            ascent.AscentDate,
            ascent.Visibility.ToString()));

        return ascent;
    }

    public Result Amend(AscentDetails details, DateOnly today)
    {
        Result validation = Validate(details, today);

        if (validation.IsFailure)
        {
            return validation;
        }

        Apply(details);
        Raise(new AscentUpdatedDomainEvent(Id, UserId, Peak.PeakId, AscentDate, Visibility.ToString()));

        return Result.Success();
    }

    public Result<AscentPhoto> AddPhoto(PhotoUpload upload, DateTime uploadedAtUtc)
    {
        if (_photos.Count >= MaxPhotos)
        {
            return Result.Failure<AscentPhoto>(AscentErrors.PhotoLimitReached);
        }

        AscentPhoto photo = AscentPhoto.Create(upload, (short)_photos.Count, uploadedAtUtc);
        _photos.Add(photo);
        Raise(new AscentPhotoStoredDomainEvent(Id, photo.CloudinaryPublicId));

        return photo;
    }

    public Result RemovePhoto(Guid photoId)
    {
        AscentPhoto? photo = _photos.Find(candidate => candidate.Id == photoId);

        if (photo is null)
        {
            return Result.Failure(AscentErrors.PhotoNotFound(photoId));
        }

        _photos.Remove(photo);
        Raise(new AscentPhotoRemovedDomainEvent(Id, photo.CloudinaryPublicId));
        Reindex();

        return Result.Success();
    }

    public void SyncPeak(string name, int altitudeMeters)
    {
        if (string.Equals(Peak.Name, name, StringComparison.Ordinal) && Peak.AltitudeMeters == altitudeMeters)
        {
            return;
        }

        Peak = Peak with { Name = name, AltitudeMeters = altitudeMeters };
    }

    public void Republish() => Raise(new AscentRegisteredDomainEvent(
        Id,
        UserId,
        Peak.PeakId,
        Peak.Name,
        Peak.AltitudeMeters,
        AscentDate,
        Visibility.ToString()));

    public void MarkDeleted()
    {
        Raise(new AscentDeletedDomainEvent(Id, UserId, Peak.PeakId, AscentDate));

        foreach (AscentPhoto photo in _photos)
        {
            Raise(new AscentPhotoRemovedDomainEvent(Id, photo.CloudinaryPublicId));
        }
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    public bool IsVisibleTo(Guid? requesterId) =>
        Visibility is AscentVisibility.Public || requesterId == UserId;

    private static Result Validate(AscentDetails details, DateOnly today)
    {
        if (details.AscentDate > today)
        {
            return Result.Failure(AscentErrors.DateInFuture);
        }

        if (details.AscentDate < MinimumAscentDate)
        {
            return Result.Failure(AscentErrors.DateTooOld);
        }

        if (details.Companions is { Length: > MaxCompanionsLength })
        {
            return Result.Failure(AscentErrors.CompanionsTooLong);
        }

        return details.RouteNotes is { Length: > MaxRouteNotesLength }
            ? Result.Failure(AscentErrors.RouteNotesTooLong)
            : Result.Success();
    }

    private void Apply(AscentDetails details)
    {
        AscentDate = details.AscentDate;
        Companions = details.Companions;
        RouteNotes = details.RouteNotes;
        Conditions = details.Conditions;
        Visibility = details.Visibility;
    }

    private void Reindex()
    {
        short position = 0;

        foreach (AscentPhoto photo in _photos.OrderBy(photo => photo.Position))
        {
            photo.MoveTo(position++);
        }
    }
}
