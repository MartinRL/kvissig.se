namespace TankTillTusen.Domain;

/// <summary>
/// Events are facts that happened — immutable history. Native C# 15 union (closed, exhaustive
/// switches, no default arm). Each event carries its own explicit timestamp (*At) where the
/// spec has one. Derived from event (`e:`) elements in
/// specs/tank-till-tusen-event-model.yaml. Wire events stay flat with an explicit playerId;
/// only the folded State keys per-player data by playerId.
/// </summary>
public record LobbyOpened(
    Guid GameId,
    Guid HostPlayerId,
    string HostName,
    Guid JoinCode,
    IReadOnlyList<Puzzle> Puzzles,
    DateTimeOffset OpenedAt
);

public record PlayerJoined(
    Guid GameId,
    Guid PlayerId,
    string PlayerName,
    DateTimeOffset JoinedAt
);

public record GameStarted(
    Guid GameId,
    int FirstRoundIndex,
    DateTimeOffset StartedAt
);

public record SolutionSubmitted(
    Guid GameId,
    Guid PlayerId,
    int RoundIndex,
    Solution Solution,
    DateTimeOffset SubmittedAt
);

public record PuzzleRevealed(
    Guid GameId,
    int RoundIndex,
    Solution SampleSolution
);

public record RoundScored(
    Guid GameId,
    int RoundIndex,
    Guid PlayerId,
    int? ReachedValue,
    int RoundScore,
    int TotalScore
);

public record NextPuzzleStarted(
    Guid GameId,
    int RoundIndex,
    DateTimeOffset StartedAt
);

public record GameEnded(
    Guid GameId,
    IReadOnlyList<ScoreboardEntry> FinalScoreboard,
    IReadOnlyList<Guid> WinnerIds,
    DateTimeOffset EndedAt
);

public union TankEvent(
    LobbyOpened,
    PlayerJoined,
    GameStarted,
    SolutionSubmitted,
    PuzzleRevealed,
    RoundScored,
    NextPuzzleStarted,
    GameEnded
);
