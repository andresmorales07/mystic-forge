using MysticForge.Application.Tagging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MysticForge.Infrastructure.Seeding;

/// <summary>
/// Parses the taxonomy-v1.yaml flat adjacency-list format into a <see cref="TaxonomyDocument"/>.
/// The YAML shape uses a top-level <c>version</c> key and a <c>synergy_hooks</c> list where each
/// entry carries a <c>parent</c> field (null for roots) rather than nested children arrays.
/// </summary>
public sealed class TaxonomyV1YamlParser : ITaxonomyV1YamlParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public TaxonomyDocument Parse(string yaml)
    {
        var raw = Deserializer.Deserialize<RawDoc>(yaml)
            ?? throw new InvalidOperationException("Empty taxonomy YAML.");

        var rawHooks = raw.SynergyHooks ?? Array.Empty<RawHookNode>();

        // Build a lookup of name → materialized path for parent resolution.
        // The YAML lists nodes in topological order (parents before children),
        // so a single pass is sufficient.
        var pathByName = new Dictionary<string, string>(StringComparer.Ordinal);
        var hooks = new List<HookNode>(rawHooks.Count);

        foreach (var node in rawHooks)
        {
            if (string.IsNullOrWhiteSpace(node.Name))
                throw new InvalidOperationException($"A synergy_hooks entry has a missing or blank name.");

            string? parentPath = null;
            if (!string.IsNullOrWhiteSpace(node.Parent))
            {
                if (!pathByName.TryGetValue(node.Parent, out parentPath))
                    throw new InvalidOperationException(
                        $"Hook '{node.Name}' references unknown parent '{node.Parent}'. " +
                        "Ensure parents appear before children in synergy_hooks.");
            }

            var path = parentPath is null ? node.Name : $"{parentPath}/{node.Name}";
            var depth = (short)(path.Count(c => c == '/') + 1);

            hooks.Add(new HookNode(
                Path: path,
                Name: node.Name,
                ParentPath: parentPath,
                Depth: depth,
                Description: node.Description ?? string.Empty,
                SortOrder: node.SortOrder ?? 0));

            pathByName[node.Name] = path;
        }

        return new TaxonomyDocument(raw.Version ?? "unspecified", hooks);
    }

    // -------------------------------------------------------------------------
    // Private DTOs — mapped from underscored YAML keys by YamlDotNet.
    // -------------------------------------------------------------------------

    private sealed class RawDoc
    {
        public string? Version { get; set; }
        public IList<RawHookNode> SynergyHooks { get; set; } = [];
    }

    private sealed class RawHookNode
    {
        public string? Name { get; set; }
        public string? Parent { get; set; }
        public string? Description { get; set; }
        public int? SortOrder { get; set; }
    }
}
