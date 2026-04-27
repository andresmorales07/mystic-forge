namespace MysticForge.Application.Tagging;

public interface IOpenRouterTaggingClient
{
    Task<RawTagSet> TagAsync(CardForTagging card, CancellationToken ct);
    string CurrentModelVersion { get; }
}
