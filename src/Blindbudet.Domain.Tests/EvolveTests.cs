using AwesomeAssertions;
using Xunit;
using static Blindbudet.Domain.Tests.Fixtures;

namespace Blindbudet.Domain.Tests;

/// <summary>
/// Decision-model fold GTs (State / Game): given prior events, then the folded AuctionState.
/// Verifies Evolve builds the single source of truth the decider reads.
/// </summary>
public class EvolveTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.UnixEpoch;

    private static AuctionOpened Opened() =>
        new(GameId, MartinId, "Martin", JoinCode, "blindbudet", [Lot0, Lot1], At);

    private static AuctionState Fold(params AuctionEvent[] events) => Decider.Fold(events);

    [Fact]
    public void state_folds_a_bid_into_the_decision_model()
    {
        var state = Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new BidPlaced(GameId, MartinId, 0, 70m, At));

        state.Phase.Should().Be(AuctionPhase.Started);
        state.HostPlayerId.Should().Be(MartinId);
        state.CurrentLotIndex.Should().Be(0);

        state.Lots[0].Bids.Should().Equal(new Dictionary<Guid, decimal> { [MartinId] = 70m });
        state.Lots[0].Resolved.Should().BeFalse();
        state.Lots[1].Bids.Should().BeEmpty();
        state.Lots[1].Resolved.Should().BeFalse();

        // nilsId still pending (derived).
        state.PendingBidPlayerIds(0).Should().Equal(NilsId);
    }

    [Fact]
    public void resolving_a_lot_folds_the_reveal_and_profits_into_that_lot()
    {
        var state = Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new AuctionStarted(GameId, 0, At),
            new BidPlaced(GameId, MartinId, 0, 70m, At),
            new BidPlaced(GameId, NilsId, 0, 90m, At),
            new LotRevealed(GameId, 0, 100m, [NilsId], 90m),
            new RoundScored(GameId, 0, MartinId, 0, 0),
            new RoundScored(GameId, 0, NilsId, 10, 10));

        state.CurrentLotIndex.Should().Be(0); // resolving does not advance; AskNextLot does

        var lot0 = state.Lots[0];
        lot0.Resolved.Should().BeTrue();
        lot0.TrueWorth.Should().Be(100m);
        lot0.WinnerIds.Should().Equal(NilsId);
        lot0.PricePaid.Should().Be(90m);
        lot0.Bids.Should().Equal(new Dictionary<Guid, decimal> { [MartinId] = 70m, [NilsId] = 90m });
        lot0.Profits.Should().Equal(new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 });

        state.Lots[1].Resolved.Should().BeFalse();

        // totalScore is DERIVED by summing profits across resolved lots.
        state.TotalScore(MartinId).Should().Be(0);
        state.TotalScore(NilsId).Should().Be(10);
    }
}
