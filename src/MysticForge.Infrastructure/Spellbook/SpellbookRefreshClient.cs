using Microsoft.Extensions.Options;
using Microsoft.Kiota.Http.HttpClientLibrary;
using MysticForge.Application.Spellbook;
using MysticForge.CommanderSpellbook.Generated;
using MysticForge.Infrastructure.Spellbook.Mapping;

namespace MysticForge.Infrastructure.Spellbook;

/// <summary>
/// Streams features, templates and variants from the Commander Spellbook API via the Kiota client,
/// yielding one page at a time. Polly retry is applied on the HttpClient; this class only wraps
/// terminal failures in <see cref="SpellbookPageException"/>.
/// </summary>
public sealed class SpellbookRefreshClient : ISpellbookRefreshClient
{
    private readonly SpellbookApiClient        _client;
    private readonly CommanderSpellbookOptions _opts;

    public SpellbookRefreshClient(SpellbookApiClient client, IOptions<CommanderSpellbookOptions> opts)
    {
        _client = client;
        _opts   = opts.Value;
    }

    // ---------------------------------------------------------------------------
    // ISpellbookRefreshClient
    // ---------------------------------------------------------------------------

    public async IAsyncEnumerable<IReadOnlyList<RawFeature>> StreamFeaturesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var offset = 0;
        while (true)
        {
            IReadOnlyList<RawFeature> page;
            string? next;
            try
            {
                var result = await _client.Features.EmptyPathSegment.GetAsync(q =>
                {
                    q.QueryParameters.Limit  = _opts.PageSize;
                    q.QueryParameters.Offset = offset;
                }, cancellationToken: ct).ConfigureAwait(false);

                if (result?.Results is not { Count: > 0 })
                    yield break;

                page = result.Results.Select(RawComboMapper.ToRawFeature).ToList();
                next = result.Next;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SpellbookPageException(
                    $"Failed to fetch features page at offset {offset}", ex);
            }

            yield return page;

            if (string.IsNullOrEmpty(next))
                yield break;

            offset += _opts.PageSize;
            await DelayIfNeeded(ct).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<IReadOnlyList<RawTemplate>> StreamTemplatesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var offset = 0;
        while (true)
        {
            IReadOnlyList<RawTemplate> page;
            string? next;
            try
            {
                var result = await _client.Templates.EmptyPathSegment.GetAsync(q =>
                {
                    q.QueryParameters.Limit  = _opts.PageSize;
                    q.QueryParameters.Offset = offset;
                }, cancellationToken: ct).ConfigureAwait(false);

                if (result?.Results is not { Count: > 0 })
                    yield break;

                page = result.Results.Select(RawComboMapper.ToRawTemplate).ToList();
                next = result.Next;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SpellbookPageException(
                    $"Failed to fetch templates page at offset {offset}", ex);
            }

            yield return page;

            if (string.IsNullOrEmpty(next))
                yield break;

            offset += _opts.PageSize;
            await DelayIfNeeded(ct).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<IReadOnlyList<RawCombo>> StreamVariantsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var offset = 0;
        while (true)
        {
            IReadOnlyList<RawCombo> page;
            string? next;
            try
            {
                var result = await _client.Variants.EmptyPathSegment.GetAsync(q =>
                {
                    q.QueryParameters.Limit  = _opts.PageSize;
                    q.QueryParameters.Offset = offset;
                }, cancellationToken: ct).ConfigureAwait(false);

                if (result?.Results is not { Count: > 0 })
                    yield break;

                page = result.Results.Select(RawComboMapper.ToRawCombo).ToList();
                next = result.Next;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SpellbookPageException(
                    $"Failed to fetch variants page at offset {offset}", ex);
            }

            yield return page;

            if (string.IsNullOrEmpty(next))
                yield break;

            offset += _opts.PageSize;
            await DelayIfNeeded(ct).ConfigureAwait(false);
        }
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private Task DelayIfNeeded(CancellationToken ct) =>
        _opts.PerPageDelayMs > 0
            ? Task.Delay(_opts.PerPageDelayMs, ct)
            : Task.CompletedTask;
}
