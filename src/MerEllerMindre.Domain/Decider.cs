namespace MerEllerMindre.Domain;

/// <summary>
/// The Decider contains two pure, total, synchronous functions:
/// - Evolve: (State, Event) -> State
/// - Decide: (State, Command, GameContext) -> Result&lt;Event[]&gt;
///
/// Both use exhaustive union switches (no default arm). Business failures are values on
/// the Result failure track, never thrown exceptions (ROP; see ADR 006).
/// </summary>
public static class Decider
{
    /// <summary>
    /// Evolve applies an event to produce new state. Pure, no side effects.
    /// </summary>
    public static GameState Evolve(GameState state, GameEvent @event) =>
        @event switch
        {
            LobbyOpened e => state with
            {
                GameId = e.GameId,
                JoinCode = e.JoinCode,
                QuestionPackId = e.QuestionPackId,
                HostPlayerId = e.HostPlayerId,
                Phase = GamePhase.Lobby,
                Players = [new Player(e.HostPlayerId, e.HostName, IsHost: true)],
                Questions = e.Questions.Select(q => new QuestionRound { Card = q }).ToList()
            },

            PlayerJoined e => state with
            {
                Players = [.. state.Players, new Player(e.PlayerId, e.PlayerName, IsHost: false)]
            },

            GameStarted e => state with
            {
                Phase = GamePhase.Started,
                CurrentQuestionIndex = e.FirstQuestionIndex
            },

            GuessSubmitted e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    Guesses = new Dictionary<Guid, Guess>(q.Guesses)
                    {
                        [e.PlayerId] = new Guess(e.Direction, e.GuessedDifference)
                    }
                })
            },

            QuestionAnswered e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    CorrectDirection = e.CorrectDirection,
                    CorrectDifference = e.CorrectDifference,
                    Scored = true
                })
            },

            QuestionScored e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    RoundScores = new Dictionary<Guid, int>(q.RoundScores)
                    {
                        [e.PlayerId] = e.RoundScore
                    }
                })
            },

            NextQuestionStarted e => state with
            {
                CurrentQuestionIndex = e.QuestionIndex
            },

            GameEnded => state with
            {
                Phase = GamePhase.Ended
            }
        };

    /// <summary>
    /// Decide validates a command against current state and produces events,
    /// or an error explaining the rejection.
    /// </summary>
    public static Result<GameEvent[]> Decide(GameState state, GameCommand command, GameContext context) =>
        command switch
        {
            OpenLobby c => DecideOpenLobby(c, context),
            JoinGame c => DecideJoinGame(state, c, context),
            StartGame c => DecideStartGame(state, c, context),
            SubmitGuess c => DecideSubmitGuess(state, c, context),
            ScoreQuestion c => DecideScoreQuestion(state, c),
            AskNextQuestion c => DecideAskNextQuestion(state, c),
            EndGame c => DecideEndGame(state, c, context)
        };

    private static Result<GameEvent[]> DecideOpenLobby(OpenLobby command, GameContext context)
    {
        var pack = context.FindPack(command.QuestionPackId);
        if (pack is null)
            return new Err(new QuestionPackNotFound());

        var gameId = context.NewGuid();
        var hostPlayerId = context.NewGuid();
        var joinCode = context.NewGuid();

        return new Ok<GameEvent[]>([
            new LobbyOpened(gameId, hostPlayerId, command.HostName, joinCode, command.QuestionPackId, pack.Questions, context.Now())
        ]);
    }

    private static Result<GameEvent[]> DecideJoinGame(GameState state, JoinGame command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound());

        if (state.Phase != GamePhase.Lobby)
            return new Err(new GameAlreadyStarted());

        if (state.Players.Any(p => p.Name == command.PlayerName))
            return new Err(new NameAlreadyTaken());

        var playerId = context.NewGuid();

        return new Ok<GameEvent[]>([
            new PlayerJoined(state.GameId, playerId, command.PlayerName, context.Now())
        ]);
    }

    private static Result<GameEvent[]> DecideStartGame(GameState state, StartGame command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound());

        if (state.Players.Count < 2)
            return new Err(new NotEnoughPlayers());

        return new Ok<GameEvent[]>([
            new GameStarted(state.GameId, FirstQuestionIndex: 0, context.Now())
        ]);
    }

    private static Result<GameEvent[]> DecideSubmitGuess(GameState state, SubmitGuess command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound());

        if (state.Phase != GamePhase.Started)
            return new Err(new GameNotStarted());

        if (!state.Players.Any(p => p.PlayerId == command.PlayerId))
            return new Err(new PlayerNotInGame());

        if (state.Questions[state.CurrentQuestionIndex].Guesses.ContainsKey(command.PlayerId))
            return new Err(new AlreadyGuessed());

        if (command.GuessedDifference < 0)
            return new Err(new DifferenceOutOfRange());

        return new Ok<GameEvent[]>([
            new GuessSubmitted(state.GameId, command.PlayerId, state.CurrentQuestionIndex, command.Direction, command.GuessedDifference, context.Now())
        ]);
    }

    private static Result<GameEvent[]> DecideScoreQuestion(GameState state, ScoreQuestion command)
    {
        if (!state.AllGuessesIn(command.QuestionIndex))
            return new Err(new NotAllGuessesIn());

        if (state.Questions[command.QuestionIndex].Scored)
            return new Err(new QuestionAlreadyScored());

        var round = state.Questions[command.QuestionIndex];
        var a = round.Card.ValueA;
        var b = round.Card.ValueB;
        var mx = Math.Max(a, b);

        var correctDirection = a >= b ? Direction.Mer : Direction.Mindre;
        var correctDifference = (byte)Math.Round(Math.Abs(a - b) / mx * 100, MidpointRounding.AwayFromZero);

        var events = new List<GameEvent>
        {
            new QuestionAnswered(state.GameId, command.QuestionIndex, correctDirection, correctDifference)
        };

        foreach (var player in state.Players)
        {
            var guess = round.Guesses[player.PlayerId];
            var normalized = (byte)Math.Min(100m, Math.Round(guess.GuessedDifference / mx * 100, MidpointRounding.AwayFromZero));
            var directionCorrect = guess.Direction == correctDirection;
            var differencePoints = (byte)Math.Abs(normalized - correctDifference);
            var bonus = directionCorrect ? -10 : 0;
            var roundScore = differencePoints + bonus;
            var totalScore = state.TotalScore(player.PlayerId) + roundScore;

            events.Add(new QuestionScored(
                state.GameId,
                command.QuestionIndex,
                player.PlayerId,
                guess.Direction,
                guess.GuessedDifference,
                normalized,
                directionCorrect,
                differencePoints,
                bonus,
                roundScore,
                totalScore));
        }

        return new Ok<GameEvent[]>([.. events]);
    }

    private static Result<GameEvent[]> DecideAskNextQuestion(GameState state, AskNextQuestion command) =>
        new Ok<GameEvent[]>([
            new NextQuestionStarted(state.GameId, state.CurrentQuestionIndex + 1)
        ]);

    private static Result<GameEvent[]> DecideEndGame(GameState state, EndGame command, GameContext context)
    {
        var scoreboard = state.Players
            .Select(p => new ScoreboardEntry(p.PlayerId, p.Name, state.TotalScore(p.PlayerId)))
            .ToList();

        var minTotal = scoreboard.Min(e => e.TotalScore);
        var winnerIds = scoreboard
            .Where(e => e.TotalScore == minTotal)
            .Select(e => e.PlayerId)
            .ToList();

        return new Ok<GameEvent[]>([
            new GameEnded(state.GameId, scoreboard, winnerIds, context.Now())
        ]);
    }

    /// <summary>
    /// Fold a sequence of events into final state.
    /// </summary>
    public static GameState Fold(IEnumerable<GameEvent> events) =>
        events.Aggregate(GameState.Initial, Evolve);

    private static IReadOnlyList<QuestionRound> MapQuestion(
        IReadOnlyList<QuestionRound> questions,
        int index,
        Func<QuestionRound, QuestionRound> map) =>
        questions.Select((q, i) => i == index ? map(q) : q).ToList();
}

/// <summary>
/// Context provides external dependencies to the Decider so it stays pure: a Guid
/// generator, a clock, and the question-pack resolver (OpenLobby resolves the chosen
/// pack via FindPack).
/// </summary>
public record GameContext(
    Func<Guid> NewGuid,
    Func<DateTimeOffset> Now,
    Func<string, QuestionPack?> FindPack
)
{
    public static GameContext Default => new(
        NewGuid: Guid.NewGuid,
        Now: () => DateTimeOffset.UtcNow,
        FindPack: _ => null
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
