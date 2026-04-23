namespace MysticForge.Application.Scryfall;

public interface IScryfallBulkClient
{
    Task<ScryfallBulkMetadata> GetBulkMetadataAsync(string bulkType, CancellationToken ct);
    Task<Stream> DownloadBulkAsync(Uri downloadUri, CancellationToken ct);
}
