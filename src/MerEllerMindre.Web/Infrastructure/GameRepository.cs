using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Loads/saves game streams on top of the <see cref="IEventStore"/>. Load folds the stream
/// into <see cref="GameState"/> via <see cref="Decider.Fold"/>; Append persists new events and
/// maintains a joinCode → gameId index so the join URL (a Guid joinCode) resolves to its game.
/// Registered as a singleton (the index is process-wide, like the event log).
/// </summary>
public sealed class GameRepository
{
    private readonly IEventStore _store;
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, Guid> _joinCodeToGameId = new();

    public GameRepository(IEventStore store) => _store = store;

    public GameState Load(Guid gameId) => Decider.Fold(_store.Read(gameId));

    public void Append(Guid gameId, IReadOnlyList<GameEvent> events)
    {
        _store.Append(gameId, events);

        lock (_gate)
        {
            foreach (var e in events)
                if (e is LobbyOpened opened)
                    _joinCodeToGameId[opened.JoinCode] = opened.GameId;
        }
    }

    public Guid? ResolveJoinCode(Guid joinCode)
    {
        lock (_gate)
            return _joinCodeToGameId.TryGetValue(joinCode, out var gameId) ? gameId : null;
    }
}
