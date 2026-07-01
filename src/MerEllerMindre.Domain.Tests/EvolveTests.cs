using AwesomeAssertions;
using Xunit;
using static MerEllerMindre.Domain.Tests.Fixtures;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Decision-Model fold GTs from specs/mer-eller-mindre-event-model.yaml: given prior events, then the
/// folded GameState (verifies Evolve / Fold via event-replay).
/// </summary>
public class EvolveTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.UnixEpoch;

    [Fact]
    public void StateFoldsEventsIntoTheDecisionModel()
    {
        var state = Decider.Fold(
        [
            new LobbyOpened(GameId, MartinId, "Martin", JoinCode, "mer-eller-mindre", [Question0, Question1], At),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new GameStarted(GameId, FirstQuestionIndex: 0, At),
            new DirectionSubmitted(GameId, MartinId, QuestionIndex: 0, Direction.Mer, At)
        ]);

        state.Phase.Should().Be(GamePhase.Started);
        state.HostPlayerId.Should().Be(MartinId);
        state.CurrentQuestionIndex.Should().Be(0);
        state.Players.Should().Equal(HostMartin, PlayerNils);

        state.Questions.Should().HaveCount(2);
        state.Questions[0].Card.Should().Be(Question0);
        state.Questions[0].Directions.Should().ContainKey(MartinId).WhoseValue.Should().Be(Direction.Mer);
        state.Questions[0].Scored.Should().BeFalse();
        state.Questions[1].Directions.Should().BeEmpty();
        state.Questions[1].Scored.Should().BeFalse();

        state.PendingDirectionPlayerIds(0).Should().Equal(NilsId);
        state.AllDirectionsIn(0).Should().BeFalse();
        state.DirectionRevealed(0).Should().BeFalse();
    }

    [Fact]
    public void RevealingDirectionFoldsTheCorrectDirectionAndBonus()
    {
        var state = Decider.Fold(
        [
            new LobbyOpened(GameId, MartinId, "Martin", JoinCode, "mer-eller-mindre", [Question0, Question1], At),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new GameStarted(GameId, FirstQuestionIndex: 0, At),
            new DirectionSubmitted(GameId, MartinId, 0, Direction.Mer, At),
            new DirectionSubmitted(GameId, NilsId, 0, Direction.Mindre, At),
            new QuestionDirectionRevealed(GameId, 0, Direction.Mer),
            new DirectionScored(GameId, 0, MartinId, Direction.Mer, DirectionCorrect: true, BonusPoints: -10),
            new DirectionScored(GameId, 0, NilsId, Direction.Mindre, DirectionCorrect: false, BonusPoints: 0)
        ]);

        var round = state.Questions[0];
        round.CorrectDirection.Should().Be(Direction.Mer);
        round.DirectionScores[MartinId].Should().Be(-10);
        round.DirectionScores[NilsId].Should().Be(0);
        round.Scored.Should().BeFalse();
        state.DirectionRevealed(0).Should().BeTrue();
    }

    [Fact]
    public void ScoringTheDifferenceFoldsTheAnswerAndRoundScores()
    {
        var state = Decider.Fold(
        [
            new LobbyOpened(GameId, MartinId, "Martin", JoinCode, "mer-eller-mindre", [Question0, Question1], At),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new GameStarted(GameId, FirstQuestionIndex: 0, At),
            new DirectionSubmitted(GameId, MartinId, 0, Direction.Mer, At),
            new DirectionSubmitted(GameId, NilsId, 0, Direction.Mindre, At),
            new QuestionDirectionRevealed(GameId, 0, Direction.Mer),
            new DirectionScored(GameId, 0, MartinId, Direction.Mer, DirectionCorrect: true, BonusPoints: -10),
            new DirectionScored(GameId, 0, NilsId, Direction.Mindre, DirectionCorrect: false, BonusPoints: 0),
            new DifferenceSubmitted(GameId, MartinId, 0, 30m, At),
            new DifferenceSubmitted(GameId, NilsId, 0, 50m, At),
            new QuestionDifferenceRevealed(GameId, 0, CorrectDifference: 40),
            new DifferenceScored(GameId, 0, MartinId, 30m, 30, 10, RoundScore: 0, TotalScore: 0),
            new DifferenceScored(GameId, 0, NilsId, 50m, 50, 10, RoundScore: 10, TotalScore: 10)
        ]);

        state.CurrentQuestionIndex.Should().Be(0);

        var round = state.Questions[0];
        round.CorrectDirection.Should().Be(Direction.Mer);
        round.CorrectDifference.Should().Be((byte)40);
        round.Scored.Should().BeTrue();
        round.RoundScores[MartinId].Should().Be(0);
        round.RoundScores[NilsId].Should().Be(10);

        state.TotalScore(MartinId).Should().Be(0);
        state.TotalScore(NilsId).Should().Be(10);
    }
}
