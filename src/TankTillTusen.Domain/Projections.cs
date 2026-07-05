namespace TankTillTusen.Domain;

/// <summary>
/// Read-model views (spec `v:` elements). Each is a pure projection of the folded TankState —
/// view data is DERIVED, never stored. Per-player ordering follows TankState.Players order.
/// (Quiz Catalog is NOT here — it is Web-only reference data, a single fixed v1 "Spela".)
/// </summary>
public record GameLobbyView(
    Guid GameId,
    Guid JoinCode,
    IReadOnlyList<Player> Players
);

public record PuzzleView(
    Guid GameId,
    int RoundIndex,
    int TotalRounds,
    IReadOnlyList<int> Numbers,
    int Target,
    DateTimeOffset Deadline
);

public record WaitingForOthersView(
    Guid GameId,
    int RoundIndex,
    IReadOnlyList<Guid> SubmittedPlayerIds,
    IReadOnlyList<Guid> PendingPlayerIds
);

/// <summary>One row of the Outstanding-solutions todo: who still owes a solution on a round.</summary>
public record OutstandingSolution(
    int RoundIndex,
    IReadOnlyList<Guid> PendingPlayerIds,
    bool AllSolutionsIn,
    DateTimeOffset? Deadline
);

public record OutstandingSolutionsView(
    Guid GameId,
    IReadOnlyList<OutstandingSolution> Rounds
);

/// <summary>Per-player round-results row: the value reached this round + score + running total.</summary>
public record PlayerResult(
    Guid PlayerId,
    int? ReachedValue,
    int RoundScore,
    int TotalScore
);

public record RoundResultsView(
    Guid GameId,
    int RoundIndex,
    int Target,
    Solution SampleSolution,
    IReadOnlyList<PlayerResult> PlayerResults
);

public record GameProgressView(
    Guid GameId,
    int RoundIndex,
    int TotalRounds,
    int ResolvedRoundCount,
    bool HasNextPuzzle
);

public record FinalStandingsView(
    Guid GameId,
    IReadOnlyList<ScoreboardEntry> FinalScoreboard,
    IReadOnlyList<Guid> WinnerIds
);

/// <summary>
/// Pure projections (TankState -> View). The Web shell folds the event stream via Decider.Fold,
/// then projects the view it needs to render.
/// </summary>
public static class Projections
{
    public static GameLobbyView GameLobby(TankState state) =>
        new(state.GameId, state.JoinCode, state.Players);

    public static PuzzleView Puzzle(TankState state)
    {
        var i = state.CurrentRoundIndex;
        var puzzle = state.Rounds[i].Puzzle;
        return new PuzzleView(state.GameId, i, state.Rounds.Count, puzzle.Numbers, puzzle.Target, state.Deadline(i)!.Value);
    }

    public static WaitingForOthersView WaitingForOthers(TankState state)
    {
        var i = state.CurrentRoundIndex;
        var submitted = state.Players
            .Where(p => state.Rounds[i].Solutions.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();
        return new WaitingForOthersView(state.GameId, i, submitted, state.PendingPlayerIds(i));
    }

    public static OutstandingSolutionsView OutstandingSolutions(TankState state)
    {
        var rounds = state.Rounds
            .Select((_, i) => new OutstandingSolution(i, state.PendingPlayerIds(i), state.AllSolutionsIn(i), state.Deadline(i)))
            .ToList();
        return new OutstandingSolutionsView(state.GameId, rounds);
    }

    public static RoundResultsView RoundResults(TankState state)
    {
        var i = state.CurrentRoundIndex;
        var round = state.Rounds[i];
        var playerResults = state.Players
            .Select(p => new PlayerResult(
                p.PlayerId,
                round.ReachedValues.TryGetValue(p.PlayerId, out var v) ? v : null,
                round.RoundScores[p.PlayerId],
                RunningTotal(state, p.PlayerId, i)))
            .ToList();
        return new RoundResultsView(state.GameId, i, round.Puzzle.Target, round.SampleSolution!, playerResults);
    }

    public static GameProgressView GameProgress(TankState state) =>
        new(
            state.GameId,
            state.CurrentRoundIndex,
            state.Rounds.Count,
            state.Rounds.Count(r => r.Scored),
            state.HasNextPuzzle);

    public static FinalStandingsView FinalStandings(TankState state) =>
        new(state.GameId, state.FinalScoreboard, state.WinnerIds);

    /// <summary>Running total at round i: sum of a player's scores over scored rounds up to and including i.</summary>
    private static int RunningTotal(TankState state, Guid playerId, int upToIndex) =>
        state.Rounds
            .Where((r, i) => r.Scored && i <= upToIndex)
            .Sum(r => r.RoundScores.TryGetValue(playerId, out var s) ? s : 0);
}
