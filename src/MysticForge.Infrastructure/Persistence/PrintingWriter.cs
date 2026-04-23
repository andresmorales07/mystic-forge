using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Scryfall;
using MysticForge.Domain.Cards;

namespace MysticForge.Infrastructure.Persistence;

public sealed class PrintingWriter : IPrintingWriter
{
    private readonly MysticForgeDbContext _db;

    public PrintingWriter(MysticForgeDbContext db)
    {
        _db = db;
    }

    public async Task<PrintingUpsertResult> UpsertAsync(IReadOnlyList<Printing> printings, CancellationToken ct)
    {
        if (printings.Count == 0) return new PrintingUpsertResult(0, 0);

        // Clear residue from prior batches in this scope — identity-map conflicts otherwise arise
        // across FlushBatch calls within a single Hangfire job.
        _db.ChangeTracker.Clear();

        var incomingIds = printings.Select(p => p.ScryfallId).ToArray();
        var existingIds = await _db.Printings
            .AsNoTracking()
            .Where(p => incomingIds.Contains(p.ScryfallId))
            .Select(p => p.ScryfallId)
            .ToHashSetAsync(ct);

        int inserted = 0, updated = 0;
        foreach (var printing in printings)
        {
            if (existingIds.Contains(printing.ScryfallId))
            {
                _db.Printings.Update(printing);
                updated++;
            }
            else
            {
                await _db.Printings.AddAsync(printing, ct);
                inserted++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new PrintingUpsertResult(inserted, updated);
    }
}
