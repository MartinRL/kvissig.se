namespace TankTillTusen.Domain;

/// <summary>
/// Commands represent player/system intentions for one arithmetic-puzzle game. Modeled as a
/// native C# 15 union (closed, exhaustive switches, no default arm). Derived from command
/// (`c:`) elements in specs/tank-till-tusen-event-model.yaml. A THIRD Decider beside MEM's
/// GameCommand and BlindBudet's AuctionCommand — never touching theirs.
/// </summary>
public record OpenLobby(
    string HostName
);

public record JoinGame(
    Guid JoinCode,
    string PlayerName
);

public record StartGame(
    Guid GameId
);

public record SubmitSolution(
    Guid GameId,
    Guid PlayerId,
    int RoundIndex,
    Solution Solution
);

public record ScoreRound(
    Guid GameId,
    int RoundIndex
);

public record AskNextPuzzle(
    Guid GameId
);

public record EndGame(
    Guid GameId
);

public union TankCommand(
    OpenLobby,
    JoinGame,
    StartGame,
    SubmitSolution,
    ScoreRound,
    AskNextPuzzle,
    EndGame
);
