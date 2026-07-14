namespace Blindbudet.Domain;

/// <summary>
/// The Decider contains two pure, total, synchronous functions:
/// - Evolve: (State, Event) -> State
/// - Decide: (State, Command, AuctionContext) -> Result&lt;Event[]&gt;
///
/// The exhaustive union switches are GENERATED from the emlang spec (Decider.g.cs, ADR 018);
/// this file holds the case BODIES as partial-method implementations — a new e:/c: in the
/// spec is a CS8795 compile error until its body is written here. Business failures are
/// values on the Result failure track, never thrown exceptions (ROP; see ADR 006). Sister to
/// MEM's Decider — a second, independent Decider. HIGHEST total wins (the OPPOSITE of MEM).
/// </summary>
public static partial class Decider
{
    /// <summary>
    /// A "mini" (concept-scale) pack is a large pool of lots sampled down to this many per game
    /// — mirrors MEM's MiniGameSize. Prod-size auction decks (no "mini" marker) play in full.
    /// </summary>
    public const int MiniAuctionSize = 7;

    private static partial AuctionState EvolveAuctionOpened(AuctionState state, AuctionOpened e) =>
        state with
        {
            GameId = e.GameId,
            JoinCode = e.JoinCode,
            PackId = e.PackId,
            HostPlayerId = e.HostPlayerId,
            Phase = AuctionPhase.Lobby,
            Players = [new Player(e.HostPlayerId, e.HostName, IsHost: true)],
            Lots = e.Lots.Select(l => new LotRound { Lot = l }).ToList()
        };

    private static partial AuctionState EvolvePlayerJoined(AuctionState state, PlayerJoined e) =>
        state with
        {
            Players = [.. state.Players, new Player(e.PlayerId, e.PlayerName, IsHost: false)]
        };

    private static partial AuctionState EvolveAuctionStarted(AuctionState state, AuctionStarted e) =>
        state with
        {
            Phase = AuctionPhase.Started,
            CurrentLotIndex = e.FirstLotIndex
        };

    private static partial AuctionState EvolveBidPlaced(AuctionState state, BidPlaced e) =>
        state with
        {
            // Bids fold in event-log order (Dictionary preserves insertion order absent
            // removals — which never happen), so the earliest top bidder wins a tie.
            Lots = MapLot(state.Lots, e.LotIndex, l => l with
            {
                Bids = new Dictionary<Guid, decimal>(l.Bids) { [e.PlayerId] = e.Amount }
            })
        };

    private static partial AuctionState EvolveLotRevealed(AuctionState state, LotRevealed e) =>
        state with
        {
            Lots = MapLot(state.Lots, e.LotIndex, l => l with
            {
                TrueWorth = e.TrueWorth,
                WinnerIds = e.WinnerIds,
                PricePaid = e.PricePaid,
                Resolved = true
            })
        };

    private static partial AuctionState EvolveRoundScored(AuctionState state, RoundScored e) =>
        state with
        {
            Lots = MapLot(state.Lots, e.LotIndex, l => l with
            {
                Profits = new Dictionary<Guid, int>(l.Profits) { [e.PlayerId] = e.Profit }
            })
        };

    private static partial AuctionState EvolveNextLotStarted(AuctionState state, NextLotStarted e) =>
        state with
        {
            CurrentLotIndex = e.LotIndex
        };

    private static partial AuctionState EvolveAuctionEnded(AuctionState state, AuctionEnded e) =>
        state with
        {
            Phase = AuctionPhase.Ended,
            FinalScoreboard = e.FinalScoreboard,
            WinnerIds = e.WinnerIds
        };

    private static partial Result<AuctionEvent[]> DecideOpenAuction(AuctionState state, OpenAuction command, AuctionContext context)
    {
        var pack = context.FindPack(command.PackId);
        if (pack is null)
            return new Err(new AuctionPackNotFound());

        var gameId = context.NewGuid();
        var hostPlayerId = context.NewGuid();
        var joinCode = context.NewGuid();

        // "mini"-slug packs are a large pool sampled to a short round (mirrors MEM's mini marker).
        // ponytail: single-value lots have no bands to balance, so a plain shuffle + take — not
        // MEM's PickBalanced. Add a FullAuctionSize cap here when a prod-size auction deck ships.
        var lots = command.PackId.Contains("mini")
            ? SampleLots(pack.Lots, MiniAuctionSize, context.NextRandom)
            : pack.Lots;

        return new Ok<AuctionEvent[]>([
            new AuctionOpened(gameId, hostPlayerId, command.HostName, joinCode, command.PackId, lots, context.Now())
        ]);
    }

