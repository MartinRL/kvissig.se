using AwesomeAssertions;
using Xunit;
using static Blindbudet.Domain.Tests.Fixtures;

namespace Blindbudet.Domain.Tests;

/// <summary>
/// Projection GTs: given prior Game-stream events (folded via Decider.Fold), then the view.
/// One test per read-model `tests:` case in specs/blindbudet-event-model.yaml.
/// </summary>
public class ProjectionTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.UnixEpoch;

    private static AuctionOpened Opened() =>
        new(GameId, MartinId, "Martin", JoinCode, "blindbudet", [Lot0, Lot1], At);

    private static AuctionState Fold(params AuctionEvent[] events) => Decider.Fold(events);

    [Fact]
    public void lobby_lists_the_host_and_joined_players()
    {
        var view = Projections.AuctionLobby(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At)));

        view.JoinCode.Should().Be(JoinCode);
        view.Players.Should().Equal(HostMartin, PlayerNils);
    }

    [Fact]
    public void current_lot_presented_with_content_and_progress()
    {
        var view = Projections.Lot(Fold(
            Opened(),
            new AuctionStarted(GameId, 0, At)));

        view.LotIndex.Should().Be(0);
        view.TotalLots.Should().Be(2);
        view.Description.Should().Be("lot0");
        view.Unit.Should().Be("lot0U");
    }

    [Fact]
    public void shows_who_has_bid_and_who_is_still_pending()
    {
        var view = Projections.WaitingForBids(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new BidPlaced(GameId, MartinId, 0, 70m, At)));

        view.LotIndex.Should().Be(0);
        view.SubmittedPlayerIds.Should().Equal(MartinId);
        view.PendingPlayerIds.Should().Equal(NilsId);
    }

    [Fact]
    public void every_lot_opens_for_every_player_when_the_auction_starts()
    {
        var view = Projections.OutstandingBids(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At)));

        view.Lots.Should().BeEquivalentTo(new[]
        {
            new OutstandingBid(0, [MartinId, NilsId], false),
            new OutstandingBid(1, [MartinId, NilsId], false)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void a_placed_bid_checks_off_that_player_on_its_lot()
    {
        var view = Projections.OutstandingBids(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new BidPlaced(GameId, MartinId, 0, 70m, At)));

        view.Lots.Should().BeEquivalentTo(new[]
        {
            new OutstandingBid(0, [NilsId], false),
            new OutstandingBid(1, [MartinId, NilsId], false)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void a_lot_shows_all_bids_in_once_every_player_has_bid()
    {
        var view = Projections.OutstandingBids(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new BidPlaced(GameId, MartinId, 0, 70m, At),
            new BidPlaced(GameId, NilsId, 0, 90m, At)));

        view.Lots.Should().BeEquivalentTo(new[]
        {
            new OutstandingBid(0, [], true),
            new OutstandingBid(1, [MartinId, NilsId], false)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void reveals_the_worth_winner_and_per_player_profit_once_resolved()
    {
        var view = Projections.RoundResults(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new BidPlaced(GameId, MartinId, 0, 70m, At),
            new BidPlaced(GameId, NilsId, 0, 90m, At),
            new LotRevealed(GameId, 0, 100m, [NilsId], 90m),
            new RoundScored(GameId, 0, MartinId, 0, 0),
            new RoundScored(GameId, 0, NilsId, 10, 10)));

        view.LotIndex.Should().Be(0);
        view.TrueWorth.Should().Be(100m);
        view.WinnerIds.Should().Equal(NilsId);
        view.PricePaid.Should().Be(90m);
        view.PlayerProfits.Should().BeEquivalentTo(new[]
        {
            new PlayerProfit(MartinId, 0, 0),
            new PlayerProfit(NilsId, 10, 10)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void progress_shows_a_next_lot_while_lots_remain()
    {
        var view = Projections.AuctionProgress(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new LotRevealed(GameId, 0, 100m, [NilsId], 90m)));

        view.LotIndex.Should().Be(0);
        view.TotalLots.Should().Be(2);
        view.ResolvedLotCount.Should().Be(1);
        view.HasNextLot.Should().BeTrue();
    }

    [Fact]
    public void progress_shows_no_next_lot_once_the_last_is_resolved()
    {
        var view = Projections.AuctionProgress(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new LotRevealed(GameId, 0, 100m, [NilsId], 90m),
            new NextLotStarted(GameId, 1),
            new LotRevealed(GameId, 1, 50m, [MartinId], 40m)));

        view.LotIndex.Should().Be(1);
        view.TotalLots.Should().Be(2);
        view.ResolvedLotCount.Should().Be(2);
        view.HasNextLot.Should().BeFalse();
    }

    [Fact]
    public void shows_the_final_scoreboard_and_winner()
    {
        var scoreboard = new[]
        {
            new ScoreboardEntry(MartinId, "Martin", 20),
            new ScoreboardEntry(NilsId, "Nils", 5)
        };

        var view = Projections.FinalStandings(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionEnded(GameId, scoreboard, [MartinId], At)));

        view.FinalScoreboard.Should().BeEquivalentTo(scoreboard, o => o.WithStrictOrdering());
        view.WinnerIds.Should().Equal(MartinId);
    }
}
