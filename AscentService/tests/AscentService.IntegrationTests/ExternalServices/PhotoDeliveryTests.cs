using AscentService.Infrastructure.ExternalServices;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.ExternalServices;

public sealed class PhotoDeliveryTests
{
    [Fact]
    public void Sanitizing_StripsTheColourProfile() =>
        PhotoDelivery.Sanitizing().ToString().Should().Contain("strip_profile");

    [Fact]
    public void Sanitizing_StripsTheExifBlock() =>
        PhotoDelivery.Sanitizing().ToString().Should().Contain("strip_exif");

    [Fact]
    public void StoredFormat_ForcesARecodeSoTheStoredBytesAreNeverTheUploadedOnes() =>
        PhotoDelivery.StoredFormat.Should().Be("webp");
}
