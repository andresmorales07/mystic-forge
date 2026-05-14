using System.Text.Json;
using MysticForge.Application.Spellbook;
using WireMock.RequestBuilders;
using WireMock.Server;
using WmResponse = WireMock.ResponseBuilders.Response;

namespace MysticForge.IntegrationTests.Spellbook;

/// <summary>
/// Registers WireMock stubs for the three Spellbook endpoints using inline C# fixture data.
/// Canonical fixture set covers two features, one template, and three variants.
/// </summary>
internal static class SpellbookFixturePlayer
{
    // -------------------------------------------------------------------------
    // Canonical fixture data
    // -------------------------------------------------------------------------

    public static readonly IReadOnlyList<RawFeature> Features =
    [
        new RawFeature(1L, "Infinite Mana",  "PU", Uncountable: false),
        new RawFeature(2L, "Win the Game",   "PU", Uncountable: false),
    ];

    public static readonly IReadOnlyList<RawTemplate> Templates =
    [
        new RawTemplate(1L, "Any haste enabler", "t:creature mv<=3 o:haste",
            "https://api.scryfall.com/cards/search?q=t%3Acreature+mv%3C%3D3+o%3Ahaste"),
    ];

    public static readonly IReadOnlyList<RawCombo> Variants =
    [
        new RawCombo(
            Id:                 "v-bloodghast-loop",
            Identity:           "BR",
            ManaNeeded:         null,
            ManaValueNeeded:    null,
            OtherPrerequisites: null,
            Description:        "Reanimate Bloodghast each turn for damage",
            Notes:              null,
            Status:             "OK",
            Spoiler:            false,
            LegalitiesJson:     null,
            BracketTag:         null,
            Popularity:         null,
            Cards:
            [
                new RawComboCard(1, "Bloodghast",      1, false, null),
                new RawComboCard(2, "Phyrexian Altar", 1, false, null),
            ],
            FeatureIds:   [1L, 2L],
            TemplateRefs: []),
        new RawCombo(
            Id:                 "v-thopter-foundry",
            Identity:           "WUB",
            ManaNeeded:         null,
            ManaValueNeeded:    null,
            OtherPrerequisites: null,
            Description:        null,
            Notes:              null,
            Status:             "OK",
            Spoiler:            false,
            LegalitiesJson:     null,
            BracketTag:         null,
            Popularity:         null,
            Cards:
            [
                new RawComboCard(1, "Thopter Foundry",      1, false, null),
                new RawComboCard(2, "Sword of the Meek",    1, false, null),
                new RawComboCard(3, "Krark-Clan Ironworks", 1, false, null),
            ],
            FeatureIds:   [1L],
            TemplateRefs: []),
        new RawCombo(
            Id:                 "v-isochron-dramatic",
            Identity:           "U",
            ManaNeeded:         null,
            ManaValueNeeded:    null,
            OtherPrerequisites: null,
            Description:        null,
            Notes:              null,
            Status:             "OK",
            Spoiler:            false,
            LegalitiesJson:     null,
            BracketTag:         null,
            Popularity:         null,
            Cards:
            [
                new RawComboCard(1, "Isochron Scepter",  1, false, null),
                new RawComboCard(2, "Dramatic Reversal", 1, false, null),
            ],
            FeatureIds:   [1L],
            TemplateRefs: []),
    ];

    // -------------------------------------------------------------------------
    // WireMock registration
    // -------------------------------------------------------------------------

    /// <summary>Registers all three endpoints using the full canonical fixture set.</summary>
    public static void RegisterAll(WireMockServer wm)
    {
        RegisterFeatures(wm, Features);
        RegisterTemplates(wm, Templates);
        RegisterVariants(wm, Variants);
    }

    /// <summary>Registers the variants endpoint with a custom subset (for soft-mark / re-emergence tests).</summary>
    public static void RegisterVariantsSubset(WireMockServer wm, IReadOnlyList<RawCombo> variants)
    {
        wm.ResetMappings();
        RegisterFeatures(wm, Features);
        RegisterTemplates(wm, Templates);
        RegisterVariants(wm, variants);
    }

    // -------------------------------------------------------------------------
    // Per-endpoint helpers
    // -------------------------------------------------------------------------

    public static void RegisterFeatures(WireMockServer wm, IReadOnlyList<RawFeature> features)
    {
        var body = JsonSerializer.Serialize(new
        {
            count    = features.Count,
            next     = (string?)null,
            previous = (string?)null,
            results  = features.Select(f => new
            {
                id          = f.Id,
                name        = f.Name,
                status      = f.Status,
                uncountable = f.Uncountable,
            }),
        });

        wm.Given(Request.Create().WithPath("/features/").WithParam("offset", "0").UsingGet())
          .RespondWith(WmResponse.Create().WithStatusCode(200)
              .WithHeader("Content-Type", "application/json")
              .WithBody(body));
    }

    public static void RegisterTemplates(WireMockServer wm, IReadOnlyList<RawTemplate> templates)
    {
        var body = JsonSerializer.Serialize(new
        {
            count    = templates.Count,
            next     = (string?)null,
            previous = (string?)null,
            results  = templates.Select(t => new
            {
                id            = t.Id,
                name          = t.Name,
                scryfallQuery = t.ScryfallQuery,
            }),
        });

        wm.Given(Request.Create().WithPath("/templates/").WithParam("offset", "0").UsingGet())
          .RespondWith(WmResponse.Create().WithStatusCode(200)
              .WithHeader("Content-Type", "application/json")
              .WithBody(body));
    }

    public static void RegisterVariants(WireMockServer wm, IReadOnlyList<RawCombo> variants)
    {
        var body = JsonSerializer.Serialize(new
        {
            count    = variants.Count,
            next     = (string?)null,
            previous = (string?)null,
            results  = variants.Select(v => new
            {
                id       = v.Id,
                identity = v.Identity,
                status   = v.Status,
                spoiler  = v.Spoiler,
                uses     = v.Cards.Select(c => new
                {
                    card             = new { name = c.CardName },
                    quantity         = (int)c.Quantity,
                    mustBeCommander  = c.MustBeCommander,
                    zoneLocations    = (string?)null,
                }).ToArray(),
                produces            = v.FeatureIds.Select(fid => new { feature = new { id = fid } }).ToArray(),
                requires            = v.TemplateRefs.Select(r => new
                {
                    template = new { id = r.TemplateId },
                    quantity = (int)r.Quantity,
                }).ToArray(),
                description          = v.Description,
                notes                = v.Notes,
                manaNeeded           = v.ManaNeeded,
                manaValueNeeded      = v.ManaValueNeeded,
                easyPrerequisites    = v.OtherPrerequisites,
                popularity           = v.Popularity,
            }),
        });

        wm.Given(Request.Create().WithPath("/variants/").WithParam("offset", "0").UsingGet())
          .RespondWith(WmResponse.Create().WithStatusCode(200)
              .WithHeader("Content-Type", "application/json")
              .WithBody(body));
    }

    public static void RegisterVariants500(WireMockServer wm)
    {
        wm.ResetMappings();
        RegisterFeatures(wm, Features);
        RegisterTemplates(wm, Templates);
        wm.Given(Request.Create().WithPath("/variants/").UsingGet())
          .RespondWith(WmResponse.Create().WithStatusCode(500));
    }
}
