namespace MerEllerMindre.Domain;

/// <summary>
/// The Decider contains two pure functions:
/// - Evolve: (State, Event) -> State
/// - Decide: (State, Command) -> Result&lt;Event[]&gt;
/// 
/// Both use exhaustive pattern matching switches.
/// </summary>
public static class Decider
{
    /// <summary>
    /// Evolve applies an event to produce new state.
    /// This is a pure function with no side effects.
    /// </summary>
    public static GameState Evolve(GameState state, GameEvent @event) =>
        @event switch
        {
            GameCreated e => state with
            {
                GameId = e.GameId,
                JoinCode = e.JoinCode,
                QuestionPackId = e.QuestionPackId,
                Phase = GamePhase.Lobby,
                Players = [new Player(e.HostPlayerId, "Host", IsHost: true)],
                Scores = new Dictionary<string, int> { [e.HostPlayerId] = 0 }
            },
            
            PlayerJoined e => state with
            {
                Players = [..state.Players, new Player(e.PlayerId, e.PlayerName, IsHost: false)],
                Scores = new Dictionary<string, int>(state.Scores) { [e.PlayerId] = 0 }
            },
            
            GameStarted e => state with
            {
                Phase = GamePhase.Question,
                CurrentQuestionIndex = e.FirstQuestionIndex,
                CurrentGuesses = new Dictionary<string, int>()
            },
            
            GuessSubmitted e => state with
            {
                CurrentGuesses = new Dictionary<string, int>(state.CurrentGuesses)
                {
                    [e.PlayerId] = e.Guess
                }
            },
            
            AllGuessesSubmitted => state, // No state change, just a marker
            
            QuestionScored e => state with
            {
                Phase = GamePhase.Results,
                Scores = new Dictionary<string, int>(e.Scores)
            },
            
            NextQuestionStarted e => state with
            {
                Phase = GamePhase.Question,
                CurrentQuestionIndex = e.QuestionIndex,
                CurrentGuesses = new Dictionary<string, int>()
            },
            
            GameEnded e => state with
            {
                Phase = GamePhase.Ended,
                Scores = new Dictionary<string, int>(e.FinalScoreboard)
            },
            
            _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}")
        };

    /// <summary>
    /// Decide validates a command against current state and produces events.
    /// Returns either events to apply or an error explaining why the command was rejected.
    /// </summary>
    public static Result<GameEvent[]> Decide(GameState state, GameCommand command, GameContext context) =>
        command switch
        {
            CreateGame c => DecideCreateGame(state, c, context),
            JoinGame c => DecideJoinGame(state, c, context),
            StartGame c => DecideStartGame(state, c, context),
            SubmitGuess c => DecideSubmitGuess(state, c, context),
            
            _ => throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}")
        };

    private static Result<GameEvent[]> DecideCreateGame(GameState state, CreateGame command, GameContext context)
    {
        var gameId = context.NewId();
        var hostPlayerId = context.NewId();
        var joinCode = context.NewJoinCode();
        
        return new Ok<GameEvent[]>([
            new GameCreated(gameId, hostPlayerId, joinCode, command.QuestionPackId)
        ]);
    }

    private static Result<GameEvent[]> DecideJoinGame(GameState state, JoinGame command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound(command.JoinCode));

        if (state.Phase != GamePhase.Lobby)
            return new Err(new GameAlreadyStarted(state.GameId!));

        var playerId = context.NewId();

        return new Ok<GameEvent[]>([
            new PlayerJoined(state.GameId!, playerId, command.PlayerName)
        ]);
    }

    private static Result<GameEvent[]> DecideStartGame(GameState state, StartGame command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound(command.GameId));

        if (state.Phase != GamePhase.Lobby)
            return new Err(new GameAlreadyStarted(state.GameId!));

        if (state.Players.Count < 2)
            return new Err(new NotEnoughPlayers(state.GameId!, 2, state.Players.Count));

        return new Ok<GameEvent[]>([
            new GameStarted(state.GameId!, FirstQuestionIndex: 0)
        ]);
    }

    private static Result<GameEvent[]> DecideSubmitGuess(GameState state, SubmitGuess command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound(command.GameId));

        if (state.Phase != GamePhase.Question)
            return new Err(new GameNotStarted(command.GameId));

        if (!state.Players.Any(p => p.PlayerId == command.PlayerId))
            return new Err(new PlayerNotInGame(command.PlayerId, command.GameId));

        if (command.Guess < 0 || command.Guess > 100)
            return new Err(new GuessOutOfRange(command.Guess, 0, 100));

        if (state.CurrentGuesses.ContainsKey(command.PlayerId))
            return new Err(new AlreadyGuessed(command.PlayerId, state.CurrentQuestionIndex));
        
        var events = new List<GameEvent>
        {
            new GuessSubmitted(state.GameId!, command.PlayerId, command.Guess, state.CurrentQuestionIndex)
        };
        
        // Check if all players have guessed
        var newGuessCount = state.CurrentGuesses.Count + 1;
        if (newGuessCount == state.Players.Count)
        {
            events.Add(new AllGuessesSubmitted(state.GameId!, state.CurrentQuestionIndex));
            // Note: QuestionScored would be triggered by a separate process that knows the correct answer
        }
        
        return new Ok<GameEvent[]>([..events]);
    }
    
    /// <summary>
    /// Fold a sequence of events into final state.
    /// </summary>
    public static GameState Fold(IEnumerable<GameEvent> events) =>
        events.Aggregate(GameState.Initial, Evolve);
}

/// <summary>
/// Context provides external dependencies to the Decider.
/// This allows the Decider to remain pure while still generating IDs, etc.
/// </summary>
public record GameContext(
    Func<string> NewId,
    Func<string> NewJoinCode,
    Func<DateTimeOffset> Now
)
{
    public static GameContext Default => new(
        NewId: () => Guid.NewGuid().ToString("N")[..8],
        NewJoinCode: () => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
        Now: () => DateTimeOffset.UtcNow
    );
}

/// <summary>
/// Railway-Oriented Result: an Ok track or an Err track (see ADR 006).
/// Represented as a native C# 15 union type (LangVersion preview, .NET 11).
/// Callers pattern-match the cases exhaustively (no default arm).
/// </summary>
public record Ok<T>(T Value);
public record Err(GameError Error);
public union Result<T>(Ok<T>, Err);
