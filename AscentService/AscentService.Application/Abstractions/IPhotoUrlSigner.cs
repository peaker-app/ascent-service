namespace AscentService.Application.Abstractions;

public interface IPhotoUrlSigner
{
    string Sign(string publicId, TimeSpan lifetime);
}
