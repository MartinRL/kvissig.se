namespace MerEllerMindre.Domain;

/// <summary>
/// Commands represent player/system intentions. Modeled as a native C# 15 union
/// (closed, exhaustive switches, no default arm). Derived from command (`c:`)
/// elements in specs/game-flows.yaml.
/// </summary>
public record OpenLobby(
    string HostName,
    string QuestionPackId
);

public record JoinGame(
    Guid JoinCode,
    string PlayerName
);

public record StartGame(
    Guid GameId
);

public record SubmitDirection(
    Guid GameId,
    Guid PlayerId,
    Direction Direction
);

public record RevealDirection(
    Guid GameId,
    int QuestionIndex
);

public record SubmitDifference(
    Guid GameId,
    Guid PlayerId,
    decimal GuessedDifference
);

public record ScoreDifference(
    Guid GameId,
    int QuestionIndex
);

public record AskNextQuestion(
    Guid GameId
);

public record EndGame(
    Guid GameId
);

public union GameCommand(
    OpenLobby,
    JoinGame,
    StartGame,
    SubmitDirection,
    RevealDirection,
    SubmitDifference,
    ScoreDifference,
    AskNextQuestion,
    EndGame
);
