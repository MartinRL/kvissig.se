using Xunit;

namespace Blindbudet.Domain.Tests;

/// <summary>
/// Shared fixtures (named to match the spec's GWT cases) plus a Given/When/Then scaffold for
/// the decider-true GWTs. Fixtures: lot0 trueWorth 100, lot1 trueWorth 50; host = martinId.
/// </summary>
public static class Fixtures
{
    public static readonly Guid MartinId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NilsId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SvenId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid GameId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid JoinCode = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Player HostMartin = new(MartinId, "Martin", IsHost: true);
    public static readonly Player PlayerNils = new(NilsId, "Nils", IsHost: false);
    public static readonly Player PlayerSven = new(SvenId, "Sven", IsHost: false);

    public static readonly Lot Lot0 = new("lot0", 100m, "lot0U");
    public static readonly Lot Lot1 = new("lot1", 50m, "lot1U");

    public static readonly AuctionPack Pack = new("blindbudet", "Blindbudet", [Lot0, Lot1]);

    /// <summary>
    /// Stub context: FindPack resolves the 2-lot fixture pack for "blindbudet", else null.
    /// NewGuid/Now are real (minted values asserted only by presence).
    /// </summary>
    public static readonly AuctionContext Context = new(
        NewGuid: Guid.NewGuid,
        Now: () => DateTimeOffset.UtcNow,
        FindPack: slug => slug == "blindbudet" ? Pack : null
    );

    /// <summary>Build a LotRound inline, mirroring the spec's flow-map fixtures.</summary>
    public static LotRound Round(
        Lot lot,
        IReadOnlyDictionary<Guid, decimal>? bids = null,
        decimal? trueWorth = null,
        Guid? winnerId = null,
        decimal? pricePaid = null,
        IReadOnlyDictionary<Guid, int>? profits = null,
        bool resolved = false) =>
        new()
        {
            Lot = lot,
            Bids = bids ?? new Dictionary<Guid, decimal>(),
            TrueWorth = trueWorth,
            WinnerId = winnerId,
            PricePaid = pricePaid,
            Profits = profits ?? new Dictionary<Guid, int>(),
            Resolved = resolved
        };
}

/// <summary>Entry point for the decider GWT scaffold: Gwt.Given(state).When(command).</summary>
public static class Gwt
{
    public static GivenState Given(AuctionState state) => new(state);
    public static GivenState GivenInitial() => new(AuctionState.Initial);
}

public sealed record GivenState(AuctionState State)
{
    public Result<AuctionEvent[]> When(AuctionCommand command) =>
        Decider.Decide(State, command, Fixtures.Context);
}

/// <summary>
/// Result/union extractors. Union case checks MUST use the `is`-pattern against a CONCRETE
/// case type (the union's runtime type is the union, not the case — a generic `is T` would
/// fall back to isinst and never match). So these helpers take concrete types only.
/// </summary>
public static class ResultAssertions
{
    public static AuctionEvent[] Events(this Result<AuctionEvent[]> result)
    {
        if (result is Ok<AuctionEvent[]> ok)
            return ok.Value;
        Assert.Fail("expected Ok (events), got an error");
        return [];
    }

    public static Err Error(this Result<AuctionEvent[]> result)
    {
        if (result is Err err)
            return err;
        Assert.Fail("expected Err, got Ok (events)");
        return null!;
    }

    public static AuctionOpened Opened(this AuctionEvent[] events)
    {
        foreach (var e in events)
            if (e is AuctionOpened a)
                return a;
        Assert.Fail("no AuctionOpened event");
        return null!;
    }

    public static PlayerJoined Joined(this AuctionEvent[] events)
    {
        foreach (var e in events)
            if (e is PlayerJoined a)
                return a;
        Assert.Fail("no PlayerJoined event");
        return null!;
    }

    public static AuctionStarted Started(this AuctionEvent[] events)
    {
        foreach (var e in events)
            if (e is AuctionStarted a)
                return a;
        Assert.Fail("no AuctionStarted event");
        return null!;
    }

    public static BidPlaced Bid(this AuctionEvent[] events)
    {
        foreach (var e in events)
            if (e is BidPlaced a)
                return a;
        Assert.Fail("no BidPlaced event");
        return null!;
    }

    public static LotRevealed Revealed(this AuctionEvent[] events)
    {
        foreach (var e in events)
            if (e is LotRevealed a)
                return a;
        Assert.Fail("no LotRevealed event");
        return null!;
    }

    public static RoundScored ScoredFor(this AuctionEvent[] events, Guid playerId)
    {
        foreach (var e in events)
            if (e is RoundScored s && s.PlayerId == playerId)
                return s;
        Assert.Fail($"no RoundScored for {playerId}");
        return null!;
    }

    public static NextLotStarted NextLot(this AuctionEvent[] events)
    {
        foreach (var e in events)
            if (e is NextLotStarted a)
                return a;
        Assert.Fail("no NextLotStarted event");
        return null!;
    }

    public static AuctionEnded Ended(this AuctionEvent[] events)
    {
        foreach (var e in events)
            if (e is AuctionEnded a)
                return a;
        Assert.Fail("no AuctionEnded event");
        return null!;
    }
}
