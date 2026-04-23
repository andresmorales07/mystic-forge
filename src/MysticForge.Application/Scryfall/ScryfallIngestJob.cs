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

            var cardBatch = new List<Card>(BatchSize);
            var printingBatch = new List<Printing>(BatchSize);

            await foreach (var json in _parser.ReadCardJsonAsync(source, ct))
            {
                var (card, printing) = ScryfallCardMapper.Map(json, _clock.UtcNow);
                cardBatch.Add(card);
                printingBatch.Add(printing);

                if (cardBatch.Count >= BatchSize)
                {
                    await FlushBatch(cardBatch, printingBatch, counts, ct);
                    cardBatch.Clear();
                    printingBatch.Clear();
                }
            }

            if (cardBatch.Count > 0)
            {
                await FlushBatch(cardBatch, printingBatch, counts, ct);
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
        List<Card> cards,
        List<Printing> printings,
        MutableCounts counts,
        CancellationToken ct)
    {
        var cardResult = await _cards.UpsertAsync(cards, ct);
        counts.CardsInserted += cardResult.Inserted;
        counts.CardsUpdated  += cardResult.Updated;

        var printingResult = await _printings.UpsertAsync(printings, ct);
        counts.PrintingsInserted += printingResult.Inserted;
        counts.PrintingsUpdated  += printingResult.Updated;

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
