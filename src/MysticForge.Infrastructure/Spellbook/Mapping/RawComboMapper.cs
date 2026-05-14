using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using MysticForge.Application.Spellbook;
using KiotaModels = MysticForge.CommanderSpellbook.Generated.Models;

namespace MysticForge.Infrastructure.Spellbook.Mapping;

/// <summary>
/// Translates Kiota-generated DTOs from the Commander Spellbook API into Application-layer DTOs.
/// All methods are pure functions — no I/O, no DI, safe to call from any context.
/// </summary>
public static class RawComboMapper
{
    // ---------------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------------

    public static RawCombo ToRawCombo(KiotaModels.Variant v)
    {
        if (v.Id is null)
            throw new InvalidOperationException("variant missing id");

        var cards = v.Uses is null
            ? []
            : v.Uses
                .Select((civ, i) => MapCard(civ, i))
                .ToList();

        var featureIds = v.Produces is null
            ? []
            : v.Produces
                .Select(p => p.Feature?.Id)
                .Where(id => id.HasValue)
                .Select(id => (long)id!.Value)
                .ToList();

        var templateRefs = v.Requires is null
            ? []
            : v.Requires
                .Where(t => t.Template?.Id is not null)
                .Select(t => new RawComboTemplateRef(
                    TemplateId: (long)t.Template!.Id!.Value,
                    Quantity:   (short)(t.Quantity ?? 1)))
                .ToList();

        var legalitiesJson = v.Legalities is not null
            ? JsonSerializer.Serialize(v.Legalities)
            : null;

        return new RawCombo(
            Id:                 v.Id,
            Identity:           EnumMemberValue(v.Identity) ?? string.Empty,
            ManaNeeded:         v.ManaNeeded,
            ManaValueNeeded:    v.ManaValueNeeded.HasValue ? (decimal)v.ManaValueNeeded.Value : null,
            OtherPrerequisites: v.EasyPrerequisites,
            Description:        v.Description,
            Notes:              v.Notes,
            Status:             EnumMemberValue(v.Status) ?? "N",
            Spoiler:            v.Spoiler ?? false,
            LegalitiesJson:     legalitiesJson,
            BracketTag:         EnumMemberValue(v.BracketTag),
            Popularity:         v.Popularity,
            Cards:              cards,
            FeatureIds:         featureIds,
            TemplateRefs:       templateRefs);
    }

    public static RawFeature ToRawFeature(KiotaModels.Feature f)
    {
        if (f.Id is null)
            throw new InvalidOperationException("feature missing id");

        return new RawFeature(
            Id:          (long)f.Id.Value,
            Name:        f.Name ?? string.Empty,
            Status:      EnumMemberValue(f.Status) ?? string.Empty,
            Uncountable: f.Uncountable ?? false);
    }

    public static RawTemplate ToRawTemplate(KiotaModels.Template t)
    {
        if (t.Id is null)
            throw new InvalidOperationException("template missing id");

        return new RawTemplate(
            Id:            (long)t.Id.Value,
            Name:          t.Name ?? string.Empty,
            ScryfallQuery: t.ScryfallQuery,
            ScryfallApi:   t.ScryfallApi);
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static RawComboCard MapCard(KiotaModels.CardInVariant civ, int index) =>
        new(
            Position:        (short)(index + 1),
            CardName:        civ.Card?.Name ?? string.Empty,
            Quantity:        (short)(civ.Quantity ?? 1),
            MustBeCommander: civ.MustBeCommander ?? false,
            ZoneLocations:   civ.ZoneLocations is { Count: > 0 }
                                 ? string.Join(",", civ.ZoneLocations)
                                 : null);

    /// <summary>
    /// Returns the <see cref="EnumMemberAttribute.Value"/> for the enum value,
    /// or <c>null</c> when <paramref name="value"/> is null.
    /// Falls back to <see cref="Enum.ToString()"/> if no attribute is present.
    /// </summary>
    private static string? EnumMemberValue<T>(T? value) where T : struct, Enum
    {
        if (value is null) return null;

        var name = value.ToString()!;
        var field = typeof(T).GetField(name);
        if (field is null) return name;

        var attr = field.GetCustomAttribute<EnumMemberAttribute>();
        return attr?.Value ?? name;
    }
}
