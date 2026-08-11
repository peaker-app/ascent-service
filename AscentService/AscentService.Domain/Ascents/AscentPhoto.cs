namespace AscentService.Domain.Ascents;

public sealed class AscentPhoto
{
    private AscentPhoto()
    {
    }

    private AscentPhoto(Guid id, PhotoUpload upload, short position, DateTime uploadedAtUtc)
    {
        Id = id;
        CloudinaryPublicId = upload.CloudinaryPublicId;
        Width = upload.Width;
        Height = upload.Height;
        Position = position;
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid Id { get; private set; }

    public string CloudinaryPublicId { get; private set; } = null!;

    public int Width { get; private set; }

    public int Height { get; private set; }

    public short Position { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    internal static AscentPhoto Create(PhotoUpload upload, short position, DateTime uploadedAtUtc) =>
        new(Guid.CreateVersion7(), upload, position, uploadedAtUtc);

    internal void MoveTo(short position) => Position = position;
}
