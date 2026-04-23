using System.Text.Json.Serialization;

namespace MysticForge.Infrastructure.Scryfall;

internal sealed record ScryfallCardJson(
    [property: JsonPropertyName("id")]               Guid Id,
    [property: JsonPropertyName("oracle_id")]        Guid OracleId,
    [property: JsonPropertyName("name")]             string Name,
    [property: JsonPropertyName("layout")]           string Layout,
    [property: JsonPropertyName("oracle_text")]      string? OracleText,
    [property: JsonPropertyName("type_line")]        string? TypeLine,
    [property: JsonPropertyName("mana_cost")]        string? ManaCost,
    [property: JsonPropertyName("card_faces")]       List<ScryfallFaceJson>? CardFaces,
    [property: JsonPropertyName("cmc")]              decimal? Cmc,
    [property: JsonPropertyName("colors")]           List<string>? Colors,
    [property: JsonPropertyName("color_identity")]   List<string> ColorIdentity,
    [property: JsonPropertyName("keywords")]         List<string>? Keywords,
    [property: JsonPropertyName("set")]              string SetCode,
    [property: JsonPropertyName("collector_number")] string CollectorNumber,
    [property: JsonPropertyName("rarity")]           string Rarity,
    [property: JsonPropertyName("prices")]           ScryfallPricesJson? Prices,
    [property: JsonPropertyName("image_uris")]       ScryfallImageUrisJson? ImageUris,
    [property: JsonPropertyName("scryfall_uri")]     string? ScryfallUri,
    [property: JsonPropertyName("released_at")]      string? ReleasedAt);

internal sealed record ScryfallFaceJson(
    [property: JsonPropertyName("name")]         string Name,
    [property: JsonPropertyName("oracle_text")]  string? OracleText,
    [property: JsonPropertyName("type_line")]    string? TypeLine,
    [property: JsonPropertyName("mana_cost")]    string? ManaCost);

internal sealed record ScryfallPricesJson(
    [property: JsonPropertyName("usd")]          string? Usd,
    [property: JsonPropertyName("usd_foil")]     string? UsdFoil,
    [property: JsonPropertyName("usd_etched")]   string? UsdEtched,
    [property: JsonPropertyName("eur")]          string? Eur,
    [property: JsonPropertyName("eur_foil")]     string? EurFoil,
    [property: JsonPropertyName("tix")]          string? Tix);

internal sealed record ScryfallImageUrisJson(
    [property: JsonPropertyName("normal")]       string? Normal,
    [property: JsonPropertyName("small")]        string? Small);
