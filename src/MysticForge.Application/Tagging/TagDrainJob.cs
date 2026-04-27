using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MysticForge.Application.Tagging;

public sealed class TagDrainJob
{
    private readonly IOutboxClaimer _claimer;
    private readonly ICardReader _cards;
    private readonly IOpenRouterTaggingClient _llm;
    private readonly ITagSetResolver _resolver;
    private readonly ITagWriter _writer;
    private readonly ITaggingFailureLogger _failures;
    private readonly TagDrainOptions _options;
    private readonly ILogger<TagDrainJob> _log;

    public TagDrainJob(
        IOutboxClaimer claimer,
        ICardReader cards,
        IOpenRouterTaggingClient llm,
        ITagSetResolver resolver,
        ITagWriter writer,
        ITaggingFailureLogger failures,
        IOptions<TagDrainOptions> options,
        ILogger<TagDrainJob> log)
    {
        _claimer = claimer;
        _cards = cards;
        _llm = llm;
        _resolver = resolver;
        _writer = writer;
        _failures = failures;
        _options = options.Value;
        _log = log;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var instanceId = $"{Environment.MachineName}:{Environment.ProcessId}";
        var claimed = await _claimer.ClaimBatchAsync(instanceId, _options.BatchSize, ct);
        if (claimed.Count == 0)
        {
            _log.LogDebug("Drain tick: no events claimed.");
            return;
        }

        _log.LogInformation("Drain tick: claimed {Count} events.", claimed.Count);

        var succeeded = 0;
        var failed = 0;

        await Parallel.ForEachAsync(
            claimed,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxConcurrency,
                CancellationToken = ct,
            },
            async (evt, token) =>
            {
                try
                {
                    await ProcessEventAsync(evt, token);
                    Interlocked.Increment(ref succeeded);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    _log.LogWarning(ex,
                        "Tag failed for oracle_id={OracleId} event_id={EventId} attempts={Attempts}.",
                        evt.OracleId, evt.EventId, evt.ClaimAttempts);
                    await _failures.LogAsync(evt, ex, _llm.CurrentModelVersion, token);
                    if (evt.ClaimAttempts >= 5)
                    {
                        await _failures.MarkConsumedAsync(evt, token);
                    }
                }
            });

        _log.LogInformation("Drain tick complete: {Succeeded} succeeded, {Failed} failed.", succeeded, failed);
    }

    private async Task ProcessEventAsync(ClaimedEvent evt, CancellationToken ct)
    {
        var card = await _cards.GetByOracleIdAsync(evt.OracleId, ct)
            ?? throw new InvalidOperationException($"Card {evt.OracleId} not found for tagging.");
        var raw = await _llm.TagAsync(card, ct);
        var resolved = await _resolver.ResolveAsync(evt.OracleId, raw, _llm.CurrentModelVersion, ct);
        await _writer.WriteAsync(evt, resolved, ct);
    }
}

public sealed class TagDrainOptions
{
    public const string SectionName = "Tagging";

    public int BatchSize { get; init; } = 100;
    public int MaxConcurrency { get; init; } = 10;
    public TimeSpan DrainInterval { get; init; } = TimeSpan.FromSeconds(30);
    public int ClaimExpirySeconds { get; init; } = 600;
    public int MaxClaimAttempts { get; init; } = 5;
    public int RequestTimeoutSeconds { get; init; } = 60;
}
