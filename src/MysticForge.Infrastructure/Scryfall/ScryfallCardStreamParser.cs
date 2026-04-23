using System.Runtime.CompilerServices;
using System.Text.Json;
using MysticForge.Application.Scryfall;

namespace MysticForge.Infrastructure.Scryfall;

public sealed class ScryfallCardStreamParser : IScryfallCardStreamParser
{
    public async IAsyncEnumerable<string> ReadCardJsonAsync(
        Stream source,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(source, cancellationToken: ct);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            yield return element.GetRawText();
        }
    }
}
