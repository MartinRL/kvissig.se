using System.Collections.Immutable;
using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// In-memory event store: a lock-protected dictionary of per-game streams. Each stream is an
/// append-only <see cref="ImmutableArray{T}"/>—appends produce a new array, events are never
/// mutated or removed (the immutability is enforced by the type). Registered as a singleton
/// (the log is process-wide game state). See ADR 001.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, ImmutableArray<GameEvent>> _streams = new();

    public void Append(Guid gameId, IEnumerable<GameEvent> events)
    {
        lock (_gate)
        {
            var stream = _streams.TryGetValue(gameId, out var existing)
                ? existing
                : ImmutableArray<GameEvent>.Empty;

            _streams[gameId] = stream.AddRange(events);
        }
    }

    public IReadOnlyList<GameEvent> Read(Guid gameId)
    {
        lock (_gate)
        {
            return _streams.TryGetValue(gameId, out var stream)
                ? stream
                : ImmutableArray<GameEvent>.Empty;
        }
    }
}
