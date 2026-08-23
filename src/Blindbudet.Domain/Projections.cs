namespace Blindbudet.Domain;

/// <summary>
/// Read-model views (spec `v:` elements). Each is a pure projection of the folded
/// AuctionState — view data is DERIVED, never stored. Per-player ordering follows
/// AuctionState.Players order. (Auction Catalog is NOT here — it is Web-only reference data
/// read straight from the CSV catalog, like MEM's Quiz Catalog.)
/// </summary>
public record RosterView(
    Guid GameId,
    Guid JoinCode,
    IReadOnlyList<Player> Players
);

public record LotCardView(
    Guid GameId,
    int LotIndex,
    int TotalLots,
    string Description,
    string Unit
);

public record BidProgressView(
    Guid GameId,
    int LotIndex,
    IReadOnlyList<Guid> SubmittedPlayerIds,
    IReadOnlyList<Guid> PendingPlayerIds
);

/// <summary>One row of the Outstanding-bids todo list: who still owes a bid on a lot.</summary>
public record OutstandingBid(
    int LotIndex,
    IReadOnlyList<Guid> PendingPlayerIds,
    bool AllBidsIn
);

public record OutstandingBidsView(
    Guid GameId,
    IReadOnlyList<OutstandingBid> Lots
);

/// <summary>Per-player round-results row: the profit on this lot + running total.</summary>
public record PlayerProfit(
    Guid PlayerId,
    int Profit,
    int TotalScore
);

public record RoundScoresView(
    Guid GameId,
    int LotIndex,
    decimal TrueWorth,
    IReadOnlyList<Guid> WinnerIds,
    decimal PricePaid,
    IReadOnlyList<PlayerProfit> PlayerProfits
);

public record AuctionProgressView(
    Guid GameId,
    int LotIndex,
    int TotalLots,
    int ResolvedLotCount,
    bool HasNextLot
);

public record ScoreboardView(
    Guid GameId,
    IReadOnlyList<ScoreboardEntry> FinalScoreboard,
    IReadOnlyList<Guid> WinnerIds
);

/// <summary>
/// Pure projections (AuctionState -> View). The Web shell folds the event stream via
/// Decider.Fold, then projects the view it needs to render.
/// </summary>
public static class Projections
{
    public static RosterView Roster(AuctionState state) =>
        new(state.GameId, state.JoinCode, state.Players);

    public static LotCardView LotCard(AuctionState state)
    {
        var i = state.CurrentLotIndex;
        var lot = state.Lots[i].Lot;
        return new LotCardView(state.GameId, i, state.Lots.Count, lot.Description, lot.Unit);
    }

    public static BidProgressView BidProgress(AuctionState state)
    {
        var i = state.CurrentLotIndex;
        var submitted = state.Players
            .Where(p => state.Lots[i].Bids.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();
        return new BidProgressView(state.GameId, i, submitted, state.PendingBidPlayerIds(i));
    }

    public static OutstandingBidsView OutstandingBids(AuctionState state)
    {
        var lots = state.Lots
            .Select((_, i) => new OutstandingBid(i, state.PendingBidPlayerIds(i), state.AllBidsIn(i)))
            .ToList();
        return new OutstandingBidsView(state.GameId, lots);
    }

    public static RoundScoresView RoundScores(AuctionState state)
    {
        var i = state.CurrentLotIndex;
        var round = state.Lots[i];
        var playerProfits = state.Players
            .Select(p => new PlayerProfit(p.PlayerId, round.Profits[p.PlayerId], RunningTotal(state, p.PlayerId, i)))
            .ToList();
        return new RoundScoresView(
            state.GameId,
            i,
            round.TrueWorth!.Value,
            round.WinnerIds,
            round.PricePaid!.Value,
            playerProfits);
    }

    public static AuctionProgressView AuctionProgress(AuctionState state) =>
        new(
            state.GameId,
            state.CurrentLotIndex,
            state.Lots.Count,
            state.Lots.Count(l => l.Resolved),
            state.HasNextLot);

    public static ScoreboardView Scoreboard(AuctionState state) =>
        new(state.GameId, state.FinalScoreboard, state.WinnerIds);

    /// <summary>Running total at lot i: sum of a player's profits over resolved lots up to and including i.</summary>
    private static int RunningTotal(AuctionState state, Guid playerId, int upToIndex) =>
        state.Lots
            .Where((l, i) => l.Resolved && i <= upToIndex)
            .Sum(l => l.Profits.TryGetValue(playerId, out var s) ? s : 0);
}
