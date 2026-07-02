using AwesomeAssertions;
using Xunit;
using static Blindbudet.Domain.Tests.Fixtures;

namespace Blindbudet.Domain.Tests;

/// <summary>
/// Decider-true GWTs: given the folded State / Game props `decide` reads, when a command,
/// then events | rejection. One test per `tests:` case in specs/blindbudet-event-model.yaml.
/// Union case checks use the CONCRETE `is`-pattern (the union runtime-type trap).
/// </summary>
public class DeciderTests
{
    private static AuctionState State(
        AuctionPhase phase,
        IReadOnlyList<Player> players,
        int currentLotIndex,
        params LotRound[] lots) =>
        new()
        {
            GameId = GameId,
            JoinCode = JoinCode,
            PackId = "blindbudet",
            Phase = phase,
            HostPlayerId = MartinId,
            Players = players,
            CurrentLotIndex = currentLotIndex,
            Lots = lots
        };

    // --- Open Auction --------------------------------------------------------

    [Fact]
    public void auction_can_be_created()
    {
        var events = Gwt.GivenInitial()
            .When(new OpenAuction("Martin", "blindbudet"))
            .Events();

        var opened = events.Opened();
        opened.HostName.Should().Be("Martin");
        opened.PackId.Should().Be("blindbudet");
        opened.Lots.Should().Equal(Lot0, Lot1);
        opened.HostPlayerId.Should().NotBe(Guid.Empty);
        opened.JoinCode.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void cannot_open_auction_with_unknown_pack()
    {
        var error = Gwt.GivenInitial()
            .When(new OpenAuction("Martin", "unknown"))
            .Error();

        (error.Error is AuctionPackNotFound).Should().BeTrue();
    }

    [Fact]
    public void mini_pack_is_sampled_to_a_short_round()
    {
        var opened = Gwt.GivenInitial()
            .When(new OpenAuction("Martin", "blindbudet-mini"))
            .Events()
            .Opened();

        opened.Lots.Should().HaveCount(Decider.MiniAuctionSize);
        opened.Lots.Should().OnlyContain(l => MiniPack.Lots.Contains(l));
    }

    // --- Join Auction --------------------------------------------------------

    [Fact]
    public void player_can_join_lobby()
    {
        var joined = Gwt.Given(State(AuctionPhase.Lobby, [HostMartin], -1))
            .When(new JoinAuction(JoinCode, "Nils"))
            .Events()
            .Joined();

        joined.PlayerName.Should().Be("Nils");
        joined.PlayerId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void cannot_join_nonexistent_auction()
    {
        var error = Gwt.GivenInitial()
            .When(new JoinAuction(JoinCode, "Nils"))
            .Error();

        (error.Error is AuctionNotFound).Should().BeTrue();
    }

    [Fact]
    public void cannot_join_started_auction()
    {
        var error = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0))
            .When(new JoinAuction(JoinCode, "Sven"))
            .Error();

        (error.Error is AuctionAlreadyStarted).Should().BeTrue();
    }

    [Fact]
    public void cannot_join_with_name_already_taken()
    {
        var error = Gwt.Given(State(AuctionPhase.Lobby, [HostMartin, PlayerNils], -1))
            .When(new JoinAuction(JoinCode, "Nils"))
            .Error();

        (error.Error is NameAlreadyTaken).Should().BeTrue();
    }

    // --- Start Auction -------------------------------------------------------

    [Fact]
    public void auction_can_be_started()
    {
        var started = Gwt.Given(State(AuctionPhase.Lobby, [HostMartin, PlayerNils], -1))
            .When(new StartAuction(GameId))
            .Events()
            .Started();

        started.FirstLotIndex.Should().Be(0);
    }

    [Fact]
    public void cannot_start_nonexistent_auction()
    {
        var error = Gwt.GivenInitial()
            .When(new StartAuction(GameId))
            .Error();

        (error.Error is AuctionNotFound).Should().BeTrue();
    }

