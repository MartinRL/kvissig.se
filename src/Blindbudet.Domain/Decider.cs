namespace Blindbudet.Domain;

/// <summary>
/// The Decider contains two pure, total, synchronous functions:
/// - Evolve: (State, Event) -> State
/// - Decide: (State, Command, AuctionContext) -> Result&lt;Event[]&gt;
///
/// Both use exhaustive union switches (no default arm). Business failures are values on the
/// Result failure track, never thrown exceptions (ROP; see ADR 006). Sister to MEM's Decider
/// — a second, independent Decider. HIGHEST total wins (the OPPOSITE of MEM).
/// </summary>
public static class Decider
{
    /// <summary>
    /// Evolve applies an event to produce new state. Pure, no side effects.
    /// </summary>
    public static AuctionState Evolve(AuctionState state, AuctionEvent @event) =>
        @event switch
        {
            AuctionOpened e => state with
            {
                GameId = e.GameId,
                JoinCode = e.JoinCode,
                PackId = e.PackId,
                HostPlayerId = e.HostPlayerId,
                Phase = AuctionPhase.Lobby,
                Players = [new Player(e.HostPlayerId, e.HostName, IsHost: true)],
                Lots = e.Lots.Select(l => new LotRound { Lot = l }).ToList()
            },

            PlayerJoined e => state with
            {
                Players = [.. state.Players, new Player(e.PlayerId, e.PlayerName, IsHost: false)]
            },

            AuctionStarted e => state with
            {
                Phase = AuctionPhase.Started,
                CurrentLotIndex = e.FirstLotIndex
            },

            BidPlaced e => state with
            {
                // Bids fold in event-log order (Dictionary preserves insertion order absent
                // removals — which never happen), so the earliest top bidder wins a tie.
                Lots = MapLot(state.Lots, e.LotIndex, l => l with
                {
                    Bids = new Dictionary<Guid, decimal>(l.Bids) { [e.PlayerId] = e.Amount }
                })
            },

            LotRevealed e => state with
            {
                Lots = MapLot(state.Lots, e.LotIndex, l => l with
                {
                    TrueWorth = e.TrueWorth,
                    WinnerId = e.WinnerId,
                    PricePaid = e.PricePaid,
                    Resolved = true
                })
            },

            RoundScored e => state with
            {
                Lots = MapLot(state.Lots, e.LotIndex, l => l with
                {
                    Profits = new Dictionary<Guid, int>(l.Profits) { [e.PlayerId] = e.Profit }
                })
            },

            NextLotStarted e => state with
            {
                CurrentLotIndex = e.LotIndex
            },

            AuctionEnded e => state with
            {
                Phase = AuctionPhase.Ended,
                FinalScoreboard = e.FinalScoreboard,
                WinnerIds = e.WinnerIds
            }
        };

    /// <summary>
    /// Decide validates a command against current state and produces events, or an error
    /// explaining the rejection.
    /// </summary>
    public static Result<AuctionEvent[]> Decide(AuctionState state, AuctionCommand command, AuctionContext context) =>
        command switch
        {
            OpenAuction c => DecideOpenAuction(c, context),
            JoinAuction c => DecideJoinAuction(state, c, context),
            StartAuction c => DecideStartAuction(state, c, context),
            PlaceBid c => DecidePlaceBid(state, c, context),
            RevealLot c => DecideRevealLot(state, c),
            AskNextLot c => DecideAskNextLot(state, c),
            EndAuction c => DecideEndAuction(state, c, context)
        };

    private static Result<AuctionEvent[]> DecideOpenAuction(OpenAuction command, AuctionContext context)
    {
        var pack = context.FindPack(command.PackId);
        if (pack is null)
            return new Err(new AuctionPackNotFound());

        var gameId = context.NewGuid();
        var hostPlayerId = context.NewGuid();
        var joinCode = context.NewGuid();

        return new Ok<AuctionEvent[]>([
            new AuctionOpened(gameId, hostPlayerId, command.HostName, joinCode, command.PackId, pack.Lots, context.Now())
        ]);
    }

    private static Result<AuctionEvent[]> DecideJoinAuction(AuctionState state, JoinAuction command, AuctionContext context)
    {
        if (state.Phase == AuctionPhase.NotCreated)
            return new Err(new AuctionNotFound());

        if (state.Phase != AuctionPhase.Lobby)
            return new Err(new AuctionAlreadyStarted());

        if (state.Players.Any(p => p.Name == command.PlayerName))
            return new Err(new NameAlreadyTaken());

        var playerId = context.NewGuid();

        return new Ok<AuctionEvent[]>([
            new PlayerJoined(state.GameId, playerId, command.PlayerName, context.Now())
        ]);
    }

    private static Result<AuctionEvent[]> DecideStartAuction(AuctionState state, StartAuction command, AuctionContext context)
    {
        if (state.Phase == AuctionPhase.NotCreated)
            return new Err(new AuctionNotFound());

        if (state.Players.Count < 2)
            return new Err(new NotEnoughPlayers());

        return new Ok<AuctionEvent[]>([
            new AuctionStarted(state.GameId, FirstLotIndex: 0, context.Now())
        ]);
    }

