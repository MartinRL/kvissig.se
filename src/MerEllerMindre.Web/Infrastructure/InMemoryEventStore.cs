using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// In-memory event store: a lock-protected dictionary of per-game streams. Reads return a
/// snapshot copy so callers fold a stable sequence while other requests append. Registered
/// as a singleton (the log is process-wide game state). See ADR 001.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, List<GameEvent>> _streams = new();

    public void Append(Guid gameId, IEnumerable<GameEvent> events)
    {
        lock (_gate)
        {
            if (!_streams.TryGetValue(gameId, out var stream))
            {
                stream = [];
                _streams[gameId] = stream;
            }

            stream.AddRange(events);
        }
    }

    public IReadOnlyList<GameEvent> Read(Guid gameId)
    {
        lock (_gate)
        {
            return _streams.TryGetValue(gameId, out var stream)
                ? [.. stream]
                : [];
        }
    }
}
