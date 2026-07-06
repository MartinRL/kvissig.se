namespace TankTillTusen.Domain;

/// <summary>
/// The Decider contains two pure, total, synchronous functions:
/// - Evolve: (State, Event) -> State
/// - Decide: (State, Command, TankContext) -> Result&lt;Event[]&gt;
///
/// Both use exhaustive union switches (no default arm). Business failures are values on the
/// Result failure track, never thrown exceptions (ROP; see ADR 006). A THIRD Decider beside
/// MEM's and BlindBudet's — independent. LOWEST total wins (like MEM, OPPOSITE of BlindBudet).
/// </summary>
public static class Decider
{
    /// <summary>The hard per-round countdown: deadline = round.StartedAt + this many seconds.</summary>
    public const int CountdownSeconds = 45;

    /// <summary>How many puzzles a game generates (the default context stamps this many rounds).</summary>
    public const int RoundCount = 5;

    /// <summary>
    /// Evolve applies an event to produce new state. Pure, no side effects.
    /// </summary>
    public static TankState Evolve(TankState state, TankEvent @event) =>
        @event switch
        {
            LobbyOpened e => state with
            {
                GameId = e.GameId,
                JoinCode = e.JoinCode,
                HostPlayerId = e.HostPlayerId,
                Phase = TankPhase.Lobby,
                Players = [new Player(e.HostPlayerId, e.HostName, IsHost: true)],
                Rounds = e.Puzzles.Select(p => new PuzzleRound { Puzzle = p }).ToList()
            },

            PlayerJoined e => state with
            {
                Players = [.. state.Players, new Player(e.PlayerId, e.PlayerName, IsHost: false)]
            },

            GameStarted e => state with
            {
                Phase = TankPhase.Started,
                CurrentRoundIndex = e.FirstRoundIndex,
                Rounds = MapRound(state.Rounds, e.FirstRoundIndex, r => r with { StartedAt = e.StartedAt })
            },

            SolutionSubmitted e => state with
            {
                // Solutions fold in event-log order (Dictionary preserves insertion order absent
                // removals — which never happen).
                Rounds = MapRound(state.Rounds, e.RoundIndex, r => r with
                {
                    Solutions = new Dictionary<Guid, Solution>(r.Solutions) { [e.PlayerId] = e.Solution }
                })
            },

            PuzzleRevealed e => state with
            {
                // Reveal fires once per round score → also the moment the round is marked scored.
                Rounds = MapRound(state.Rounds, e.RoundIndex, r => r with
                {
                    SampleSolution = e.SampleSolution,
                    Scored = true
                })
            },

            RoundScored e => state with
            {
                Rounds = MapRound(state.Rounds, e.RoundIndex, r => r with
                {
                    ReachedValues = e.ReachedValue is { } v
                        ? new Dictionary<Guid, int>(r.ReachedValues) { [e.PlayerId] = v }
                        : r.ReachedValues,
                    RoundScores = new Dictionary<Guid, int>(r.RoundScores) { [e.PlayerId] = e.RoundScore }
                })
            },

            NextPuzzleStarted e => state with
            {
                CurrentRoundIndex = e.RoundIndex,
                Rounds = MapRound(state.Rounds, e.RoundIndex, r => r with { StartedAt = e.StartedAt })
            },

            GameEnded e => state with
            {
                Phase = TankPhase.Ended,
                FinalScoreboard = e.FinalScoreboard,
                WinnerIds = e.WinnerIds
            }
        };

    /// <summary>
    /// Decide validates a command against current state and produces events, or an error
    /// explaining the rejection.
    /// </summary>
    public static Result<TankEvent[]> Decide(TankState state, TankCommand command, TankContext context) =>
        command switch
        {
            OpenLobby c => DecideOpenLobby(c, context),
            JoinGame c => DecideJoinGame(state, c, context),
            StartGame c => DecideStartGame(state, c, context),
            SubmitSolution c => DecideSubmitSolution(state, c, context),
            ScoreRound c => DecideScoreRound(state, c, context),
            AskNextPuzzle c => DecideAskNextPuzzle(state, c, context),
            EndGame c => DecideEndGame(state, c, context)
        };

    private static Result<TankEvent[]> DecideOpenLobby(OpenLobby command, TankContext context)
    {
        var gameId = context.NewGuid();
        var hostPlayerId = context.NewGuid();
        var joinCode = context.NewGuid();
        var puzzles = context.GeneratePuzzles();

        return new Ok<TankEvent[]>([
            new LobbyOpened(gameId, hostPlayerId, command.HostName, joinCode, puzzles, context.Now())
        ]);
    }

    private static Result<TankEvent[]> DecideJoinGame(TankState state, JoinGame command, TankContext context)
    {
        if (state.Phase == TankPhase.NotCreated)
            return new Err(new GameNotFound());

        if (state.Phase != TankPhase.Lobby)
            return new Err(new GameAlreadyStarted());

        if (state.Players.Any(p => p.Name == command.PlayerName))
            return new Err(new NameAlreadyTaken());

        var playerId = context.NewGuid();

        return new Ok<TankEvent[]>([
            new PlayerJoined(state.GameId, playerId, command.PlayerName, context.Now())
        ]);
    }

    private static Result<TankEvent[]> DecideStartGame(TankState state, StartGame command, TankContext context)
    {
        if (state.Phase == TankPhase.NotCreated)
            return new Err(new GameNotFound());

        if (state.Players.Count < 2)
            return new Err(new NotEnoughPlayers());

        return new Ok<TankEvent[]>([
            new GameStarted(state.GameId, FirstRoundIndex: 0, context.Now())
        ]);
    }

