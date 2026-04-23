using MysticForge.Domain.Events;

namespace MysticForge.Application.Scryfall;

public interface IOracleEventEmitter
{
    Task<int> EmitAsync(IReadOnlyList<CardOracleEvent> events, CancellationToken ct);
}
