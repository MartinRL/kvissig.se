namespace MerEllerMindre.Domain;

/// <summary>
/// Events represent facts that happened — immutable history. Modeled as a native C# 15
/// union (closed, exhaustive switches, no default arm). Each event carries its own
/// explicit timestamp (*At) per spec. Derived from event (`e:`) elements in
/// specs/game-flows.yaml.
/// </summary>
public record LobbyOpened(
    Guid GameId,
    Guid HostPlayerId,
    string HostName,
    Guid JoinCode,
    string QuestionPackId,
    IReadOnlyList<Question> Questions,
    DateTimeOffset CreatedAt
);

public record PlayerJoined(
    Guid GameId,
    Guid PlayerId,
    string PlayerName,
    DateTimeOffset JoinedAt
);

public record GameStarted(
    Guid GameId,
    int FirstQuestionIndex,
    DateTimeOffset StartedAt
);

public record GuessSubmitted(
    Guid GameId,
    Guid PlayerId,
    int QuestionIndex,
    Direction Direction,
    decimal GuessedDifference,
    DateTimeOffset SubmittedAt
);

public record QuestionAnswered(
    Guid GameId,
    int QuestionIndex,
    Direction CorrectDirection,
    byte CorrectDifference
);

public record QuestionScored(
    Guid GameId,
    int QuestionIndex,
    Guid PlayerId,
    Direction GuessedDirection,
    decimal GuessedDifference,
    byte GuessedDifferenceNormalized,
    bool DirectionCorrect,
    byte DifferencePoints,
    int BonusPoints,
    int RoundScore,
    int TotalScore
);

public record NextQuestionStarted(
    Guid GameId,
    int QuestionIndex
);

public record GameEnded(
    Guid GameId,
    IReadOnlyList<ScoreboardEntry> FinalScoreboard,
    IReadOnlyList<Guid> WinnerIds,
    DateTimeOffset EndedAt
);

public union GameEvent(
    LobbyOpened,
    PlayerJoined,
    GameStarted,
    GuessSubmitted,
    QuestionAnswered,
    QuestionScored,
    NextQuestionStarted,
    GameEnded
);