    /// <summary>Fisher-Yates shuffle a copy of the pool, then take the first <paramref name="count"/>.</summary>
    private static IReadOnlyList<Lot> SampleLots(IReadOnlyList<Lot> pool, int count, Func<int, int> next)
    {
        if (pool.Count <= count)
            return pool;

        var copy = pool.ToList();
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var j = next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.Take(count).ToList();
    }

    private static partial Result<AuctionEvent[]> DecideJoinAuction(AuctionState state, JoinAuction command, AuctionContext context)
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

    private static partial Result<AuctionEvent[]> DecideStartAuction(AuctionState state, StartAuction command, AuctionContext context)
    {
        if (state.Phase == AuctionPhase.NotCreated)
            return new Err(new AuctionNotFound());

        if (state.Players.Count < 2)
            return new Err(new NotEnoughPlayers());

        return new Ok<AuctionEvent[]>([
            new AuctionStarted(state.GameId, FirstLotIndex: 0, context.Now())
        ]);
    }

    private static partial Result<AuctionEvent[]> DecidePlaceBid(AuctionState state, PlaceBid command, AuctionContext context)
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

    private static partial Result<AuctionEvent[]> DecideRevealLot(AuctionState state, RevealLot command, AuctionContext context)
    {
        if (!state.AllBidsIn(command.LotIndex))
            return new Err(new NotAllBidsIn());

        if (state.Lots[command.LotIndex].Resolved)
            return new Err(new LotAlreadyResolved());

        var round = state.Lots[command.LotIndex];
        var trueWorth = round.Lot.TrueWorth;

        // 0-100-artad regel: överbud (bud > santVärde) diskas — 0 poäng, aldrig vinnare, aldrig
        // negativt. Bland de giltiga buden (≤ santVärde) vinner det högsta; DELAD vinst → alla
        // på det budet vinner (Players-ordning = stabil, ingen tiebreak). Alla bjöd över → ingen
        // vinnare. Vinnarpoäng: exakt (bud == santVärde) → platt 10; annars vinst-% av santVärde.
        var validBids = round.Bids.Where(kv => kv.Value <= trueWorth).ToList();
        IReadOnlyList<Guid> winnerIds;
        decimal winningBid;
        if (validBids.Count == 0)
        {
            winnerIds = [];
            winningBid = 0m;
        }
        else
        {
            winningBid = validBids.Max(kv => kv.Value);
            winnerIds = state.Players
                .Where(p => round.Bids.TryGetValue(p.PlayerId, out var b) && b == winningBid)
                .Select(p => p.PlayerId)
                .ToList();
        }

        var winnerProfit = winnerIds.Count == 0
            ? 0
            : winningBid == trueWorth
                ? 10
                : (int)Math.Round((trueWorth - winningBid) / trueWorth * 100m, MidpointRounding.AwayFromZero);

        var events = new List<AuctionEvent>
        {
            new LotRevealed(state.GameId, command.LotIndex, trueWorth, winnerIds, winningBid)
        };

        foreach (var player in state.Players)
        {
            var profit = winnerIds.Contains(player.PlayerId) ? winnerProfit : 0;
            var totalScore = state.TotalScore(player.PlayerId) + profit;

            events.Add(new RoundScored(state.GameId, command.LotIndex, player.PlayerId, profit, totalScore));
        }

        return new Ok<AuctionEvent[]>([.. events]);
    }

    private static partial Result<AuctionEvent[]> DecideAskNextLot(AuctionState state, AskNextLot command, AuctionContext context) =>
        new Ok<AuctionEvent[]>([
            new NextLotStarted(state.GameId, state.CurrentLotIndex + 1)
        ]);

    private static partial Result<AuctionEvent[]> DecideEndAuction(AuctionState state, EndAuction command, AuctionContext context)
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
    Func<string, AuctionPack?> FindPack,
    Func<int, int> NextRandom
)
{
    public static AuctionContext Default => new(
        NewGuid: Guid.NewGuid,
        Now: () => DateTimeOffset.UtcNow,
        FindPack: _ => null,
        NextRandom: Random.Shared.Next
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
