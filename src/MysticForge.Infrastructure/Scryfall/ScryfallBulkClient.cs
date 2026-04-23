using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MysticForge.Application.Scryfall;

namespace MysticForge.Infrastructure.Scryfall;

public sealed class ScryfallBulkClient : IScryfallBulkClient
{
    private readonly HttpClient _httpClient;

    public ScryfallBulkClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ScryfallBulkMetadata> GetBulkMetadataAsync(string bulkType, CancellationToken ct)
    {
        var index = await _httpClient.GetFromJsonAsync<BulkDataIndexJson>("bulk-data", ct)
            ?? throw new InvalidOperationException("Scryfall returned null bulk-data index.");

        var entry = index.Data.FirstOrDefault(e => string.Equals(e.Type, bulkType, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Scryfall bulk-data index has no entry for type '{bulkType}'.");

        return new ScryfallBulkMetadata(entry.Type, new Uri(entry.DownloadUri), entry.UpdatedAt);
    }

    public async Task<Stream> DownloadBulkAsync(Uri downloadUri, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    private sealed record BulkDataIndexJson(
        [property: JsonPropertyName("data")] List<BulkDataEntryJson> Data);

    private sealed record BulkDataEntryJson(
        [property: JsonPropertyName("type")]         string Type,
        [property: JsonPropertyName("download_uri")] string DownloadUri,
        [property: JsonPropertyName("updated_at")]   DateTimeOffset UpdatedAt);
}