    [Fact]
    public void cannot_start_without_enough_players()
    {
        var error = Gwt.Given(State(AuctionPhase.Lobby, [HostMartin], -1))
            .When(new StartAuction(GameId))
            .Error();

        (error.Error is NotEnoughPlayers).Should().BeTrue();
    }

    // --- Place Bid -----------------------------------------------------------

    [Fact]
    public void bid_placed_successfully()
    {
        var bid = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0, Round(Lot0)))
            .When(new PlaceBid(GameId, NilsId, 0, 90m))
            .Events()
            .Bid();

        bid.PlayerId.Should().Be(NilsId);
        bid.LotIndex.Should().Be(0);
        bid.Amount.Should().Be(90m);
    }

    [Fact]
    public void cannot_bid_in_nonexistent_auction()
    {
        var error = Gwt.GivenInitial()
            .When(new PlaceBid(GameId, NilsId, 0, 90m))
            .Error();

        (error.Error is AuctionNotFound).Should().BeTrue();
    }

    [Fact]
    public void negative_bid_rejected()
    {
        var error = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0, Round(Lot0)))
            .When(new PlaceBid(GameId, NilsId, 0, -5m))
            .Error();

        (error.Error is BidNegative).Should().BeTrue();
    }

    [Fact]
    public void cannot_bid_twice_on_same_lot()
    {
        var error = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Lot0, bids: new Dictionary<Guid, decimal> { [NilsId] = 50m })))
            .When(new PlaceBid(GameId, NilsId, 0, 70m))
            .Error();

        (error.Error is AlreadyBid).Should().BeTrue();
    }

    [Fact]
    public void cannot_bid_on_an_already_resolved_lot()
    {
        var resolved = Round(Lot0,
            bids: new Dictionary<Guid, decimal> { [MartinId] = 60m, [NilsId] = 90m },
            trueWorth: 100m, winnerId: NilsId, pricePaid: 90m,
            profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, resolved: true);

        var error = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0, resolved))
            .When(new PlaceBid(GameId, MartinId, 0, 80m))
            .Error();

        (error.Error is LotAlreadyResolved).Should().BeTrue();
    }

    // --- Reveal Lot ----------------------------------------------------------

    [Fact]
    public void highest_bid_wins_and_pays_its_bid()
    {
        var events = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Lot0, bids: new Dictionary<Guid, decimal> { [MartinId] = 70m, [NilsId] = 90m }),
                Round(Lot1)))
            .When(new RevealLot(GameId, 0))
            .Events();

        var revealed = events.Revealed();
        revealed.LotIndex.Should().Be(0);
        revealed.TrueWorth.Should().Be(100m);
        revealed.WinnerId.Should().Be(NilsId);
        revealed.PricePaid.Should().Be(90m);

        events.ScoredFor(MartinId).Should().BeEquivalentTo(new { Profit = 0, TotalScore = 0 });
        events.ScoredFor(NilsId).Should().BeEquivalentTo(new { Profit = 10, TotalScore = 10 });
    }

    [Fact]
    public void overbidding_the_worth_yields_a_negative_profit()
    {
        var events = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Lot0, bids: new Dictionary<Guid, decimal> { [MartinId] = 120m, [NilsId] = 80m }),
                Round(Lot1)))
            .When(new RevealLot(GameId, 0))
            .Events();

        var revealed = events.Revealed();
        revealed.WinnerId.Should().Be(MartinId);
        revealed.PricePaid.Should().Be(120m);

        events.ScoredFor(MartinId).Should().BeEquivalentTo(new { Profit = -20, TotalScore = -20 });
        events.ScoredFor(NilsId).Should().BeEquivalentTo(new { Profit = 0, TotalScore = 0 });
    }

    [Fact]
    public void tie_on_highest_bid_broken_by_earliest_bid()
    {
        // martin bid first (folded earlier) -> wins the tie.
        var bids = new Dictionary<Guid, decimal> { [MartinId] = 80m, [NilsId] = 80m };

        var events = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Lot0, bids: bids), Round(Lot1)))
            .When(new RevealLot(GameId, 0))
            .Events();

        var revealed = events.Revealed();
        revealed.WinnerId.Should().Be(MartinId);
        revealed.PricePaid.Should().Be(80m);
        events.ScoredFor(MartinId).Profit.Should().Be(20);
        events.ScoredFor(NilsId).Profit.Should().Be(0);
    }

    [Fact]
    public void scores_accumulate_across_lots()
    {
        var lot0 = Round(Lot0, trueWorth: 100m, winnerId: NilsId, pricePaid: 90m,
            profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, resolved: true);
        var lot1 = Round(Lot1, bids: new Dictionary<Guid, decimal> { [MartinId] = 40m, [NilsId] = 30m });

        var events = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 1, lot0, lot1))
            .When(new RevealLot(GameId, 1))
            .Events();

        var revealed = events.Revealed();
        revealed.LotIndex.Should().Be(1);
        revealed.TrueWorth.Should().Be(50m);
        revealed.WinnerId.Should().Be(MartinId);
        revealed.PricePaid.Should().Be(40m);

        events.ScoredFor(MartinId).Should().BeEquivalentTo(new { Profit = 10, TotalScore = 10 });
        events.ScoredFor(NilsId).Should().BeEquivalentTo(new { Profit = 0, TotalScore = 10 });
    }

    [Fact]
    public void cannot_reveal_before_all_bids_in()
    {
        var error = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Lot0, bids: new Dictionary<Guid, decimal> { [MartinId] = 70m }),
                Round(Lot1)))
            .When(new RevealLot(GameId, 0))
            .Error();

        (error.Error is NotAllBidsIn).Should().BeTrue();
    }

    [Fact]
    public void cannot_reveal_an_already_resolved_lot()
    {
        var resolved = Round(Lot0, trueWorth: 100m, winnerId: NilsId, pricePaid: 90m,
            bids: new Dictionary<Guid, decimal> { [MartinId] = 70m, [NilsId] = 90m },
            profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, resolved: true);

        var error = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0, resolved, Round(Lot1)))
            .When(new RevealLot(GameId, 0))
            .Error();

        (error.Error is LotAlreadyResolved).Should().BeTrue();
    }

    // --- Next Lot ------------------------------------------------------------

    [Fact]
    public void next_lot_presented_when_one_remains()
    {
        var lot0 = Round(Lot0, trueWorth: 100m, winnerId: NilsId, pricePaid: 90m,
            profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, resolved: true);

        var next = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 0, lot0, Round(Lot1)))
            .When(new AskNextLot(GameId))
            .Events()
            .NextLot();

        next.LotIndex.Should().Be(1);
    }

    // --- End Auction ---------------------------------------------------------

    [Fact]
    public void highest_score_wins()
    {
        var lot0 = Round(Lot0, profits: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 0 }, resolved: true);
        var lot1 = Round(Lot1, profits: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 5 }, resolved: true);

        var ended = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 1, lot0, lot1))
            .When(new EndAuction(GameId))
            .Events()
            .Ended();

        ended.FinalScoreboard.Should().BeEquivalentTo(new[]
        {
            new ScoreboardEntry(MartinId, "Martin", 20),
            new ScoreboardEntry(NilsId, "Nils", 5)
        }, o => o.WithStrictOrdering());
        ended.WinnerIds.Should().Equal(MartinId);
    }

    [Fact]
    public void tied_highest_totals_share_the_win()
    {
        var lot0 = Round(Lot0, profits: new Dictionary<Guid, int> { [MartinId] = 10, [NilsId] = 10 }, resolved: true);
        var lot1 = Round(Lot1, profits: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 5 }, resolved: true);

        var ended = Gwt.Given(State(AuctionPhase.Started, [HostMartin, PlayerNils], 1, lot0, lot1))
            .When(new EndAuction(GameId))
            .Events()
            .Ended();

        ended.FinalScoreboard.Should().BeEquivalentTo(new[]
        {
            new ScoreboardEntry(MartinId, "Martin", 15),
            new ScoreboardEntry(NilsId, "Nils", 15)
        }, o => o.WithStrictOrdering());
        ended.WinnerIds.Should().Equal(MartinId, NilsId);
    }
}
