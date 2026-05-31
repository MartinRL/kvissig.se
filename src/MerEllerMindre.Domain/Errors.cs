namespace MerEllerMindre.Domain;

/// <summary>
/// Errors represent why a command was rejected.
/// Derived from exception (`x:`) elements in specs/game-flows.yaml
/// </summary>
public abstract record GameError;

public record GameNotFound(
    string JoinCode
) : GameError;

public record GameAlreadyStarted(
    string GameId
) : GameError;

public record GuessOutOfRange(
    int Guess,
    int Min,
    int Max
) : GameError;

public record AlreadyGuessed(
    string PlayerId,
    int QuestionIndex
) : GameError;

public record PlayerNotInGame(
    string PlayerId,
    string GameId
) : GameError;

public record GameNotStarted(
    string GameId
) : GameError;

public record NotEnoughPlayers(
    string GameId,
    int MinRequired,
    int Actual
) : GameError;
