namespace AscentService.Domain.Ascents;

public sealed record PhotoUpload(string CloudinaryPublicId, string SecureUrl, int Width, int Height)
{
    public const int MaxPublicIdLength = 255;
    public const int MaxSecureUrlLength = 500;
}
