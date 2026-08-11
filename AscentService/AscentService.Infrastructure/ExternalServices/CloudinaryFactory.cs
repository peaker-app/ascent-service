using CloudinaryDotNet;
using Microsoft.Extensions.Options;

namespace AscentService.Infrastructure.ExternalServices;

internal sealed class CloudinaryFactory
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryFactory(IOptions<CloudinaryOptions> options)
    {
        Options = options.Value;
        _cloudinary = new Cloudinary(new Account(Options.CloudName, Options.ApiKey, Options.ApiSecret));
        _cloudinary.Api.Secure = true;
    }

    public CloudinaryOptions Options { get; }

    public Cloudinary Client => _cloudinary;
}