    private static Result<TankEvent[]> DecideSubmitSolution(TankState state, SubmitSolution command, TankContext context)
    {
        if (state.Phase == TankPhase.NotCreated)
            return new Err(new GameNotFound());

        var round = state.Rounds[command.RoundIndex];

        // Scored is checked before AlreadySubmitted: a scored round is closed, not "you submitted
        // twice". DeadlinePassed then guards a late build before we spend effort replaying it.
        if (round.Scored)
            return new Err(new RoundAlreadyScored());

        if (round.Solutions.ContainsKey(command.PlayerId))
            return new Err(new AlreadySubmitted());

        if (state.DeadlinePassed(command.RoundIndex, context.Now()))
            return new Err(new DeadlinePassed());

        // Trust boundary: re-validate the build by replay. Null = illegal (operand missing/reused,
        // ÷ uneven, result not > 0, answerIndex out of range).
        if (SolutionValidator.Validate(round.Puzzle, command.Solution) is null)
            return new Err(new InvalidSolution());

        return new Ok<TankEvent[]>([
            new SolutionSubmitted(state.GameId, command.PlayerId, command.RoundIndex, command.Solution, context.Now())
        ]);
    }

    private static Result<TankEvent[]> DecideScoreRound(TankState state, ScoreRound command, TankContext context)
    {
        var round = state.Rounds[command.RoundIndex];

        if (round.Scored)
            return new Err(new RoundAlreadyScored());

        if (!state.ReadyToScore(command.RoundIndex, context.Now()))
            return new Err(new NotReadyToScore());

        var target = round.Puzzle.Target;

        var events = new List<TankEvent>
        {
            new PuzzleRevealed(state.GameId, command.RoundIndex, round.Puzzle.SampleSolution)
        };

        foreach (var player in state.Players)
        {
            // A non-submitter has no reachedValue and scores the worst (100). A stored solution is
            // already validated at submit, so replay yields a value; distance sets the score.
            var reached = round.Solutions.TryGetValue(player.PlayerId, out var solution)
                ? SolutionValidator.Validate(round.Puzzle, solution)
                : null;

            var roundScore = reached is { } value
                ? (value == target
                    ? -10                                   // exakt = perfektions-bonus (som MEM)
                    : (int)Math.Round(Math.Min(100m, Math.Abs(value - target) / (decimal)target * 100m), MidpointRounding.AwayFromZero))
                : 100;

            var totalScore = state.TotalScore(player.PlayerId) + roundScore;

            events.Add(new RoundScored(state.GameId, command.RoundIndex, player.PlayerId, reached, roundScore, totalScore));
        }

        return new Ok<TankEvent[]>([.. events]);
    }

    private static Result<TankEvent[]> DecideAskNextPuzzle(TankState state, AskNextPuzzle command, TankContext context) =>
        new Ok<TankEvent[]>([
            new NextPuzzleStarted(state.GameId, state.CurrentRoundIndex + 1, context.Now())
        ]);

    private static Result<TankEvent[]> DecideEndGame(TankState state, EndGame command, TankContext context)
    {
        var scoreboard = state.Players
            .Select(p => new ScoreboardEntry(p.PlayerId, p.Name, state.TotalScore(p.PlayerId)))
            .ToList();

        // LOWEST total wins — like MEM (OPPOSITE of BlindBudet). Ties share the win.
        var minTotal = scoreboard.Min(e => e.TotalScore);
        var winnerIds = scoreboard
            .Where(e => e.TotalScore == minTotal)
            .Select(e => e.PlayerId)
            .ToList();

        return new Ok<TankEvent[]>([
            new GameEnded(state.GameId, scoreboard, winnerIds, context.Now())
        ]);
    }

    /// <summary>
    /// Fold a sequence of events into final state.
    /// </summary>
    public static TankState Fold(IEnumerable<TankEvent> events) =>
        events.Aggregate(TankState.Initial, Evolve);

    private static IReadOnlyList<PuzzleRound> MapRound(
        IReadOnlyList<PuzzleRound> rounds,
        int index,
        Func<PuzzleRound, PuzzleRound> map) =>
        rounds.Select((r, i) => i == index ? map(r) : r).ToList();
}

/// <summary>
/// Context provides external dependencies to the Decider so it stays pure: a Guid generator, a
/// clock, and the puzzle-set generator (OpenLobby stamps a freshly generated set — the analog of
/// BlindBudet's FindPack, but the content is generated, not loaded from a CSV catalog).
/// </summary>
public record TankContext(
    Func<Guid> NewGuid,
    Func<DateTimeOffset> Now,
    Func<IReadOnlyList<Puzzle>> GeneratePuzzles
)
{
    public static TankContext Default => new(
        NewGuid: Guid.NewGuid,
        Now: () => DateTimeOffset.UtcNow,
        GeneratePuzzles: () => PuzzleGenerator.GenerateSet(Decider.RoundCount, Random.Shared.Next)
    );
}

/// <summary>
/// Railway-Oriented Result: an Ok track or an Err track (see ADR 006). Native C# 15 union.
///
/// ponytail: NOT MEM's or BlindBudet's Result — each union's Err is bound to that game's error
/// type. A 3-line sister union is the lazier correct choice over coupling the three games.
/// </summary>
public record Ok<T>(T Value);
public record Err(TankError Error);
public union Result<T>(Ok<T>, Err);
