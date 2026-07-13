namespace Blindbudet.Domain.Tests;

/// <summary>
/// Shared fixtures, named exactly as the spec's `tests:` reference them (the generated
/// SpecTests resolve bare words to Fixtures.* — a missing name is a CS0117).
/// lot0 trueWorth 100, lot1 trueWorth 50; host = martinId.
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

    /// <summary>A 10-lot "mini"-slug pool, to exercise round-sampling (MiniAuctionSize = 7).</summary>
    public static readonly AuctionPack MiniPack = new(
        "blindbudet-mini",
        "Blindbudet mini",
        [.. Enumerable.Range(0, 10).Select(i => new Lot($"lot{i}", 10m * i, "u"))]);

    /// <summary>A fixed clock so timestamp pins are deterministic in tests.</summary>
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    // Timestamps the decider stamps from the fixed clock — all Now under Context.
    public static readonly DateTimeOffset OpenedAt = Now;
    public static readonly DateTimeOffset JoinedAt = Now;
    public static readonly DateTimeOffset StartedAt = Now;
    public static readonly DateTimeOffset BidAt = Now;
    public static readonly DateTimeOffset EndedAt = Now;

    /// <summary>A join code / game id that resolves to nothing.</summary>
    public static readonly Guid Unknown = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

    // LotRound fixtures, named as the spec's `tests:` reference them (lot0Fresh, …).
    public static readonly LotRound Lot0Fresh = Round(Lot0);
    public static readonly LotRound Lot1Fresh = Round(Lot1);
    public static readonly LotRound Lot0Nils50 = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [NilsId] = 50m });
    public static readonly LotRound Lot0MartinOnly = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 70m });
    public static readonly LotRound Lot0BothIn = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 70m, [NilsId] = 90m });
    public static readonly LotRound Lot0MartinOverbid = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 120m, [NilsId] = 80m });
    public static readonly LotRound Lot0Tied80 = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 80m, [NilsId] = 80m });
    public static readonly LotRound Lot0Exact100 = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 100m, [NilsId] = 100m });
    public static readonly LotRound Lot0AllOverbid = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 120m, [NilsId] = 150m });
    public static readonly LotRound Lot1BothIn = Round(Lot1,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 40m, [NilsId] = 30m });
    public static readonly LotRound Lot0Resolved = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 60m, [NilsId] = 90m },
        trueWorth: 100m, winnerIds: [NilsId], pricePaid: 90m,
        profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, resolved: true);
    public static readonly LotRound Lot0Scored = Round(Lot0,
        trueWorth: 100m, winnerIds: [NilsId], pricePaid: 90m,
        profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, resolved: true);
    public static readonly LotRound Lot0ResolvedFull = Round(Lot0,
        bids: new Dictionary<Guid, decimal> { [MartinId] = 70m, [NilsId] = 90m },
        trueWorth: 100m, winnerIds: [NilsId], pricePaid: 90m,
        profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, resolved: true);
    public static readonly LotRound Lot0Profits20And0 = Round(Lot0,
        profits: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 0 }, resolved: true);
    public static readonly LotRound Lot1Profits0And5 = Round(Lot1,
        profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 5 }, resolved: true);
    public static readonly LotRound Lot0Profits10Each = Round(Lot0,
        profits: new Dictionary<Guid, int> { [MartinId] = 10, [NilsId] = 10 }, resolved: true);
    public static readonly LotRound Lot1Profits5Each = Round(Lot1,
        profits: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 5 }, resolved: true);

    // View-row fixtures (Outstanding bids / Round results / final scoreboards).
    public static readonly OutstandingBid Ob0AllPending = new(0, [MartinId, NilsId], false);
    public static readonly OutstandingBid Ob1AllPending = new(1, [MartinId, NilsId], false);
    public static readonly OutstandingBid Ob0NilsPending = new(0, [NilsId], false);
    public static readonly OutstandingBid Ob0AllIn = new(0, [], true);

    public static readonly PlayerProfit PpMartin0 = new(MartinId, 0, 0);
    public static readonly PlayerProfit PpNils10 = new(NilsId, 10, 10);

    public static readonly ScoreboardEntry SbMartin20 = new(MartinId, "Martin", 20);
    public static readonly ScoreboardEntry SbNils5 = new(NilsId, "Nils", 5);
    public static readonly ScoreboardEntry SbMartin15 = new(MartinId, "Martin", 15);
    public static readonly ScoreboardEntry SbNils15 = new(NilsId, "Nils", 15);

    /// <summary>
    /// Stub context: a fixed clock at Now; FindPack resolves the 2-lot fixture pack for
    /// "blindbudet" and the 10-lot MiniPack for "blindbudet-mini", else null. NextRandom is
    /// fixed to 0 so Fisher-Yates is deterministic in tests. NewGuid is real (minted values
    /// asserted only by presence).
    /// </summary>
    public static readonly AuctionContext Context = new(
        NewGuid: Guid.NewGuid,
        Now: () => Now,
        FindPack: slug => slug switch
        {
            "blindbudet" => Pack,
            "blindbudet-mini" => MiniPack,
            _ => null
        },
        NextRandom: _ => 0
    );

    /// <summary>Build a LotRound inline, mirroring the spec's flow-map fixtures.</summary>
    public static LotRound Round(
        Lot lot,
        IReadOnlyDictionary<Guid, decimal>? bids = null,
        decimal? trueWorth = null,
        IReadOnlyList<Guid>? winnerIds = null,
        decimal? pricePaid = null,
        IReadOnlyDictionary<Guid, int>? profits = null,
        bool resolved = false) =>
        new()
        {
            Lot = lot,
            Bids = bids ?? new Dictionary<Guid, decimal>(),
            TrueWorth = trueWorth,
            WinnerIds = winnerIds ?? [],
            PricePaid = pricePaid,
            Profits = profits ?? new Dictionary<Guid, int>(),
            Resolved = resolved
        };
}
