namespace MerEllerMindre.Domain;

/// <summary>
/// Events represent facts that happened - immutable history.
/// Derived from event (`e:`) elements in specs/game-flows.yaml
/// </summary>
public abstract record GameEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record GameCreated(
    string GameId,
    string HostPlayerId,
    string JoinCode,
    string QuestionPackId
) : GameEvent;

public record PlayerJoined(
    string GameId,
    string PlayerId,
    string PlayerName
) : GameEvent;

public record GameStarted(
    string GameId,
    int FirstQuestionIndex
) : GameEvent;

public record GuessSubmitted(
    string GameId,
    string PlayerId,
    int Guess,
    int QuestionIndex
) : GameEvent;

public record AllGuessesSubmitted(
    string GameId,
    int QuestionIndex
) : GameEvent;

public record QuestionScored(
    string GameId,
    int QuestionIndex,
    int CorrectAnswer,
    IReadOnlyDictionary<string, int> Scores,
    string? WinnerId
) : GameEvent;

public record NextQuestionStarted(
    string GameId,
    int QuestionIndex
) : GameEvent;

public record GameEnded(
    string GameId,
    IReadOnlyDictionary<string, int> FinalScoreboard,
    string WinnerId
) : GameEvent;
