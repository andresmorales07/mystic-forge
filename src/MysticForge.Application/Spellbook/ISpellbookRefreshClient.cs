namespace MysticForge.Application.Spellbook;

public interface ISpellbookRefreshClient
{
    /// <summary>Yields features page-by-page. Each item is one successfully-fetched page.
    /// Failed pages throw <see cref="SpellbookPageException"/> — caller logs and decides whether to keep going.</summary>
    IAsyncEnumerable<IReadOnlyList<RawFeature>>  StreamFeaturesAsync (CancellationToken ct);
    IAsyncEnumerable<IReadOnlyList<RawTemplate>> StreamTemplatesAsync(CancellationToken ct);
    IAsyncEnumerable<IReadOnlyList<RawCombo>>    StreamVariantsAsync (CancellationToken ct);
}

public sealed class SpellbookPageException : Exception
{
    public SpellbookPageException(string message, Exception? inner = null) : base(message, inner) { }
}
