namespace AscentService.Domain.Ascents;

public sealed record PhotoUpload(string CloudinaryPublicId, int Width, int Height)
{
    public const int MaxPublicIdLength = 255;
}
