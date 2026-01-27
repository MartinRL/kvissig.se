namespace MerEllerMindre.Domain;

/// <summary>
/// GameState is derived by folding events through Evolve.
/// Never stored directly - always reconstructed from events.
/// </summary>
public record GameState
{
    public string? GameId { get; init; }
    public string? JoinCode { get; init; }
    public string? QuestionPackId { get; init; }
    public GamePhase Phase { get; init; } = GamePhase.NotCreated;
    public IReadOnlyList<Player> Players { get; init; } = [];
    public int CurrentQuestionIndex { get; init; } = -1;
    public IReadOnlyDictionary<string, int> CurrentGuesses { get; init; } = 
        new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> Scores { get; init; } = 
        new Dictionary<string, int>();
    public int TotalQuestions { get; init; }
    
    public static GameState Initial => new();
}

public record Player(
    string PlayerId,
    string Name,
    bool IsHost
);

public enum GamePhase
{
    NotCreated,
    Lobby,
    Question,
    Results,
    Ended
}
