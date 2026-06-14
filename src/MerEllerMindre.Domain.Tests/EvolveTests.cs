using AwesomeAssertions;
using Xunit;
using static MerEllerMindre.Domain.Tests.Fixtures;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Decision-Model fold GTs from specs/game-flows.yaml: given prior events, then the
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
            new GuessSubmitted(GameId, MartinId, QuestionIndex: 0, Direction.Mer, GuessedDifference: 30m, At)
        ]);

        state.Phase.Should().Be(GamePhase.Started);
        state.HostPlayerId.Should().Be(MartinId);
        state.CurrentQuestionIndex.Should().Be(0);
        state.Players.Should().Equal(HostMartin, PlayerNils);

        state.Questions.Should().HaveCount(2);
        state.Questions[0].Card.Should().Be(Question0);
        state.Questions[0].Guesses.Should().ContainKey(MartinId).WhoseValue.Should().Be(GuessMer30);
        state.Questions[0].Scored.Should().BeFalse();
        state.Questions[1].Guesses.Should().BeEmpty();
        state.Questions[1].Scored.Should().BeFalse();

        state.PendingPlayerIds(0).Should().Equal(NilsId);
        state.AllGuessesIn(0).Should().BeFalse();
    }

    [Fact]
    public void ScoringAQuestionFoldsTheAnswerAndRoundScores()
    {
        var state = Decider.Fold(
        [
            new LobbyOpened(GameId, MartinId, "Martin", JoinCode, "mer-eller-mindre", [Question0, Question1], At),
            new PlayerJoined(GameId, NilsId, "Nils", At),
            new GameStarted(GameId, FirstQuestionIndex: 0, At),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At),
            new GuessSubmitted(GameId, NilsId, 0, Direction.Mindre, 50m, At),
            new QuestionAnswered(GameId, 0, Direction.Mer, CorrectDifference: 40),
            new QuestionScored(GameId, 0, MartinId, Direction.Mer, 30m, 30, DirectionCorrect: true, 10, -10, RoundScore: 0, TotalScore: 0),
            new QuestionScored(GameId, 0, NilsId, Direction.Mindre, 50m, 50, DirectionCorrect: false, 10, 0, RoundScore: 10, TotalScore: 10)
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
