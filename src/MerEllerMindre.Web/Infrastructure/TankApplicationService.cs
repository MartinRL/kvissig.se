using System.Collections.Immutable;
using TankTillTusen.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Imperative-shell command side for Tänk Till Tusen: an in-memory append-only event log (one
/// stream per gameId), a joinCode→gameId index, and the gears. Loads state via Decider.Fold,
/// runs the pure Decider.Decide, persists the events.
///
/// ponytail: store + repository + service collapsed into ONE class (n=1 impl, no interface) —
/// same shape as Blindbudet's AuctionApplicationService. Registered as a singleton.
///
/// The gears fire on a STATE condition. Unlike Blindbudet this game has a HARD DEADLINE: the
/// score gear resolves a round the moment the last solution lands OR the 45 s deadline passes
/// (driven by the waiting-screen poll and the /state poll), so a round closes even if someone
/// never submits. The progression gear (host-paced from the round-results "next" button)
/// advances to the next puzzle or ends the game.
/// </summary>
public sealed class TankApplicationService
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, ImmutableArray<TankEvent>> _streams = new();
    private readonly Dictionary<Guid, Guid> _joinCodeToGameId = new();
    private readonly TankContext _context = TankContext.Default;

    public DateTimeOffset Now => _context.Now();

    public TankState Load(Guid gameId)
    {
        lock (_gate)
            return Decider.Fold(Stream(gameId));
    }

    public Guid? ResolveJoinCode(Guid joinCode)
    {
        lock (_gate)
            return _joinCodeToGameId.TryGetValue(joinCode, out var gameId) ? gameId : null;
    }

    /// <summary>Open a new game. The Decider mints gameId/joinCode/hostPlayerId + puzzles itself.</summary>
    public Result<TankEvent[]> Open(OpenLobby command)
    {
        lock (_gate)
        {
            var result = Decider.Decide(TankState.Initial, command, _context);
            if (result is Ok<TankEvent[]> ok && ok.Value is [LobbyOpened opened, ..])
                Append(opened.GameId, ok.Value);
            return result;
        }
    }

    /// <summary>Execute a command against an existing game stream.</summary>
    public Result<TankEvent[]> Execute(Guid gameId, TankCommand command)
    {
        lock (_gate)
            return ExecuteLocked(gameId, command);
    }

    /// <summary>Score gear: resolve the current round once all solutions are in OR the deadline passes.</summary>
    public void RunScoreGear(Guid gameId)
    {
        lock (_gate)
        {
            var state = Decider.Fold(Stream(gameId));
            if (state.Phase != TankPhase.Started)
                return;

            var i = state.CurrentRoundIndex;
            if (!state.Rounds[i].Scored && state.ReadyToScore(i, _context.Now()))
                ExecuteLocked(gameId, new ScoreRound(gameId, i));
        }
    }

    /// <summary>Progression gear: once the current round is scored, ask the next puzzle or end.</summary>
    public Result<TankEvent[]> RunNextGear(Guid gameId)
    {
        lock (_gate)
        {
            var state = Decider.Fold(Stream(gameId));
            if (state.Phase != TankPhase.Started || !state.CurrentRoundScored)
                return new Ok<TankEvent[]>([]);

            return state.HasNextPuzzle
                ? ExecuteLocked(gameId, new AskNextPuzzle(gameId))
                : ExecuteLocked(gameId, new EndGame(gameId));
        }
    }

    // --- private (all callers hold _gate) ------------------------------------------------

    private Result<TankEvent[]> ExecuteLocked(Guid gameId, TankCommand command)
    {
        var state = Decider.Fold(Stream(gameId));
        var result = Decider.Decide(state, command, _context);
        if (result is Ok<TankEvent[]> ok && ok.Value.Length > 0)
            Append(gameId, ok.Value);
        return result;
    }

    private ImmutableArray<TankEvent> Stream(Guid gameId) =>
        _streams.TryGetValue(gameId, out var s) ? s : ImmutableArray<TankEvent>.Empty;

    private void Append(Guid gameId, IReadOnlyList<TankEvent> events)
    {
        _streams[gameId] = Stream(gameId).AddRange(events);
        foreach (var e in events)
            if (e is LobbyOpened opened)
                _joinCodeToGameId[opened.JoinCode] = opened.GameId;
    }
}
