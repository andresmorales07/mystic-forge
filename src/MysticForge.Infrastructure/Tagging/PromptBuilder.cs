using System.Text;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Tagging;

public sealed class PromptBuilder : IPromptBuilder
{
    private readonly ITaxonomyCache _cache;

    public PromptBuilder(ITaxonomyCache cache) { _cache = cache; }

    public string GetSystemPreamble()
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a Magic: The Gathering card tagger. Given a card's printed text, you produce structured tags consumed by a Commander/EDH deck-recommender pipeline.");
        sb.AppendLine();
        sb.AppendLine("Output four arrays:");
        sb.AppendLine("- `roles`: Tier 1 high-level role labels (closed enum).");
        sb.AppendLine("- `synergy_hook_paths`: Tier 2 deck-archetype hooks (closed list, slash-separated paths).");
        sb.AppendLine("- `mechanics`: Tier 3 named MTG mechanics, ability words, and recurring constructs (open).");
        sb.AppendLine("- `tribal_interest`: creature types this card actively cares about (only when the card creates, tutors, buffs, scales with, or grows by them).");
        sb.AppendLine();
        sb.AppendLine("# Tier 1 — Roles (closed list)");
        AppendRoleDescriptions(sb);
        sb.AppendLine();
        sb.AppendLine("# Tier 2 — Synergy hooks (closed list)");
        AppendHookTree(sb);
        sb.AppendLine();
        sb.AppendLine("# Tier 3 — Mechanics (open list)");
        sb.AppendLine("Emit any named MTG mechanic, ability word, or recurring construct you observe in the rules text. Use lowercase with underscores (e.g. 'flashback', 'cascade', 'partner_with'). Don't invent labels for unique one-off effects.");
        sb.AppendLine();
        sb.AppendLine("# Tribal_interest rule");
        sb.AppendLine("Only populate `tribal_interest` for creature types the card actively cares about — creates tokens of, tutors, buffs, scales with, grows from. Do NOT list a card's own creature subtypes by default. A 1/1 Goblin with no tribal text should not list 'Goblin'.");
        sb.AppendLine();
        sb.AppendLine("# Multi-face rule");
        sb.AppendLine("If the card has multiple faces (`faces` array is populated), consider all faces together and emit a single merged tag set covering the union of effects.");
        sb.AppendLine();
        sb.AppendLine("# Output rule");
        sb.AppendLine("Tag based on rules text, not on flavor text or strategic speculation. If a card has multiple effects, tag each one. If you genuinely don't know what mechanic a recurring construct is called, leave `mechanics` empty for that effect rather than guessing.");
        sb.AppendLine();
        sb.AppendLine("Return JSON matching the response_format schema. All four arrays are required (use [] when nothing applies).");

        return sb.ToString();
    }

    private static void AppendRoleDescriptions(StringBuilder sb)
    {
        WriteRole(sb, Role.Ramp, "produces or accelerates mana (mana rocks, mana dorks, ramp spells)");
        WriteRole(sb, Role.Draw, "replaces itself or generates additional cards");
        WriteRole(sb, Role.Tutor, "searches the library for a specific card or category");
        WriteRole(sb, Role.Removal, "destroys, exiles, bounces, or neutralizes a single permanent");
        WriteRole(sb, Role.Counterspell, "counters a spell on the stack");
        WriteRole(sb, Role.Wipe, "destroys, exiles, or bounces multiple permanents");
        WriteRole(sb, Role.Protection, "prevents damage to or grants hexproof/indestructible/shroud to permanents or players");
        WriteRole(sb, Role.WinCon, "win condition: directly wins the game or telegraphs an alternate win");
        WriteRole(sb, Role.Stax, "global hate piece slowing the game (taxes, restrictions on opponents)");
        WriteRole(sb, Role.LockPiece, "narrow asymmetric lock that shuts down a specific axis");
        WriteRole(sb, Role.Utility, "miscellaneous useful effects not otherwise categorized");
    }

    private static void WriteRole(StringBuilder sb, string role, string description) =>
        sb.AppendLine($"- `{role}` — {description}");

    private void AppendHookTree(StringBuilder sb)
    {
        var hooks = _cache.AllHooks.OrderBy(h => h.Path).ToList();

        // Group by ParentId and handle nullable safely
        var rootHooks = hooks.Where(h => h.ParentId is null).ToList();
        var hooksByParentId = hooks
            .Where(h => h.ParentId is not null)
            .GroupBy(h => h.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var root in rootHooks.OrderBy(h => h.SortOrder).ThenBy(h => h.Name))
        {
            sb.AppendLine($"- `{root.Path}` — {root.Description}");
            WriteChildren(sb, root.Id, hooksByParentId, indent: 1);
        }
    }

    private static void WriteChildren(StringBuilder sb, long parentId, Dictionary<long, List<SynergyHook>> byParent, int indent)
    {
        if (!byParent.TryGetValue(parentId, out var children)) return;
        var prefix = new string(' ', indent * 2);
        foreach (var child in children.OrderBy(h => h.SortOrder).ThenBy(h => h.Name))
        {
            sb.AppendLine($"{prefix}- `{child.Path}` — {child.Description}");
            WriteChildren(sb, child.Id, byParent, indent + 1);
        }
    }
}
