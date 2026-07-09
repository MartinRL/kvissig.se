namespace TankTillTusen.Domain;

/// <summary>
/// Game lifecycle. The spec's meaningful set is lobby|started|ended; NotCreated is a C#
/// deviation — the empty-stream sentinel for Initial, so GameNotFound = NotCreated.
/// </summary>
public enum TankPhase
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
/// The immutable puzzle card plus how it is being solved. The whole set is loaded up front (one
/// PuzzleRound per generated Puzzle). Each hidden solution folds into Solutions in event-log
/// order. At score, SampleSolution/ReachedValues/RoundScores are set and Scored flips true.
///
/// The per-player maps are keyed by playerId — a deliberate deviation from the constitution's
/// IReadOnlyList&lt;T&gt; rule for the keyed decision model (see spec). Wire events stay flat
/// with an explicit playerId; only the folded State is keyed.
/// </summary>
public record PuzzleRound
{
    public required Puzzle Puzzle { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public IReadOnlyDictionary<Guid, Solution> Solutions { get; init; } = new Dictionary<Guid, Solution>();
    public Solution? SampleSolution { get; init; }
    public IReadOnlyDictionary<Guid, int> ReachedValues { get; init; } = new Dictionary<Guid, int>();
    public IReadOnlyDictionary<Guid, int> RoundScores { get; init; } = new Dictionary<Guid, int>();
    public bool Scored { get; init; }
}

/// <summary>
/// A row of the final scoreboard, carried on GameEnded.
/// </summary>
public record ScoreboardEntry(
    Guid PlayerId,
    string PlayerName,
    int TotalScore
);

/// <summary>
/// TankState is derived by folding events through Evolve. Never stored directly — always
/// reconstructed from events. Progress/pending/totals + the round deadline are DERIVED
/// (methods), not stored, so rounds + players are the single source of truth.
/// </summary>
public record TankState
{
    public Guid GameId { get; init; }
    public Guid JoinCode { get; init; }
    public TankPhase Phase { get; init; } = TankPhase.NotCreated;
    public Guid HostPlayerId { get; init; }
    public IReadOnlyList<Player> Players { get; init; } = [];
    public int CurrentRoundIndex { get; init; } = -1;
    public IReadOnlyList<PuzzleRound> Rounds { get; init; } = [];

    // Folded from GameEnded so the Final Standings projection passes them straight through
    // (the scoreboard is computed in Decide/EndGame; Evolve just records it).
    public IReadOnlyList<ScoreboardEntry> FinalScoreboard { get; init; } = [];
    public IReadOnlyList<Guid> WinnerIds { get; init; } = [];

    public static TankState Initial => new();

    /// <summary>Players who have not yet submitted a solution for round i.</summary>
    public IReadOnlyList<Guid> PendingPlayerIds(int i) =>
        Players
            .Where(p => !Rounds[i].Solutions.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();

    /// <summary>True once every player has submitted for round i.</summary>
    public bool AllSolutionsIn(int i) => PendingPlayerIds(i).Count == 0;

    /// <summary>The hard deadline for round i (startedAt + 60s), or null before it starts.</summary>
    public DateTimeOffset? Deadline(int i) =>
        Rounds[i].StartedAt is { } startedAt
            ? startedAt.AddSeconds(Decider.CountdownSeconds)
            : null;

    /// <summary>
    /// True once now is at/past round i's deadline + grace (the round must have started).
    /// The grace lets the client's timeout auto-lock (ceil-rounded clock + latency) land;
    /// the visible clock still counts down to the ungraced Deadline.
    /// </summary>
    public bool DeadlinePassed(int i, DateTimeOffset now) =>
        Deadline(i) is { } deadline && now >= deadline.AddSeconds(Decider.GraceSeconds);

    /// <summary>The score gear's gate: every solution is in OR the clock has run out.</summary>
    public bool ReadyToScore(int i, DateTimeOffset now) =>
        AllSolutionsIn(i) || DeadlinePassed(i, now);

    /// <summary>Whether the current round has been scored.</summary>
    public bool CurrentRoundScored => Rounds[CurrentRoundIndex].Scored;

    /// <summary>Whether another round follows the current one.</summary>
    public bool HasNextPuzzle => CurrentRoundIndex + 1 < Rounds.Count;

    /// <summary>Running total: sum of a player's round scores over scored rounds.</summary>
    public int TotalScore(Guid playerId) =>
        Rounds
            .Where(r => r.Scored)
            .Sum(r => r.RoundScores.TryGetValue(playerId, out var s) ? s : 0);
}
