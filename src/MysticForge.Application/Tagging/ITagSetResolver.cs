namespace MysticForge.Application.Tagging;

public interface ITagSetResolver
{
    Task<ResolvedTagSet> ResolveAsync(
        Guid oracleId,
        RawTagSet raw,
        string modelVersion,
        CancellationToken ct);
}
