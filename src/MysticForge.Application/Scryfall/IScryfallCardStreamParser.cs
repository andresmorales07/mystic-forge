namespace MysticForge.Application.Scryfall;

public interface IScryfallCardStreamParser
{
    IAsyncEnumerable<string> ReadCardJsonAsync(Stream source, CancellationToken ct);
}
