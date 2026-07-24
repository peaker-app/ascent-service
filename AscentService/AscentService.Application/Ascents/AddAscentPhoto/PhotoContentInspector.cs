namespace AscentService.Application.Ascents.AddAscentPhoto;

internal static class PhotoContentInspector
{
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffMagic = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] WebpMagic = [0x57, 0x45, 0x42, 0x50];

    public static bool IsSupported(ReadOnlySpan<byte> content) =>
        IsJpeg(content) || IsPng(content) || IsWebp(content);

    private static bool IsJpeg(ReadOnlySpan<byte> content) => content.StartsWith(JpegMagic);

    private static bool IsPng(ReadOnlySpan<byte> content) => content.StartsWith(PngMagic);

    private static bool IsWebp(ReadOnlySpan<byte> content) =>
        content.Length >= 12 && content.StartsWith(RiffMagic) && content.Slice(8, 4).SequenceEqual(WebpMagic);
}
