namespace AscentService.Application.Ascents.GetAscentById;

public sealed record AscentPhotoResponse(
    Guid Id,
    string SecureUrl,
    int Width,
    int Height,
    short Position,
    DateTime UploadedAtUtc);
