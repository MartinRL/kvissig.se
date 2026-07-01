using AwesomeAssertions;
using Xunit;
using static MerEllerMindre.Domain.Tests.Fixtures;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Projection GTs from specs/mer-eller-mindre-event-model.yaml: given prior events, fold via Decider.Fold,
/// then project the view and assert the spec's `then:` props.
/// </summary>
public class ProjectionTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.UnixEpoch;

    private static LobbyOpened Lobby() =>
        new(GameId, MartinId, "Martin", JoinCode, "mer-eller-mindre", [Question0, Question1], At);

    private static PlayerJoined NilsJoins() => new(GameId, NilsId, "Nils", At);
    private static GameStarted Started() => new(GameId, FirstQuestionIndex: 0, At);

    // Stage-1 reveal of question 0 (Mer): Martin correct (-10), Nils wrong (0).
    private static readonly GameEvent[] Q0DirectionRevealed =
    [
        new DirectionSubmitted(GameId, MartinId, 0, Direction.Mer, At),
        new DirectionSubmitted(GameId, NilsId, 0, Direction.Mindre, At),
        new QuestionDirectionRevealed(GameId, 0, Direction.Mer),
        new DirectionScored(GameId, 0, MartinId, Direction.Mer, DirectionCorrect: true, BonusPoints: -10),
        new DirectionScored(GameId, 0, NilsId, Direction.Mindre, DirectionCorrect: false, BonusPoints: 0)
    ];

    // Stage-2 score of question 0: Martin 30 (round 0), Nils 50 (round 10).
    private static readonly GameEvent[] Q0DifferenceScored =
    [
        new DifferenceSubmitted(GameId, MartinId, 0, 30m, At),
        new DifferenceSubmitted(GameId, NilsId, 0, 50m, At),
        new QuestionDifferenceRevealed(GameId, 0, CorrectDifference: 40),
        new DifferenceScored(GameId, 0, MartinId, 30m, 30, 10, RoundScore: 0, TotalScore: 0),
        new DifferenceScored(GameId, 0, NilsId, 50m, 50, 10, RoundScore: 10, TotalScore: 10)
    ];

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
    public void WaitingForOthersShowsWhoHasSubmittedADirectionAndWhoIsStillPending()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new DirectionSubmitted(GameId, MartinId, 0, Direction.Mer, At)
        ]);

        var view = Projections.WaitingForOthers(state);

        view.QuestionIndex.Should().Be(0);
        view.SubmittedPlayerIds.Should().Equal(MartinId);
        view.PendingPlayerIds.Should().Equal(NilsId);
    }

    [Fact]
    public void WaitingForOthersDerivesPendingFromDifferencesOnceDirectionRevealed()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            .. Q0DirectionRevealed,
            new DifferenceSubmitted(GameId, MartinId, 0, 30m, At)
        ]);

        var view = Projections.WaitingForOthers(state);

        view.QuestionIndex.Should().Be(0);
        view.SubmittedPlayerIds.Should().Equal(MartinId);
        view.PendingPlayerIds.Should().Equal(NilsId);
    }

    [Fact]
    public void DirectionResultsRevealsTheCorrectDirectionWithPerPlayerBonusAndRunningTotal()
    {
        var state = Decider.Fold([Lobby(), NilsJoins(), Started(), .. Q0DirectionRevealed]);

        var view = Projections.DirectionResults(state);

        view.QuestionIndex.Should().Be(0);
        view.CorrectDirection.Should().Be(Direction.Mer);
        view.PlayerDirections.Should().BeEquivalentTo(
            [
                new PlayerDirectionResult(MartinId, Direction.Mer, DirectionCorrect: true, BonusPoints: -10, TotalSoFar: -10),
                new PlayerDirectionResult(NilsId, Direction.Mindre, DirectionCorrect: false, BonusPoints: 0, TotalSoFar: 0)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void RoundResultsRevealsTheAnswerAndPerPlayerScoresOnceScored()
    {
        var state = Decider.Fold([Lobby(), NilsJoins(), Started(), .. Q0DirectionRevealed, .. Q0DifferenceScored]);

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
    public void OutstandingDirectionsEveryQuestionOpensForEveryPlayerWhenTheGameStarts()
    {
        var state = Decider.Fold([Lobby(), NilsJoins(), Started()]);

        var view = Projections.OutstandingDirections(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingDirection(0, [MartinId, NilsId], AllDirectionsIn: false),
                new OutstandingDirection(1, [MartinId, NilsId], AllDirectionsIn: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void OutstandingDirectionsASubmittedDirectionChecksOffThatPlayerOnItsQuestion()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new DirectionSubmitted(GameId, MartinId, 0, Direction.Mer, At)
        ]);

        var view = Projections.OutstandingDirections(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingDirection(0, [NilsId], AllDirectionsIn: false),
                new OutstandingDirection(1, [MartinId, NilsId], AllDirectionsIn: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void OutstandingDirectionsAQuestionShowsAllDirectionsInOnceEveryPlayerHasAnsweredIt()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            new DirectionSubmitted(GameId, MartinId, 0, Direction.Mer, At),
            new DirectionSubmitted(GameId, NilsId, 0, Direction.Mindre, At)
        ]);

        var view = Projections.OutstandingDirections(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingDirection(0, [], AllDirectionsIn: true),
                new OutstandingDirection(1, [MartinId, NilsId], AllDirectionsIn: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void OutstandingDifferencesGateOnTheRevealedDirectionAndCheckOffPerPlayer()
    {
        var state = Decider.Fold(
        [
            Lobby(), NilsJoins(), Started(),
            .. Q0DirectionRevealed,
            new DifferenceSubmitted(GameId, MartinId, 0, 30m, At)
        ]);

        var view = Projections.OutstandingDifferences(state);

        view.Questions.Should().BeEquivalentTo(
            [
                new OutstandingDifference(0, [NilsId], AllDifferencesIn: false, DirectionRevealed: true),
                new OutstandingDifference(1, [MartinId, NilsId], AllDifferencesIn: false, DirectionRevealed: false)
            ],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void GameProgressShowsANextQuestionWhileQuestionsRemain()
    {
        var state = Decider.Fold([Lobby(), NilsJoins(), Started(), .. Q0DirectionRevealed, .. Q0DifferenceScored]);

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
            .. Q0DirectionRevealed, .. Q0DifferenceScored,
            new NextQuestionStarted(GameId, QuestionIndex: 1),
            new DirectionSubmitted(GameId, MartinId, 1, Direction.Mindre, At),
            new DirectionSubmitted(GameId, NilsId, 1, Direction.Mindre, At),
            new QuestionDirectionRevealed(GameId, 1, Direction.Mindre),
            new DirectionScored(GameId, 1, MartinId, Direction.Mindre, DirectionCorrect: true, BonusPoints: -10),
            new DirectionScored(GameId, 1, NilsId, Direction.Mindre, DirectionCorrect: true, BonusPoints: -10),
            new DifferenceSubmitted(GameId, MartinId, 1, 25m, At),
            new DifferenceSubmitted(GameId, NilsId, 1, 20m, At),
            new QuestionDifferenceRevealed(GameId, 1, CorrectDifference: 20),
            new DifferenceScored(GameId, 1, MartinId, 25m, 25, 5, RoundScore: -5, TotalScore: -5),
            new DifferenceScored(GameId, 1, NilsId, 20m, 20, 0, RoundScore: -10, TotalScore: 0)
        ]);

        var view = Projections.GameProgress(state);

        view.QuestionIndex.Should().Be(1);
        view.TotalQuestions.Should().Be(2);
        view.ScoredQuestionCount.Should().Be(2);
        view.HasNextQuestion.Should().BeFalse();
    }
}
