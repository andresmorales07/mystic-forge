using MysticForge.Domain.Cards;

namespace MysticForge.Application.Scryfall;

public interface IPrintingWriter
{
    Task<PrintingUpsertResult> UpsertAsync(IReadOnlyList<Printing> printings, CancellationToken ct);
}

public sealed record PrintingUpsertResult(int Inserted, int Updated);
