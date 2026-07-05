namespace TankTillTusen.Domain;

/// <summary>
/// Errors are why a command was rejected — values on the Result failure track (ROP, never
/// thrown; see ADR 006). Native C# 15 union of parameterless marker records (the spec asserts
/// no props). Derived from exception (`x:`) elements in
/// specs/tank-till-tusen-event-model.yaml.
/// </summary>
public record GameNotFound;

public record GameAlreadyStarted;

public record NameAlreadyTaken;

public record NotEnoughPlayers;

public record AlreadySubmitted;

public record RoundAlreadyScored;

public record DeadlinePassed;

public record InvalidSolution;

public record NotReadyToScore;

public union TankError(
    GameNotFound,
    GameAlreadyStarted,
    NameAlreadyTaken,
    NotEnoughPlayers,
    AlreadySubmitted,
    RoundAlreadyScored,
    DeadlinePassed,
    InvalidSolution,
    NotReadyToScore
);
