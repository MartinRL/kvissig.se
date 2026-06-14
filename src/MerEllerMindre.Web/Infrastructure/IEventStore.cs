using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Imperative-shell event store. The shell owns the mutable event log; the event TYPES
/// (GameEvent records) live in the Domain functional core. One stream per game, keyed by
/// gameId. See ADR 001 (in-memory event sourcing).
/// </summary>
public interface IEventStore
{
    /// <summary>Append events to a game's stream, in order.</summary>
    void Append(Guid gameId, IEnumerable<GameEvent> events);

    /// <summary>Read a snapshot of a game's stream. Empty if the game does not exist.</summary>
    IReadOnlyList<GameEvent> Read(Guid gameId);
}
