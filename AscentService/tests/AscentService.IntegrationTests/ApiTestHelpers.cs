using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AscentService.Application.Ascents.GetAscentById;

namespace AscentService.IntegrationTests;

internal static class ApiTestHelpers
{
    public static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    public static Guid NewUserId() => Guid.CreateVersion7();

    public static string AscentRoute(Guid ascentId) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/ascents/{ascentId}");

    public static string ByUserRoute(Guid userId) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/ascents/by-user/{userId}");

    public static async Task<Guid> RegisterAscentAsync(this HttpClient client, object body)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/ascents", body);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public static async Task<AscentPhotoResponse> AddPhotoAsync(this HttpClient client, Guid ascentId)
    {
        HttpResponseMessage response = await client.UploadPhotoAsync(ascentId, JpegBytes);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AscentPhotoResponse>())!;
    }

    public static async Task<HttpResponseMessage> UploadPhotoAsync(
        this HttpClient client,
        Guid ascentId,
        byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "cumbre.jpg");

        return await client.PostAsync($"{AscentRoute(ascentId)}/photos", form);
    }
}