    private static Result<AuctionEvent[]> DecidePlaceBid(AuctionState state, PlaceBid command, AuctionContext context)
    {
        if (state.Phase == AuctionPhase.NotCreated)
            return new Err(new AuctionNotFound());

        if (command.Amount < 0)
            return new Err(new BidNegative());

        // Resolved is checked before AlreadyBid: a resolved lot already holds the winner's
        // bid, so bidding on it is "closed", not "you bid twice".
        if (state.Lots[command.LotIndex].Resolved)
            return new Err(new LotAlreadyResolved());

        if (state.Lots[command.LotIndex].Bids.ContainsKey(command.PlayerId))
            return new Err(new AlreadyBid());

        return new Ok<AuctionEvent[]>([
            new BidPlaced(state.GameId, command.PlayerId, command.LotIndex, command.Amount, context.Now())
        ]);
    }

    private static Result<AuctionEvent[]> DecideRevealLot(AuctionState state, RevealLot command)
    {
        if (!state.AllBidsIn(command.LotIndex))
            return new Err(new NotAllBidsIn());

        if (state.Lots[command.LotIndex].Resolved)
            return new Err(new LotAlreadyResolved());

        var round = state.Lots[command.LotIndex];
        var trueWorth = round.Lot.TrueWorth;

        // Highest bid wins; ties -> earliest BidPlaced (bids folded in event-log order, so the
        // first occurrence of the max in iteration order is the earliest bid). First-price:
        // the winner pays their own bid.
        var winnerId = Guid.Empty;
        var pricePaid = decimal.MinValue;
        foreach (var (playerId, amount) in round.Bids)
            if (amount > pricePaid)
            {
                pricePaid = amount;
                winnerId = playerId;
            }

        var events = new List<AuctionEvent>
        {
            new LotRevealed(state.GameId, command.LotIndex, trueWorth, winnerId, pricePaid)
        };

        foreach (var player in state.Players)
        {
            var isWinner = player.PlayerId == winnerId;
            var profit = isWinner ? (int)Math.Round(trueWorth - pricePaid, MidpointRounding.AwayFromZero) : 0;
            var totalScore = state.TotalScore(player.PlayerId) + profit;

            events.Add(new RoundScored(state.GameId, command.LotIndex, player.PlayerId, profit, totalScore));
        }

        return new Ok<AuctionEvent[]>([.. events]);
    }

    private static Result<AuctionEvent[]> DecideAskNextLot(AuctionState state, AskNextLot command) =>
        new Ok<AuctionEvent[]>([
            new NextLotStarted(state.GameId, state.CurrentLotIndex + 1)
        ]);

    private static Result<AuctionEvent[]> DecideEndAuction(AuctionState state, EndAuction command, AuctionContext context)
    {
        var scoreboard = state.Players
            .Select(p => new ScoreboardEntry(p.PlayerId, p.Name, state.TotalScore(p.PlayerId)))
            .ToList();

        // HIGHEST total wins — the OPPOSITE of MEM (lowest). Ties share the win.
        var maxTotal = scoreboard.Max(e => e.TotalScore);
        var winnerIds = scoreboard
            .Where(e => e.TotalScore == maxTotal)
            .Select(e => e.PlayerId)
            .ToList();

        return new Ok<AuctionEvent[]>([
            new AuctionEnded(state.GameId, scoreboard, winnerIds, context.Now())
        ]);
    }

    /// <summary>
    /// Fold a sequence of events into final state.
    /// </summary>
    public static AuctionState Fold(IEnumerable<AuctionEvent> events) =>
        events.Aggregate(AuctionState.Initial, Evolve);

    private static IReadOnlyList<LotRound> MapLot(
        IReadOnlyList<LotRound> lots,
        int index,
        Func<LotRound, LotRound> map) =>
        lots.Select((l, i) => i == index ? map(l) : l).ToList();
}

/// <summary>
/// Context provides external dependencies to the Decider so it stays pure: a Guid generator,
/// a clock, and the lot-pack resolver (OpenAuction resolves the chosen pack via FindPack).
/// Mirrors MEM's GameContext form (a sister, not the same type).
/// </summary>
public record AuctionContext(
    Func<Guid> NewGuid,
    Func<DateTimeOffset> Now,
    Func<string, AuctionPack?> FindPack
)
{
    public static AuctionContext Default => new(
        NewGuid: Guid.NewGuid,
        Now: () => DateTimeOffset.UtcNow,
        FindPack: _ => null
    );
}

/// <summary>
/// Railway-Oriented Result: an Ok track or an Err track (see ADR 006). Native C# 15 union.
///
/// ponytail: NOT MEM's Result — that union's Err is bound to GameError. Reusing it would
/// require Blindbudet errors to be GameErrors (couples the two games) or a shared error-base
/// extraction touching MEM (forbidden). A 3-line sister union is the lazier correct choice.
/// </summary>
public record Ok<T>(T Value);
public record Err(AuctionError Error);
public union Result<T>(Ok<T>, Err);
