namespace MerEllerMindre.Domain;

/// <summary>
/// Which value is larger. Mer = ItemA holds the larger value (author convention).
/// </summary>
public enum Direction
{
    Mer,
    Mindre
}

/// <summary>
/// Game lifecycle. The spec's meaningful set is lobby|started|ended; NotCreated is a
/// C# deviation — the empty-stream sentinel for Initial, so GameNotFound = NotCreated.
/// </summary>
public enum GamePhase
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
/// The fixed card plus how it is being answered across the two-stage rocket. The deck is
/// loaded up front (one QuestionRound per card). STAGE 1: each direction folds into
/// Directions; correctDirection + the per-player bonus (DirectionScores) are set at the
/// stage-1 reveal. STAGE 2: each raw magnitude folds into Differences; correctDifference +
/// the combined RoundScores are set at the stage-2 score. Normalization happens
/// server-side in ScoreDifference.
///
/// The per-player maps are keyed by playerId — a deliberate deviation from the
/// constitution's IReadOnlyList&lt;T&gt; rule for the keyed decision model (see spec). The
/// value objects never repeat the key; wire events stay flat with an explicit playerId.
/// </summary>
public record QuestionRound
{
    public required Question Card { get; init; }
    public IReadOnlyDictionary<Guid, Direction> Directions { get; init; } = new Dictionary<Guid, Direction>();
    public Direction? CorrectDirection { get; init; }
    public IReadOnlyDictionary<Guid, int> DirectionScores { get; init; } = new Dictionary<Guid, int>();
    public IReadOnlyDictionary<Guid, decimal> Differences { get; init; } = new Dictionary<Guid, decimal>();
    public byte? CorrectDifference { get; init; }
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
/// Per-player round-results DTO (for the Round results read model).
/// </summary>
public record PlayerScore(
    Guid PlayerId,
    int RoundScore,
    int TotalScore
);

/// <summary>
/// GameState is derived by folding events through Evolve. Never stored directly —
/// always reconstructed from events. Progress/pending/totals are DERIVED (methods),
/// not stored, so questions + players are the single source of truth.
/// </summary>
public record GameState
{
    public Guid GameId { get; init; }
    public Guid JoinCode { get; init; }
    public string QuestionPackId { get; init; } = "";
    public GamePhase Phase { get; init; } = GamePhase.NotCreated;
    public Guid HostPlayerId { get; init; }
    public IReadOnlyList<Player> Players { get; init; } = [];
    public int CurrentQuestionIndex { get; init; } = -1;
    public IReadOnlyList<QuestionRound> Questions { get; init; } = [];

    // Folded from GameEnded so the Final Standings projection passes them straight
    // through (the scoreboard is computed in Decide/EndGame; Evolve just records it).
    public IReadOnlyList<ScoreboardEntry> FinalScoreboard { get; init; } = [];
    public IReadOnlyList<Guid> WinnerIds { get; init; } = [];

    public static GameState Initial => new();

    /// <summary>Players who have not yet submitted a direction for question i (stage 1).</summary>
    public IReadOnlyList<Guid> PendingDirectionPlayerIds(int i) =>
        Players
            .Where(p => !Questions[i].Directions.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();

    /// <summary>True once every player has submitted a direction for question i.</summary>
    public bool AllDirectionsIn(int i) => PendingDirectionPlayerIds(i).Count == 0;

    /// <summary>True once question i's direction has been revealed (stage 1 closed).</summary>
    public bool DirectionRevealed(int i) => Questions[i].CorrectDirection is not null;

    /// <summary>Players who have not yet submitted a difference for question i (stage 2).</summary>
    public IReadOnlyList<Guid> PendingDifferencePlayerIds(int i) =>
        Players
            .Where(p => !Questions[i].Differences.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();

    /// <summary>True once every player has submitted a difference for question i.</summary>
    public bool AllDifferencesIn(int i) => PendingDifferencePlayerIds(i).Count == 0;

    /// <summary>Whether the current question has been scored.</summary>
    public bool CurrentQuestionScored => Questions[CurrentQuestionIndex].Scored;

    /// <summary>Whether another question follows the current one.</summary>
    public bool HasNextQuestion => CurrentQuestionIndex + 1 < Questions.Count;

    /// <summary>Running total: sum of a player's round scores over scored questions.</summary>
    public int TotalScore(Guid playerId) =>
        Questions
            .Where(q => q.Scored)
            .Sum(q => q.RoundScores.TryGetValue(playerId, out var s) ? s : 0);
}
