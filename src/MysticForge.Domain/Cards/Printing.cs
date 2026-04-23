namespace MysticForge.Domain.Cards;

public sealed class Printing
{
    public required Guid ScryfallId { get; init; }
    public required Guid OracleId { get; init; }
    public required string SetCode { get; init; }
    public required string CollectorNumber { get; init; }
    public required string Rarity { get; init; }

    public decimal? PriceUsd { get; init; }
    public decimal? PriceUsdFoil { get; init; }
    public decimal? PriceUsdEtched { get; init; }
    public decimal? PriceEur { get; init; }
    public decimal? PriceEurFoil { get; init; }
    public decimal? PriceTix { get; init; }

    public string? ImageUriNormal { get; init; }
    public string? ImageUriSmall { get; init; }
    public string? ScryfallUri { get; init; }
    public DateOnly? ReleasedAt { get; init; }

    public required DateTimeOffset LastPriceUpdate { get; init; }
}
