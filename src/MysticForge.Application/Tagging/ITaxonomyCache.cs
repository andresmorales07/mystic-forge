using MysticForge.Domain.Tags;

namespace MysticForge.Application.Tagging;

public interface ITaxonomyCache
{
    /// <summary>Map from full hook path (e.g. 'graveyard_value/reanimate') to hook id.</summary>
    bool TryResolveHook(string path, out long hookId);

    /// <summary>For a leaf hook id, returns the ids of all its ancestors (excluding self).</summary>
    IReadOnlyList<long> AncestorsOf(long hookId);

    /// <summary>True if the role name is in the closed Tier 1 enum.</summary>
    bool IsValidRole(string role);

    /// <summary>The taxonomy_version currently loaded.</summary>
    string CurrentTaxonomyVersion { get; }

    /// <summary>All hooks loaded into the cache (used by PromptBuilder).</summary>
    IReadOnlyList<SynergyHook> AllHooks { get; }

    /// <summary>Reload the cache from the database.</summary>
    Task ReloadAsync(CancellationToken ct);
}
