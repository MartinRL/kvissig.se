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
    /// Number of question cards a single game plays, drawn balanced from the pack.
    /// Prod decks play the full round; concept ("mini") packs play a short round to test
    /// a game idea cheaply.
    /// </summary>
    // ponytail: "mini"-slug marker picks the size; promote = rename without "mini" + grow
    // the pack to 1085, which auto-makes it a 21-question prod deck.
    public const int FullGameSize = 21;
    public const int MiniGameSize = 7;

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

            DirectionSubmitted e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    Directions = new Dictionary<Guid, Direction>(q.Directions)
                    {
                        [e.PlayerId] = e.Direction
                    }
                })
            },

            QuestionDirectionRevealed e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    CorrectDirection = e.CorrectDirection
                })
            },

            DirectionScored e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    DirectionScores = new Dictionary<Guid, int>(q.DirectionScores)
                    {
                        [e.PlayerId] = e.BonusPoints
                    }
                })
            },

            DifferenceSubmitted e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    Differences = new Dictionary<Guid, decimal>(q.Differences)
                    {
                        [e.PlayerId] = e.GuessedDifference
                    }
                })
            },

            QuestionDifferenceRevealed e => state with
            {
                Questions = MapQuestion(state.Questions, e.QuestionIndex, q => q with
                {
                    CorrectDifference = e.CorrectDifference,
                    Scored = true
                })
            },

            DifferenceScored e => state with
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

            GameEnded e => state with
            {
                Phase = GamePhase.Ended,
                FinalScoreboard = e.FinalScoreboard,
                WinnerIds = e.WinnerIds
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
            SubmitDirection c => DecideSubmitDirection(state, c, context),
            RevealDirection c => DecideRevealDirection(state, c),
            SubmitDifference c => DecideSubmitDifference(state, c, context),
            ScoreDifference c => DecideScoreDifference(state, c),
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

        var count = command.QuestionPackId.Contains("mini") ? MiniGameSize : FullGameSize;
        var questions = QuestionSelection.PickBalanced(pack.Questions, count, context.NextRandom);

        return new Ok<GameEvent[]>([
            new LobbyOpened(gameId, hostPlayerId, command.HostName, joinCode, command.QuestionPackId, questions, context.Now())
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

    private static Result<GameEvent[]> DecideSubmitDirection(GameState state, SubmitDirection command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound());

        if (state.Phase != GamePhase.Started)
            return new Err(new GameNotStarted());

        if (!state.Players.Any(p => p.PlayerId == command.PlayerId))
            return new Err(new PlayerNotInGame());

        if (state.Questions[state.CurrentQuestionIndex].Directions.ContainsKey(command.PlayerId))
            return new Err(new AlreadySubmittedDirection());

        return new Ok<GameEvent[]>([
            new DirectionSubmitted(state.GameId, command.PlayerId, state.CurrentQuestionIndex, command.Direction, context.Now())
        ]);
    }

    private static Result<GameEvent[]> DecideRevealDirection(GameState state, RevealDirection command)
    {
        if (!state.AllDirectionsIn(command.QuestionIndex))
            return new Err(new NotAllDirectionsIn());

        if (state.DirectionRevealed(command.QuestionIndex))
            return new Err(new DirectionAlreadyRevealed());

        var round = state.Questions[command.QuestionIndex];
        var correctDirection = round.Card.ValueA >= round.Card.ValueB ? Direction.Mer : Direction.Mindre;

        var events = new List<GameEvent>
        {
            new QuestionDirectionRevealed(state.GameId, command.QuestionIndex, correctDirection)
        };

        foreach (var player in state.Players)
        {
            var guessed = round.Directions[player.PlayerId];
            var directionCorrect = guessed == correctDirection;
            var bonus = directionCorrect ? -10 : 0;

            events.Add(new DirectionScored(
                state.GameId,
                command.QuestionIndex,
                player.PlayerId,
                guessed,
                directionCorrect,
                bonus));
        }

        return new Ok<GameEvent[]>([.. events]);
    }

    private static Result<GameEvent[]> DecideSubmitDifference(GameState state, SubmitDifference command, GameContext context)
    {
        if (state.Phase == GamePhase.NotCreated)
            return new Err(new GameNotFound());

        if (state.Phase != GamePhase.Started)
            return new Err(new GameNotStarted());

        if (!state.Players.Any(p => p.PlayerId == command.PlayerId))
            return new Err(new PlayerNotInGame());

        if (!state.DirectionRevealed(state.CurrentQuestionIndex))
            return new Err(new DirectionNotRevealed());

        if (state.Questions[state.CurrentQuestionIndex].Differences.ContainsKey(command.PlayerId))
            return new Err(new AlreadySubmittedDifference());

        if (command.GuessedDifference < 0)
            return new Err(new DifferenceOutOfRange());

        return new Ok<GameEvent[]>([
            new DifferenceSubmitted(state.GameId, command.PlayerId, state.CurrentQuestionIndex, command.GuessedDifference, context.Now())
        ]);
    }

    /// <summary>
    /// Normalizes a raw difference into the 0-100 scale used for scoring, clamped at 100.
    /// Shared by the facit and per-guess normalization so the results screen can show the
    /// same integer the scoring used.
    /// </summary>
    public static byte NormalizeDifference(decimal value, decimal mx) =>
        mx <= 0 ? (byte)0
        : (byte)Math.Min(100m, Math.Round(value / mx * 100, MidpointRounding.AwayFromZero));

    private static Result<GameEvent[]> DecideScoreDifference(GameState state, ScoreDifference command)
    {
        if (!state.AllDifferencesIn(command.QuestionIndex))
            return new Err(new NotAllDifferencesIn());

        if (state.Questions[command.QuestionIndex].Scored)
            return new Err(new QuestionAlreadyScored());

        var round = state.Questions[command.QuestionIndex];
        var a = round.Card.ValueA;
        var b = round.Card.ValueB;
        var mx = Math.Max(a, b);

        var correctDifference = NormalizeDifference(Math.Abs(a - b), mx);

        var events = new List<GameEvent>
        {
            new QuestionDifferenceRevealed(state.GameId, command.QuestionIndex, correctDifference)
        };

        foreach (var player in state.Players)
        {
            var guessedDifference = round.Differences[player.PlayerId];
            var normalized = NormalizeDifference(guessedDifference, mx);
            var differencePoints = (byte)Math.Abs(normalized - correctDifference);
            var bonus = round.DirectionScores.TryGetValue(player.PlayerId, out var b1) ? b1 : 0;
            var roundScore = differencePoints + bonus;
            var totalScore = state.TotalScore(player.PlayerId) + roundScore;

            events.Add(new DifferenceScored(
                state.GameId,
                command.QuestionIndex,
                player.PlayerId,
                guessedDifference,
                normalized,
                differencePoints,
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
    Func<string, QuestionPack?> FindPack,
    Func<int, int> NextRandom
)
{
    public static GameContext Default => new(
        NewGuid: Guid.NewGuid,
        Now: () => DateTimeOffset.UtcNow,
        FindPack: _ => null,
        NextRandom: Random.Shared.Next
    );
}

/// <summary>
/// Picks a difficulty-band-balanced subset of question cards for a single game. Balance is
/// on the difficulty band (band = NormalizeDifference(|A-B|, max(A,B)), the same math
/// ScoreDifference uses) PLUS each item (itemA/itemB) appears at most once per game PLUS each
/// topic (questionText category) appears at most ceil(count / distinct-topics) times so one
/// category can't dominate a round; all best-effort, falls back on repetition only if the pool
/// can't supply enough distinct cards. Final order is shuffled so bands don't cluster. Pure: RNG is injected as `next`
/// (an exclusive-upper-bound generator, like Random.Next(n)).
/// </summary>
public static class QuestionSelection
{
    // Target band distribution from specs/question-style-guide.md:
    // [0-20]=15%, (20-60]=40%, (60-85]=30%, (85-100]=15%.
    private static readonly int[] BandWeights = [15, 40, 30, 15];

    public static IReadOnlyList<Question> PickBalanced(
        IReadOnlyList<Question> pool, int count, Func<int, int> next)
    {
        // ponytail: small pool = use all, as-is (keeps the current 10-card pack and the
        // 2-card test fixtures byte-identical until the pool exceeds count).
        if (pool.Count <= count)
            return pool;

        var bands = BucketIntoBands(pool);
        var quotas = Apportion(count, BandWeights);
        var picker = new BandPicker(count, TopicCap(pool, count));

        // Per band, take up to its quota (item-distinct + under the topic cap); anything
        // not taken (over quota or guard-rejected) drops into leftover for the fill passes.
        var leftover = new List<Question>();
        for (var b = 0; b < bands.Length; b++)
        {
            Shuffle(bands[b], next);
            var taken = 0;
            foreach (var q in bands[b])
            {
                if (taken < quotas[b] && picker.TryTake(q)) taken++;
                else leftover.Add(q);
            }
        }

        Shuffle(leftover, next);
        picker.Fill(leftover); // fill band deficits, still item-distinct

        // ponytail: item-distinct is best-effort. If the pool can't yield `count` item-distinct
        // cards (never hits the live pack) — fill without the guard so the game isn't short.
        picker.TopUp(leftover);

        // Shuffle the final selection so bands don't cluster in play order.
        Shuffle(picker.Picked, next);
        return picker.Picked;
    }

    private static List<Question>[] BucketIntoBands(IReadOnlyList<Question> pool)
    {
        var bands = new List<Question>[BandWeights.Length];
        for (var b = 0; b < bands.Length; b++)
            bands[b] = [];
        foreach (var q in pool)
            bands[BandOf(q)].Add(q);
        return bands;
    }

    // At most ceil(count / distinct-topics) cards per topic, so no category dominates a round.
    private static int TopicCap(IReadOnlyList<Question> pool, int count)
    {
        var distinctTopics = pool.Select(q => q.QuestionText)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return (int)Math.Ceiling((double)count / Math.Max(1, distinctTopics));
    }

    /// <summary>
    /// The mutable selection state that the three pick passes share: the chosen cards plus the
    /// item- and topic-distinctness bookkeeping. Kept together so no pass leaks the guards.
    /// </summary>
    private sealed class BandPicker(int count, int topicCap)
    {
        public List<Question> Picked { get; } = [];
        private readonly HashSet<Question> _seen = [];
        private readonly HashSet<string> _usedItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _topicCounts = new(StringComparer.OrdinalIgnoreCase);

        private bool Full => Picked.Count >= count;

        // Guarded take: only if both items are unused and the topic is under its cap.
        public bool TryTake(Question q)
        {
            if (_usedItems.Contains(q.ItemA) || _usedItems.Contains(q.ItemB)) return false;
            if (_topicCounts.GetValueOrDefault(q.QuestionText) >= topicCap) return false;
            _usedItems.Add(q.ItemA);
            _usedItems.Add(q.ItemB);
            _topicCounts[q.QuestionText] = _topicCounts.GetValueOrDefault(q.QuestionText) + 1;
            Add(q);
            return true;
        }

        // Fill any deficit from the pool, still item-distinct, stopping once full.
        public void Fill(IEnumerable<Question> pool)
        {
            foreach (var q in pool)
            {
                if (Full) return;
                TryTake(q);
            }
        }

        // Last resort: take any not-yet-picked card, ignoring the item/topic guards.
        public void TopUp(IEnumerable<Question> pool)
        {
            foreach (var q in pool)
            {
                if (Full) return;
                Add(q);
            }
        }

        private void Add(Question q)
        {
            if (_seen.Add(q))
                Picked.Add(q);
        }
    }

    private static int BandOf(Question q)
    {
        var norm = Decider.NormalizeDifference(Math.Abs(q.ValueA - q.ValueB), Math.Max(q.ValueA, q.ValueB));
        return norm switch
        {
            <= 20 => 0,
            <= 60 => 1,
            <= 85 => 2,
            _ => 3
        };
    }

    // Largest-remainder apportionment of `count` seats over the integer weights.
    private static int[] Apportion(int count, int[] weights)
    {
        var totalWeight = weights.Sum();
        var quotas = new int[weights.Length];
        var remainders = new decimal[weights.Length];
        var assigned = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            var exact = (decimal)count * weights[i] / totalWeight;
            quotas[i] = (int)Math.Floor(exact);
            remainders[i] = exact - quotas[i];
            assigned += quotas[i];
        }

        foreach (var i in Enumerable.Range(0, weights.Length).OrderByDescending(i => remainders[i]))
        {
            if (assigned >= count)
                break;
            quotas[i]++;
            assigned++;
        }

        return quotas;
    }

    private static void Shuffle<T>(IList<T> list, Func<int, int> next)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

/// <summary>
/// Railway-Oriented Result: an Ok track or an Err track (see ADR 006).
/// Represented as a native C# 15 union type (LangVersion preview, .NET 11).
/// Callers pattern-match the cases exhaustively (no default arm).
/// </summary>
public record Ok<T>(T Value);
public record Err(GameError Error);
public union Result<T>(Ok<T>, Err);
