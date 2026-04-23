using System.Globalization;
using System.Text.Json;
using MysticForge.Domain.Cards;

namespace MysticForge.Application.Scryfall;

public static class ScryfallCardMapper
{
    public static (Card Card, Printing Printing) Map(string scryfallJson, DateTimeOffset now)
    {
        var json = JsonSerializer.Deserialize<ScryfallCardJson>(scryfallJson)
            ?? throw new InvalidOperationException("Scryfall payload deserialized to null.");
        return Map(json, now);
    }

    internal static (Card Card, Printing Printing) Map(ScryfallCardJson json, DateTimeOffset now)
    {
        var faces = json.CardFaces?
            .Select(f => new CardFace(f.Name, f.OracleText, f.TypeLine, NormalizeEmptyMana(f.ManaCost)))
            .ToList();

        var isMultiFace = faces is { Count: > 0 };

        var hash = isMultiFace
            ? OracleHasher.HashMultiFace(faces!)
            : OracleHasher.HashSingleFace(json.OracleText ?? string.Empty);

        var card = new Card
        {
            OracleId = json.OracleId,
            Name = json.Name,
            Layout = json.Layout,
            OracleText = isMultiFace ? null : json.OracleText,
            TypeLine = isMultiFace ? null : json.TypeLine,
            ManaCost = isMultiFace ? null : NormalizeEmptyMana(json.ManaCost),
            Faces = isMultiFace ? faces : null,
            Cmc = json.Cmc,
            Colors = json.Colors ?? [],
            ColorIdentity = json.ColorIdentity,
            Keywords = json.Keywords ?? [],
            OracleHash = hash,
            LastOracleChange = now,
        };

        card.EnsureFaceInvariant();

        var printing = new Printing
        {
            ScryfallId = json.Id,
            OracleId = json.OracleId,
            SetCode = json.SetCode,
            CollectorNumber = json.CollectorNumber,
            Rarity = json.Rarity,
            PriceUsd       = ParseDecimal(json.Prices?.Usd),
            PriceUsdFoil   = ParseDecimal(json.Prices?.UsdFoil),
            PriceUsdEtched = ParseDecimal(json.Prices?.UsdEtched),
            PriceEur       = ParseDecimal(json.Prices?.Eur),
            PriceEurFoil   = ParseDecimal(json.Prices?.EurFoil),
            PriceTix       = ParseDecimal(json.Prices?.Tix),
            ImageUriNormal = json.ImageUris?.Normal,
            ImageUriSmall  = json.ImageUris?.Small,
            ScryfallUri    = json.ScryfallUri,
            ReleasedAt     = ParseDate(json.ReleasedAt),
            LastPriceUpdate = now,
        };

        return (card, printing);
    }

    private static string? NormalizeEmptyMana(string? manaCost)
        => string.IsNullOrEmpty(manaCost) ? null : manaCost;

    private static decimal? ParseDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
}
