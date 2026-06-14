using AwesomeAssertions;
using Xunit;
using static MerEllerMindre.Domain.Tests.Fixtures;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Projection GTs from specs/game-flows.yaml: given prior events, fold via Decider.Fold,
/// then project the view and assert the spec's `then:` props.
/// </summary>
public class ProjectionTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.UnixEpoch;

    private static LobbyOpened Lobby() =>
        new(GameId, MartinId, "Martin", JoinCode, "mer-eller-mindre", [Question0, Question1], At);

    private static PlayerJoined NilsJoins() => new(GameId, NilsId, "Nils", At);
    private static GameStarted Started() => new(GameId, FirstQuestionIndex: 0, At);

    [Fact]
    public void LobbyListsTheHostAndJoinedPlayers()
    {
        var state = Decider.Fold([Lobby(), NilsJoins()]);

        var view = Projections.GameLobby(state);

        view.JoinCode.Should().Be(JoinCode);
        view.Players.Should().Equal(HostMartin, PlayerNils);
    }

    [Fact]
    public void CurrentQuestionPresentedWithCardContentAndProgress()
    {
        var state = Decider.Fold([Lobby(), Started()]);

        var view = Projections.Question(state);

        view.QuestionIndex.Should().Be(0);
        view.TotalQuestions.Should().Be(2);
        view.QuestionText.Should().Be("question0");
        view.ItemA.Should().Be("question0A");
        view.ItemB.Should().Be("question0B");
    }

    [Fact]
    public void WaitingForOthersShowsWhoHasGuessedAndWhoIsStillPending()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At)
        ]);

        var view = Projections.WaitingForOthers(state);

        view.QuestionIndex.Should().Be(0);
        view.SubmittedPlayerIds.Should().Equal(MartinId);
        view.PendingPlayerIds.Should().Equal(NilsId);
    }

    [Fact]
    public void RoundResultsRevealsTheAnswerAndPerPlayerScoresOnceScored()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At),
            new GuessSubmitted(GameId, NilsId, 0, Direction.Mindre, 50m, At),
            new QuestionAnswered(GameId, 0, Direction.Mer, CorrectDifference: 40),
            new QuestionScored(GameId, 0, MartinId, Direction.Mer, 30m, 30, true, 10, -10, RoundScore: 0, TotalScore: 0),
            new QuestionScored(GameId, 0, NilsId, Direction.Mindre, 50m, 50, false, 10, 0, RoundScore: 10, TotalScore: 10)
        ]);

        var view = Projections.RoundResults(state);

        view.QuestionIndex.Should().Be(0);
        view.CorrectDirection.Should().Be(Direction.Mer);
        view.CorrectDifference.Should().Be((byte)40);
        view.PlayerScores.Should().Equal(
            new PlayerScore(MartinId, RoundScore: 0, TotalScore: 0),
            new PlayerScore(NilsId, RoundScore: 10, TotalScore: 10));
    }

    [Fact]
    public void FinalStandingsShowsTheFinalScoreboardAndWinner()
    {
        IReadOnlyList<ScoreboardEntry> scoreboard =
        [
            new ScoreboardEntry(MartinId, "Martin", 5),
            new ScoreboardEntry(NilsId, "Nils", 10)
        ];
        var state = Decider.Fold([new GameEnded(GameId, scoreboard, [MartinId], At)]);

        var view = Projections.FinalStandings(state);

        view.FinalScoreboard.Should().Equal(scoreboard);
        view.WinnerIds.Should().Equal(MartinId);
    }

    [Fact]
    public void OutstandingGuessesEveryQuestionOpensForEveryPlayerWhenTheGameStarts()
    {
        var state = Decider.Fold([Lobby(), NilsJoins(), Started()]);

        var view = Projections.OutstandingGuesses(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingQuestion(0, [MartinId, NilsId], AllGuessesIn: false),
                new OutstandingQuestion(1, [MartinId, NilsId], AllGuessesIn: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void OutstandingGuessesASubmittedGuessChecksOffThatPlayerOnItsQuestion()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At)
        ]);

        var view = Projections.OutstandingGuesses(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingQuestion(0, [NilsId], AllGuessesIn: false),
                new OutstandingQuestion(1, [MartinId, NilsId], AllGuessesIn: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void OutstandingGuessesAQuestionShowsAllGuessesInOnceEveryPlayerHasGuessedIt()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At),
            new GuessSubmitted(GameId, NilsId, 0, Direction.Mindre, 50m, At)
        ]);

        var view = Projections.OutstandingGuesses(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingQuestion(0, [], AllGuessesIn: true),
                new OutstandingQuestion(1, [MartinId, NilsId], AllGuessesIn: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void OutstandingGuessesAreCheckedOffPerQuestionIndependently()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At),
            new GuessSubmitted(GameId, NilsId, 0, Direction.Mindre, 50m, At),
            new QuestionAnswered(GameId, 0, Direction.Mer, CorrectDifference: 40),
            new NextQuestionStarted(GameId, QuestionIndex: 1),
            new GuessSubmitted(GameId, MartinId, 1, Direction.Mindre, 25m, At)
        ]);

        var view = Projections.OutstandingGuesses(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingQuestion(0, [], AllGuessesIn: true),
                new OutstandingQuestion(1, [NilsId], AllGuessesIn: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void GameProgressShowsANextQuestionWhileQuestionsRemain()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At),
            new GuessSubmitted(GameId, NilsId, 0, Direction.Mindre, 50m, At),
            new QuestionAnswered(GameId, 0, Direction.Mer, CorrectDifference: 40),
            new QuestionScored(GameId, 0, MartinId, Direction.Mer, 30m, 30, true, 10, -10, RoundScore: 0, TotalScore: 0),
            new QuestionScored(GameId, 0, NilsId, Direction.Mindre, 50m, 50, false, 10, 0, RoundScore: 10, TotalScore: 10)
        ]);

        var view = Projections.GameProgress(state);

        view.QuestionIndex.Should().Be(0);
        view.TotalQuestions.Should().Be(2);
        view.ScoredQuestionCount.Should().Be(1);
        view.HasNextQuestion.Should().BeTrue();
    }

    [Fact]
    public void GameProgressShowsNoNextQuestionOnceTheLastIsScored()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new GuessSubmitted(GameId, MartinId, 0, Direction.Mer, 30m, At),
            new GuessSubmitted(GameId, NilsId, 0, Direction.Mindre, 50m, At),
            new QuestionAnswered(GameId, 0, Direction.Mer, CorrectDifference: 40),
            new QuestionScored(GameId, 0, MartinId, Direction.Mer, 30m, 30, true, 10, -10, RoundScore: 0, TotalScore: 0),
            new QuestionScored(GameId, 0, NilsId, Direction.Mindre, 50m, 50, false, 10, 0, RoundScore: 10, TotalScore: 10),
            new NextQuestionStarted(GameId, QuestionIndex: 1),
            new GuessSubmitted(GameId, MartinId, 1, Direction.Mindre, 25m, At),
            new GuessSubmitted(GameId, NilsId, 1, Direction.Mindre, 20m, At),
            new QuestionAnswered(GameId, 1, Direction.Mindre, CorrectDifference: 20),
            new QuestionScored(GameId, 1, MartinId, Direction.Mindre, 25m, 25, true, 5, -10, RoundScore: 5, TotalScore: 5),
            new QuestionScored(GameId, 1, NilsId, Direction.Mindre, 20m, 20, true, 0, -10, RoundScore: 0, TotalScore: 10)
        ]);

        var view = Projections.GameProgress(state);

        view.QuestionIndex.Should().Be(1);
        view.TotalQuestions.Should().Be(2);
        view.ScoredQuestionCount.Should().Be(2);
        view.HasNextQuestion.Should().BeFalse();
    }
}
