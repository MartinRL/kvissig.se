namespace MerEllerMindre.Domain;

/// <summary>
/// Events represent facts that happened — immutable history. Modeled as a native C# 15
/// union (closed, exhaustive switches, no default arm). Each event carries its own
/// explicit timestamp (*At) per spec. Derived from event (`e:`) elements in
/// specs/mer-eller-mindre-event-model.yaml.
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

// --- Stage 1: direction ---

public record DirectionSubmitted(
    Guid GameId,
    Guid PlayerId,
    int QuestionIndex,
    Direction Direction,
    DateTimeOffset SubmittedAt
);

public record QuestionDirectionRevealed(
    Guid GameId,
    int QuestionIndex,
    Direction CorrectDirection
);

public record DirectionScored(
    Guid GameId,
    int QuestionIndex,
    Guid PlayerId,
    Direction GuessedDirection,
    bool DirectionCorrect,
    int BonusPoints
);

// --- Stage 2: difference ---

public record DifferenceSubmitted(
    Guid GameId,
    Guid PlayerId,
    int QuestionIndex,
    decimal GuessedDifference,
    DateTimeOffset SubmittedAt
);

public record QuestionDifferenceRevealed(
    Guid GameId,
    int QuestionIndex,
    byte CorrectDifference
);

public record DifferenceScored(
    Guid GameId,
    int QuestionIndex,
    Guid PlayerId,
    decimal GuessedDifference,
    byte GuessedDifferenceNormalized,
    byte DifferencePoints,
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
    DirectionSubmitted,
    QuestionDirectionRevealed,
    DirectionScored,
    DifferenceSubmitted,
    QuestionDifferenceRevealed,
    DifferenceScored,
    NextQuestionStarted,
    GameEnded
);
