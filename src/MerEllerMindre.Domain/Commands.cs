namespace MerEllerMindre.Domain;

/// <summary>
/// Commands represent player intentions - things players want to do.
/// Derived from command (`c:`) elements in specs/game-flows.yaml
/// </summary>
public abstract record GameCommand;

public record CreateGame(
    string HostName,
    string QuestionPackId
) : GameCommand;

public record JoinGame(
    string JoinCode,
    string PlayerName
) : GameCommand;

public record StartGame(
    string GameId
) : GameCommand;

public record SubmitGuess(
    string GameId,
    string PlayerId,
    int Guess
) : GameCommand;
