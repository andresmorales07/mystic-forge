using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;

namespace MysticForge.Application.Scryfall;

public sealed class ScryfallIngestJob
{
    private const int BatchSize = 1_000;

    private readonly IScryfallBulkClient _client;
    private readonly IScryfallCardStreamParser _parser;
    private readonly ICardWriter _cards;
    private readonly IPrintingWriter _printings;
    private readonly IOracleEventEmitter _emitter;
    private readonly IIngestRunTracker _tracker;
    private readonly IClock _clock;

    public ScryfallIngestJob(
        IScryfallBulkClient client,
        IScryfallCardStreamParser parser,
        ICardWriter cards,
        IPrintingWriter printings,
        IOracleEventEmitter emitter,
        IIngestRunTracker tracker,
        IClock clock)
    {
        _client = client;
        _parser = parser;
        _cards = cards;
        _printings = printings;
        _emitter = emitter;
        _tracker = tracker;
        _clock = clock;
    }

    public async Task RunAsync(string bulkType, CancellationToken ct)
    {
        var metadata = await _client.GetBulkMetadataAsync(bulkType, ct);
        var lastSeen = await _tracker.GetLastSuccessfulUpdatedAtAsync(bulkType, ct);

        if (lastSeen.HasValue && lastSeen.Value == metadata.UpdatedAt)
        {
            await _tracker.RecordSkipAsync(bulkType, metadata.UpdatedAt, ct);
            return;
        }

        var runId = await _tracker.StartAsync(bulkType, metadata.UpdatedAt, ct);
        var counts = new MutableCounts();

        try
        {
            await using var source = await _client.DownloadBulkAsync(metadata.DownloadUri, ct);

            // Scryfall's default_cards bulk file has one row per printing. Many printings can share
            // a single oracle_id (e.g. Sol Ring has ~50 printings). We dedupe Cards by oracle_id
            // within a batch — each printing maps to the same Card shape, so "last one wins" is
            // safe and prevents EF from tracking the same primary key twice in a single SaveChanges.
            var cardBatch = new Dictionary<Guid, Card>(BatchSize);
            var printingBatch = new List<Printing>(BatchSize);

            await foreach (var json in _parser.ReadCardJsonAsync(source, ct))
            {
                var (card, printing) = ScryfallCardMapper.Map(json, _clock.UtcNow);
                cardBatch[card.OracleId] = card;
                printingBatch.Add(printing);

                // Printings fill faster than cards (many-to-one), so printing count is the correct
                // high-water mark for flushing.
                if (printingBatch.Count >= BatchSize)
                {
                    await FlushBatch(cardBatch.Values.ToList(), printingBatch, counts, ct);
                    cardBatch.Clear();
                    printingBatch.Clear();
                }
            }

            if (printingBatch.Count > 0)
            {
                await FlushBatch(cardBatch.Values.ToList(), printingBatch, counts, ct);
            }

            await _tracker.CompleteAsync(runId, "success", counts.Snapshot(), ct);
        }
        catch (Exception ex)
        {
            await _tracker.FailAsync(runId, ex.Message, ct);
            throw;
        }
    }

    private async Task FlushBatch(
        IReadOnlyList<Card> cards,
        IReadOnlyList<Printing> printings,
        MutableCounts counts,
        CancellationToken ct)
    {
        var cardResult = await _cards.UpsertAsync(cards, ct);
        counts.CardsInserted += cardResult.Inserted;
        counts.CardsUpdated += cardResult.Updated;

        var printingResult = await _printings.UpsertAsync(printings, ct);
        counts.PrintingsInserted += printingResult.Inserted;
        counts.PrintingsUpdated += printingResult.Updated;

        var events = cardResult.Changes.Select(change => new CardOracleEvent
        {
            OracleId = change.OracleId,
            EventType = change.IsNew ? OracleEventType.Created : OracleEventType.Errata,
            PreviousHash = change.PreviousHash,
            NewHash = change.NewHash,
            ObservedAt = _clock.UtcNow,
        }).ToList();

        var emittedCount = await _emitter.EmitAsync(events, ct);
        counts.ErrataEmitted += emittedCount;
    }

    private sealed class MutableCounts
    {
        public int CardsInserted;
        public int CardsUpdated;
        public int PrintingsInserted;
        public int PrintingsUpdated;
        public int ErrataEmitted;

        public IngestCounts Snapshot() => new(CardsInserted, CardsUpdated, PrintingsInserted, PrintingsUpdated, ErrataEmitted);
    }
}
