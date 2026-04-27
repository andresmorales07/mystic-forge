namespace MysticForge.Domain.Tags;

public sealed class TagFailure
{
    public long Id { get; init; }
    public required Guid OracleId { get; init; }
    public required long EventId { get; init; }
    public required string ErrorKind { get; init; }       // 'schema_violation' | 'http_error' | 'unknown_tag' | 'other'
    public required string ErrorMessage { get; init; }
    public required short Attempts { get; init; }
    public required string ModelVersion { get; init; }
    public required DateTimeOffset FailedAt { get; init; }
}
