using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence;

public sealed class TaggingFailureLogger : ITaggingFailureLogger
{
    private readonly IDbContextFactory<MysticForgeDbContext> _factory;

    public TaggingFailureLogger(IDbContextFactory<MysticForgeDbContext> factory) { _factory = factory; }

    public async Task LogAsync(ClaimedEvent evt, Exception ex, string modelVersion, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.TagFailures.Add(new TagFailure
        {
            OracleId = evt.OracleId,
            EventId = evt.EventId,
            ErrorKind = ClassifyError(ex),
            ErrorMessage = ex.Message,
            Attempts = evt.ClaimAttempts,
            ModelVersion = modelVersion,
            FailedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkConsumedAsync(ClaimedEvent evt, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE card_oracle_events SET consumed_at = now() WHERE event_id = {evt.EventId}", ct);
    }

    private static string ClassifyError(Exception ex) => ex switch
    {
        HttpRequestException => "http_error",
        System.Text.Json.JsonException => "schema_violation",
        TaskCanceledException => "http_error",
        InvalidOperationException io when io.Message.Contains("schema", StringComparison.OrdinalIgnoreCase) => "schema_violation",
        _ => "other",
    };
}
