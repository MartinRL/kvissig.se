using AwesomeAssertions;
using Xunit;
using static TankTillTusen.Domain.Tests.Fixtures;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Decision-model fold GTs (State / Game): given prior events, then the folded TankState.
/// Verifies Evolve builds the single source of truth the decider reads.
/// </summary>
public class EvolveTests
{
    private static LobbyOpened Opened() =>
        new(GameId, MartinId, "Martin", JoinCode, [Puzzle0, Puzzle1], StartedAt);

    private static TankState Fold(params TankEvent[] events) => Decider.Fold(events);

    [Fact]
    public void state_folds_a_submitted_solution_into_the_decision_model()
    {
        var state = Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new SolutionSubmitted(GameId, MartinId, 0, SolMiss, StartedAt));

        state.Phase.Should().Be(TankPhase.Started);
        state.HostPlayerId.Should().Be(MartinId);
        state.CurrentRoundIndex.Should().Be(0);

        state.Rounds[0].StartedAt.Should().Be(StartedAt);
        state.Rounds[0].Solutions.Should().Equal(new Dictionary<Guid, Solution> { [MartinId] = SolMiss });
        state.Rounds[0].Scored.Should().BeFalse();
        state.Rounds[1].Solutions.Should().BeEmpty();
        state.Rounds[1].Scored.Should().BeFalse();

        // nilsId still pending (derived).
        state.PendingPlayerIds(0).Should().Equal(NilsId);
    }

    [Fact]
    public void scoring_a_round_folds_the_reveal_and_scores_into_that_round()
    {
        var state = Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new SolutionSubmitted(GameId, MartinId, 0, SolMiss, StartedAt),
            new SolutionSubmitted(GameId, NilsId, 0, SolHit, StartedAt),
            new PuzzleRevealed(GameId, 0, SampleSol0),
            new RoundScored(GameId, 0, MartinId, 20, 80, 80),
            new RoundScored(GameId, 0, NilsId, 100, 0, 0));

        state.CurrentRoundIndex.Should().Be(0); // scoring does not advance; AskNextPuzzle does

        var round0 = state.Rounds[0];
        round0.Scored.Should().BeTrue();
        round0.SampleSolution.Should().Be(SampleSol0);
        round0.Solutions.Should().Equal(new Dictionary<Guid, Solution> { [MartinId] = SolMiss, [NilsId] = SolHit });
        round0.ReachedValues.Should().Equal(new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 100 });
        round0.RoundScores.Should().Equal(new Dictionary<Guid, int> { [MartinId] = 80, [NilsId] = 0 });

        state.Rounds[1].Scored.Should().BeFalse();

        // totalScore is DERIVED by summing round scores across scored rounds.
        state.TotalScore(MartinId).Should().Be(80);
        state.TotalScore(NilsId).Should().Be(0);
    }
}
