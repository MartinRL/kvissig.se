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
/// A player's guess: the direction plus the RAW absolute difference in the card's own
/// unit (>= 0, NOT 0-100). Normalization happens server-side in ScoreQuestion.
/// </summary>
public record Guess(
    Direction Direction,
    decimal GuessedDifference
);

/// <summary>
/// The fixed card plus how it is being answered. The deck is loaded up front (one
/// QuestionRound per card) and each guess folds into its question. correctDirection/
/// correctDifference are revealed (non-null) once scored.
///
/// guesses/roundScores are maps keyed by playerId — a deliberate deviation from the
/// constitution's IReadOnlyList&lt;T&gt; rule for the keyed decision model (see spec). The
/// value objects never repeat the key; wire events stay flat with an explicit playerId.
/// </summary>
public record QuestionRound
{
    public required Question Card { get; init; }
    public IReadOnlyDictionary<Guid, Guess> Guesses { get; init; } = new Dictionary<Guid, Guess>();
    public Direction? CorrectDirection { get; init; }
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

    public static GameState Initial => new();

    /// <summary>Players who have not yet guessed question i.</summary>
    public IReadOnlyList<Guid> PendingPlayerIds(int i) =>
        Players
            .Where(p => !Questions[i].Guesses.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();

    /// <summary>True once every player has guessed question i.</summary>
    public bool AllGuessesIn(int i) => PendingPlayerIds(i).Count == 0;

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
