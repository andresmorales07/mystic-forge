using System.Reflection;
using FluentAssertions;
using MysticForge.CommanderSpellbook.Generated.Models;
using MysticForge.Infrastructure.Spellbook.Mapping;

namespace MysticForge.UnitTests.Spellbook;

public sealed class RawComboMapperTests
{
    // -------------------------------------------------------------------------
    // Variant → RawCombo
    // -------------------------------------------------------------------------

    [Fact]
    public void Maps_minimal_variant_with_id_and_identity_only()
    {
        var v = new Variant { Id = "abc-123", Identity = ColorEnum.WUBRG };

        var result = RawComboMapper.ToRawCombo(v);

        result.Id.Should().Be("abc-123");
        result.Identity.Should().Be("WUBRG");
        result.Cards.Should().BeEmpty();
        result.FeatureIds.Should().BeEmpty();
        result.TemplateRefs.Should().BeEmpty();
        result.Spoiler.Should().BeFalse();
        result.LegalitiesJson.Should().BeNull();
    }

    [Fact]
    public void Maps_variant_with_cards_features_templates()
    {
        var card = new Card { Name = "Sol Ring" };
        var civ  = new CardInVariant { Quantity = 1 };
        Sp(civ, "Card",            card);
        Sp(civ, "MustBeCommander", false);
        Sp(civ, "ZoneLocations",   new List<string> { "Hand" });

        var feature = new Feature { Name = "Infinite Mana", Status = FeatureStatusEnum.S, Uncountable = false };
        Sp(feature, "Id", 42);

        var fpbv = new FeatureProducedByVariant { Quantity = 1 };
        Sp(fpbv, "Feature", feature);

        var template = new Template { Name = "Any artifact", ScryfallQuery = "t:artifact" };
        Sp(template, "Id", 7);

        var tiv = new TemplateInVariant { Quantity = 2 };
        Sp(tiv, "Template", template);

        var v = new Variant { Id = "v-1", Identity = ColorEnum.C, Status = VariantStatusEnum.OK, BracketTag = BracketTagEnum.C, Spoiler = false };
        Sp(v, "Popularity", 99);
        Sp(v, "Uses",       new List<CardInVariant>              { civ  });
        Sp(v, "Produces",   new List<FeatureProducedByVariant>   { fpbv });
        Sp(v, "Requires",   new List<TemplateInVariant>          { tiv  });

        var result = RawComboMapper.ToRawCombo(v);

        result.Id.Should().Be("v-1");
        result.Identity.Should().Be("C");
        result.Status.Should().Be("OK");
        result.BracketTag.Should().Be("C");
        result.Popularity.Should().Be(99);

        result.Cards.Should().HaveCount(1);
        result.Cards[0].Position.Should().Be(1);
        result.Cards[0].CardName.Should().Be("Sol Ring");
        result.Cards[0].Quantity.Should().Be(1);
        result.Cards[0].MustBeCommander.Should().BeFalse();
        result.Cards[0].ZoneLocations.Should().Be("Hand");

        result.FeatureIds.Should().ContainSingle().Which.Should().Be(42L);

        result.TemplateRefs.Should().HaveCount(1);
        result.TemplateRefs[0].TemplateId.Should().Be(7L);
        result.TemplateRefs[0].Quantity.Should().Be(2);
    }

    [Fact]
    public void Throws_on_missing_variant_id()
    {
        var v = new Variant { Id = null };

        var act = () => RawComboMapper.ToRawCombo(v);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*variant missing id*");
    }

    [Fact]
    public void Serializes_legalities_to_json_when_present()
    {
        var legalities = new VariantLegalities { Commander = true, Pauper = false };

        var v = new Variant { Id = "x" };
        Sp(v, "Legalities", legalities);

        var result = RawComboMapper.ToRawCombo(v);

        result.LegalitiesJson.Should().NotBeNullOrEmpty();
        result.LegalitiesJson.Should().Contain("Commander");
    }

    // -------------------------------------------------------------------------
    // Feature → RawFeature
    // -------------------------------------------------------------------------

    [Fact]
    public void Maps_feature_with_id_name_status_uncountable()
    {
        var f = new Feature { Name = "Infinite Mana", Status = FeatureStatusEnum.S, Uncountable = true };
        Sp(f, "Id", 5);

        var result = RawComboMapper.ToRawFeature(f);

        result.Id.Should().Be(5L);
        result.Name.Should().Be("Infinite Mana");
        result.Status.Should().Be("S");
        result.Uncountable.Should().BeTrue();
    }

    [Fact]
    public void ToRawFeature_throws_when_id_is_null()
    {
        var f = new Feature { Name = "missing" };
        // Id stays null (private set, default null)

        var act = () => RawComboMapper.ToRawFeature(f);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*feature missing id*");
    }

    // -------------------------------------------------------------------------
    // Template → RawTemplate
    // -------------------------------------------------------------------------

    [Fact]
    public void Maps_template_with_query_and_api()
    {
        var t = new Template { Name = "Any artifact", ScryfallQuery = "t:artifact" };
        Sp(t, "Id",          3);
        Sp(t, "ScryfallApi", "https://api.scryfall.com/cards/search?q=t%3Aartifact");

        var result = RawComboMapper.ToRawTemplate(t);

        result.Id.Should().Be(3L);
        result.Name.Should().Be("Any artifact");
        result.ScryfallQuery.Should().Be("t:artifact");
        result.ScryfallApi.Should().Be("https://api.scryfall.com/cards/search?q=t%3Aartifact");
    }

    [Fact]
    public void ToRawTemplate_throws_when_id_is_null()
    {
        var t = new Template { Name = "missing" };
        // Id stays null

        var act = () => RawComboMapper.ToRawTemplate(t);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*template missing id*");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Sets a property with a private setter via reflection — required to build Kiota DTOs in tests.</summary>
    private static void Sp(object obj, string propertyName, object? value)
    {
        var prop = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMemberException(obj.GetType().Name, propertyName);
        prop.SetValue(obj, value);
    }
}
