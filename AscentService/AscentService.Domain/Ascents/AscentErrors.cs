using Common.Domain.Results;

namespace AscentService.Domain.Ascents;

public static class AscentErrors
{
    public static readonly Error DateInFuture =
        Error.Validation("Ascent.DateInFuture", "La fecha de ascensión no puede ser futura.");

    public static readonly Error DateTooOld =
        Error.Validation("Ascent.DateTooOld", "La fecha de ascensión no puede ser anterior al 1 de enero de 1900.");

    public static readonly Error CompanionsTooLong = Error.Validation(
        "Ascent.CompanionsTooLong",
        $"Los compañeros de ruta admiten como máximo {Ascent.MaxCompanionsLength} caracteres.");

    public static readonly Error RouteNotesTooLong = Error.Validation(
        "Ascent.RouteNotesTooLong",
        $"Las notas de vía admiten como máximo {Ascent.MaxRouteNotesLength} caracteres.");

    public static readonly Error PhotoLimitReached = Error.Conflict(
        "Ascent.PhotoLimitReached",
        $"Una ascensión admite como máximo {Ascent.MaxPhotos} fotos.");

    public static readonly Error UnsupportedPhotoFormat = Error.Validation(
        "Ascent.UnsupportedPhotoFormat",
        "Solo se admiten imágenes JPEG, PNG o WebP.");

    public static readonly Error PhotoTooLarge = Error.Validation(
        "Ascent.PhotoTooLarge",
        "La imagen supera el tamaño máximo admitido de 10 MB.");

    public static readonly Error PhotoUploadFailed =
        Error.Failure("Ascent.PhotoUploadFailed", "No se pudo almacenar la imagen.");

    public static readonly Error NotOwned =
        Error.Forbidden("Ascent.NotOwned", "La ascensión pertenece a otro usuario.");

    public static readonly Error PeakCatalogUnavailable = Error.Unavailable(
        "Ascent.PeakCatalogUnavailable",
        "El catálogo de picos no está disponible. Inténtalo de nuevo en unos instantes.");

    public static readonly Error ProfileDirectoryUnavailable = Error.Unavailable(
        "Ascent.ProfileDirectoryUnavailable",
        "El servicio de perfiles no está disponible. Inténtalo de nuevo en unos instantes.");

    public static Error NotFound(Guid ascentId) =>
        Error.NotFound("Ascent.NotFound", $"No existe la ascensión {ascentId}.");

    public static Error PhotoNotFound(Guid photoId) =>
        Error.NotFound("Ascent.PhotoNotFound", $"No existe la foto {photoId}.");

    public static Error PeakNotFound(Guid peakId) =>
        Error.NotFound("Ascent.PeakNotFound", $"No existe el pico {peakId} en el catálogo.");

    public static Error ProfileNotVisible(Guid userId) =>
        Error.NotFound("Ascent.ProfileNotVisible", $"No existe un perfil público para el usuario {userId}.");
}
