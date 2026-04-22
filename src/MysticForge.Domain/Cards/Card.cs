namespace MysticForge.Domain.Cards;

public sealed class Card
{
    public required Guid OracleId { get; init; }
    public required string Name { get; init; }
    public required string Layout { get; init; }

    // Single-face path (exactly one of these two paths is populated)
    public string? OracleText { get; init; }
    public string? TypeLine { get; init; }
    public string? ManaCost { get; init; }

    // Multi-face path
    public IReadOnlyList<CardFace>? Faces { get; init; }

    public decimal? Cmc { get; init; }
    public IReadOnlyList<string> Colors { get; init; } = [];
    public required IReadOnlyList<string> ColorIdentity { get; init; }
    public IReadOnlyList<string> Keywords { get; init; } = [];

    public required byte[] OracleHash { get; init; }
    public required DateTimeOffset LastOracleChange { get; init; }

    public bool IsMultiFaced => Faces is { Count: > 0 };

    public void EnsureFaceInvariant()
    {
        if (IsMultiFaced)
        {
            if (OracleText is not null || TypeLine is not null || ManaCost is not null)
            {
                throw new InvalidOperationException(
                    $"Multi-face card '{Name}' ({OracleId}) must not carry root oracle_text/type_line/mana_cost.");
            }
        }
        else
        {
            if (OracleText is null || TypeLine is null)
            {
                throw new InvalidOperationException(
                    $"Single-face card '{Name}' ({OracleId}) must carry oracle_text and type_line.");
            }
        }
    }
}
