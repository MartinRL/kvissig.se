using System.Collections.Immutable;
using Blindbudet.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Imperative-shell command side for Blindbudet: an in-memory append-only event log (one
/// stream per gameId), a joinCode→gameId index, and the gears. Loads state via Decider.Fold,
/// runs the pure Decider.Decide, persists the events.
///
/// ponytail: store + repository + service collapsed into ONE class (n=1 impl, no interface —
/// MEM split them across three files; the second game doesn't need the ceremony). Registered
/// as a singleton — the log is process-wide game state.
///
/// The gears fire on a STATE condition (co-located party game, one room, no timer): the reveal
/// gear resolves the lot the moment the last bid lands; the progression gear (host-paced from
/// the round-results "next" button) advances to the next lot or ends the auction.
/// </summary>
public sealed class AuctionApplicationService
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, ImmutableArray<AuctionEvent>> _streams = new();
    private readonly Dictionary<Guid, Guid> _joinCodeToGameId = new();
    private readonly AuctionContext _context;

    public AuctionApplicationService(FileSystemAuctionPackCatalog catalog) =>
        _context = new AuctionContext(Guid.NewGuid, () => DateTimeOffset.UtcNow, catalog.Find, Random.Shared.Next);

    public AuctionState Load(Guid gameId)
    {
        lock (_gate)
            return Decider.Fold(Stream(gameId));
    }

    public Guid? ResolveJoinCode(Guid joinCode)
    {
        lock (_gate)
            return _joinCodeToGameId.TryGetValue(joinCode, out var gameId) ? gameId : null;
    }

    /// <summary>Open a new auction. The Decider mints gameId/joinCode/hostPlayerId itself.</summary>
    public Result<AuctionEvent[]> Open(OpenAuction command)
    {
        lock (_gate)
        {
            var result = Decider.Decide(AuctionState.Initial, command, _context);
            if (result is Ok<AuctionEvent[]> ok && ok.Value is [AuctionOpened opened, ..])
                Append(opened.GameId, ok.Value);
            return result;
        }
    }

    /// <summary>Execute a command against an existing auction stream.</summary>
    public Result<AuctionEvent[]> Execute(Guid gameId, AuctionCommand command)
    {
        lock (_gate)
            return ExecuteLocked(gameId, command);
    }

    /// <summary>Reveal gear: resolve the current lot the moment the last bid lands.</summary>
    public void RunRevealGear(Guid gameId)
    {
        lock (_gate)
        {
            var state = Decider.Fold(Stream(gameId));
            if (state.Phase != AuctionPhase.Started)
                return;

            var i = state.CurrentLotIndex;
            if (state.AllBidsIn(i) && !state.Lots[i].Resolved)
                ExecuteLocked(gameId, new RevealLot(gameId, i));
        }
    }

    /// <summary>Progression gear: once the current lot is resolved, ask the next lot or end.</summary>
    public Result<AuctionEvent[]> RunNextGear(Guid gameId)
    {
        lock (_gate)
        {
            var state = Decider.Fold(Stream(gameId));
            if (state.Phase != AuctionPhase.Started || !state.CurrentLotResolved)
                return new Ok<AuctionEvent[]>([]);

            return state.HasNextLot
                ? ExecuteLocked(gameId, new AskNextLot(gameId))
                : ExecuteLocked(gameId, new EndAuction(gameId));
        }
    }

    // --- private (all callers hold _gate) ------------------------------------------------

    private Result<AuctionEvent[]> ExecuteLocked(Guid gameId, AuctionCommand command)
    {
        var state = Decider.Fold(Stream(gameId));
        var result = Decider.Decide(state, command, _context);
        if (result is Ok<AuctionEvent[]> ok && ok.Value.Length > 0)
            Append(gameId, ok.Value);
        return result;
    }

    private ImmutableArray<AuctionEvent> Stream(Guid gameId) =>
        _streams.TryGetValue(gameId, out var s) ? s : ImmutableArray<AuctionEvent>.Empty;

    private void Append(Guid gameId, IReadOnlyList<AuctionEvent> events)
    {
        _streams[gameId] = Stream(gameId).AddRange(events);
        foreach (var e in events)
            if (e is AuctionOpened opened)
                _joinCodeToGameId[opened.JoinCode] = opened.GameId;
    }
}
