namespace MerEllerMindre.Domain;

/// <summary>
/// Errors represent why a command was rejected — values on the Result failure track
/// (ROP, never thrown; see ADR 006). Modeled as a native C# 15 union of parameterless
/// marker records (the spec asserts no props on these). Derived from exception (`x:`)
/// elements in specs/game-flows.yaml.
/// </summary>
public record QuestionPackNotFound;

public record GameNotFound;

public record GameAlreadyStarted;

public record NameAlreadyTaken;

public record NotEnoughPlayers;

public record GameNotStarted;

public record PlayerNotInGame;

public record AlreadyGuessed;

public record DifferenceOutOfRange;

public record NotAllGuessesIn;

public record QuestionAlreadyScored;

public union GameError(
    QuestionPackNotFound,
    GameNotFound,
    GameAlreadyStarted,
    NameAlreadyTaken,
    NotEnoughPlayers,
    GameNotStarted,
    PlayerNotInGame,
    AlreadyGuessed,
    DifferenceOutOfRange,
    NotAllGuessesIn,
    QuestionAlreadyScored
);
