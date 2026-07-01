namespace Blindbudet.Domain;

/// <summary>
/// Auction lifecycle. The spec's meaningful set is lobby|started|ended; NotCreated is a
/// C# deviation — the empty-stream sentinel for Initial, so AuctionNotFound = NotCreated.
/// </summary>
public enum AuctionPhase
{
    NotCreated,
    Lobby,
    Started,
    Ended
}

public record Player(
    Guid PlayerId,
    string Name,
    bool IsHost
);

/// <summary>
/// The immutable lot card plus how it is being bid on. The deck is loaded up front (one
/// LotRound per lot). Each hidden bid folds into Bids in event-log order (earliest top bid
/// wins ties). At reveal, TrueWorth/WinnerId/PricePaid + the per-player Profits are set and
/// Resolved flips true.
///
/// The per-player maps are keyed by playerId — a deliberate deviation from the constitution's
/// IReadOnlyList&lt;T&gt; rule for the keyed decision model (see spec). Wire events stay flat
/// with an explicit playerId; only the folded State is keyed.
/// </summary>
public record LotRound
{
    public required Lot Lot { get; init; }
    public IReadOnlyDictionary<Guid, decimal> Bids { get; init; } = new Dictionary<Guid, decimal>();
    public decimal? TrueWorth { get; init; }
    public Guid? WinnerId { get; init; }
    public decimal? PricePaid { get; init; }
    public IReadOnlyDictionary<Guid, int> Profits { get; init; } = new Dictionary<Guid, int>();
    public bool Resolved { get; init; }
}

/// <summary>
/// A row of the final scoreboard, carried on AuctionEnded.
/// </summary>
public record ScoreboardEntry(
    Guid PlayerId,
    string PlayerName,
    int TotalScore
);

/// <summary>
/// AuctionState is derived by folding events through Evolve. Never stored directly — always
/// reconstructed from events. Progress/pending/totals are DERIVED (methods), not stored, so
/// lots + players are the single source of truth.
/// </summary>
public record AuctionState
{
    public Guid GameId { get; init; }
    public Guid JoinCode { get; init; }
    public string PackId { get; init; } = "";
    public AuctionPhase Phase { get; init; } = AuctionPhase.NotCreated;
    public Guid HostPlayerId { get; init; }
    public IReadOnlyList<Player> Players { get; init; } = [];
    public int CurrentLotIndex { get; init; } = -1;
    public IReadOnlyList<LotRound> Lots { get; init; } = [];

    // Folded from AuctionEnded so the Final Standings projection passes them straight through
    // (the scoreboard is computed in Decide/EndAuction; Evolve just records it).
    public IReadOnlyList<ScoreboardEntry> FinalScoreboard { get; init; } = [];
    public IReadOnlyList<Guid> WinnerIds { get; init; } = [];

    public static AuctionState Initial => new();

    /// <summary>Players who have not yet bid on lot i.</summary>
    public IReadOnlyList<Guid> PendingBidPlayerIds(int i) =>
        Players
            .Where(p => !Lots[i].Bids.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();

    /// <summary>True once every player has bid on lot i.</summary>
    public bool AllBidsIn(int i) => PendingBidPlayerIds(i).Count == 0;

    /// <summary>Whether the current lot has been revealed/scored.</summary>
    public bool CurrentLotResolved => Lots[CurrentLotIndex].Resolved;

    /// <summary>Whether another lot follows the current one.</summary>
    public bool HasNextLot => CurrentLotIndex + 1 < Lots.Count;

    /// <summary>Running total: sum of a player's profits over resolved lots (signed).</summary>
    public int TotalScore(Guid playerId) =>
        Lots
            .Where(l => l.Resolved)
            .Sum(l => l.Profits.TryGetValue(playerId, out var s) ? s : 0);
}
