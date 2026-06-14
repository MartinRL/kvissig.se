using AwesomeAssertions;
using Xunit;
using static MerEllerMindre.Domain.Tests.Fixtures;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Decider-true GWTs from specs/game-flows.yaml: given a GameState built DIRECTLY
/// (never event-replay), when a command, then events | an error. One per ✍️ slice.
/// </summary>
public class DeciderTests
{
    // --- Open Lobby ---------------------------------------------------------

    [Fact]
    public void GameCanBeCreated()
    {
        var events = Gwt.GivenInitial()
            .When(new OpenLobby(HostName: "Martin", QuestionPackId: "mer-eller-mindre"))
            .Events();

        var ev = events.Should().ContainSingle().Subject;
        if (ev is not LobbyOpened le) { Assert.Fail("expected LobbyOpened"); return; }
        le.HostName.Should().Be("Martin");
        le.QuestionPackId.Should().Be("mer-eller-mindre");
        le.Questions.Should().Equal(Question0, Question1);
    }

    [Fact]
    public void CannotOpenLobbyWithUnknownPack()
    {
        var err = Gwt.GivenInitial()
            .When(new OpenLobby(HostName: "Martin", QuestionPackId: "unknown"))
            .Error();

        (err.Error is QuestionPackNotFound).Should().BeTrue();
    }

    // --- Join Game ----------------------------------------------------------

    [Fact]
    public void PlayerCanJoinLobby()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            JoinCode = JoinCode,
            Phase = GamePhase.Lobby,
            Players = [HostMartin]
        };

        var events = Gwt.Given(state)
            .When(new JoinGame(JoinCode: JoinCode, PlayerName: "Nils"))
            .Events();

        var ev = events.Should().ContainSingle().Subject;
        if (ev is not PlayerJoined pj) { Assert.Fail("expected PlayerJoined"); return; }
        pj.PlayerName.Should().Be("Nils");
    }

    [Fact]
    public void CannotJoinNonexistentGame()
    {
        var err = Gwt.GivenInitial()
            .When(new JoinGame(JoinCode: JoinCode, PlayerName: "Nils"))
            .Error();

        (err.Error is GameNotFound).Should().BeTrue();
    }

    [Fact]
    public void CannotJoinStartedGame()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            JoinCode = JoinCode,
            Phase = GamePhase.Started,
            Players = [HostMartin, PlayerNils]
        };

        var err = Gwt.Given(state)
            .When(new JoinGame(JoinCode: JoinCode, PlayerName: "Sven"))
            .Error();

        (err.Error is GameAlreadyStarted).Should().BeTrue();
    }

    [Fact]
    public void CannotJoinWithNameAlreadyTaken()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            JoinCode = JoinCode,
            Phase = GamePhase.Lobby,
            Players = [HostMartin, PlayerNils]
        };

        var err = Gwt.Given(state)
            .When(new JoinGame(JoinCode: JoinCode, PlayerName: "Nils"))
            .Error();

        (err.Error is NameAlreadyTaken).Should().BeTrue();
    }

    // --- Start Game ---------------------------------------------------------

    [Fact]
    public void GameCanBeStarted()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            Phase = GamePhase.Lobby,
            Players = [HostMartin, PlayerNils]
        };

        var events = Gwt.Given(state)
            .When(new StartGame(GameId))
            .Events();

        var ev = events.Should().ContainSingle().Subject;
        if (ev is not GameStarted gs) { Assert.Fail("expected GameStarted"); return; }
        gs.FirstQuestionIndex.Should().Be(0);
    }

    [Fact]
    public void CannotStartNonexistentGame()
    {
        var err = Gwt.GivenInitial()
            .When(new StartGame(GameId))
            .Error();

        (err.Error is GameNotFound).Should().BeTrue();
    }

    [Fact]
    public void CannotStartWithoutEnoughPlayers()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            Phase = GamePhase.Lobby,
            Players = [HostMartin]
        };

        var err = Gwt.Given(state)
            .When(new StartGame(GameId))
            .Error();

        (err.Error is NotEnoughPlayers).Should().BeTrue();
    }

    // --- Submit Guess -------------------------------------------------------

    [Fact]
    public void GuessSubmittedSuccessfully()
    {
        var state = StartedAt(0, [Round(Question0)]);

        var events = Gwt.Given(state)
            .When(new SubmitGuess(GameId, NilsId, Direction.Mer, GuessedDifference: 30m))
            .Events();

        var ev = events.Should().ContainSingle().Subject;
        if (ev is not GuessSubmitted gs) { Assert.Fail("expected GuessSubmitted"); return; }
        gs.PlayerId.Should().Be(NilsId);
        gs.QuestionIndex.Should().Be(0);
        gs.Direction.Should().Be(Direction.Mer);
        gs.GuessedDifference.Should().Be(30m);
    }

    [Fact]
    public void CannotGuessInNonexistentGame()
    {
        var err = Gwt.GivenInitial()
            .When(new SubmitGuess(GameId, NilsId, Direction.Mer, 30m))
            .Error();

        (err.Error is GameNotFound).Should().BeTrue();
    }

    [Fact]
    public void CannotGuessBeforeGameStarts()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            Phase = GamePhase.Lobby,
            Players = [HostMartin, PlayerNils]
        };

        var err = Gwt.Given(state)
            .When(new SubmitGuess(GameId, NilsId, Direction.Mer, 30m))
            .Error();

        (err.Error is GameNotStarted).Should().BeTrue();
    }

    [Fact]
    public void CannotGuessAsNonMember()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            Phase = GamePhase.Started,
            Players = [HostMartin, PlayerNils]
        };

        var err = Gwt.Given(state)
            .When(new SubmitGuess(GameId, SvenId, Direction.Mer, 30m))
            .Error();

        (err.Error is PlayerNotInGame).Should().BeTrue();
    }

    [Fact]
    public void CannotGuessAgainOnSameQuestion()
    {
        var state = StartedAt(0, [Round(Question0, (NilsId, GuessMer30))]);

        var err = Gwt.Given(state)
            .When(new SubmitGuess(GameId, NilsId, Direction.Mindre, 20m))
            .Error();

        (err.Error is AlreadyGuessed).Should().BeTrue();
    }

    [Fact]
    public void NegativeDifferenceRejected()
    {
        var state = StartedAt(0, [Round(Question0)]);

        var err = Gwt.Given(state)
            .When(new SubmitGuess(GameId, NilsId, Direction.Mer, GuessedDifference: -5m))
            .Error();

        (err.Error is DifferenceOutOfRange).Should().BeTrue();
    }

    // --- Score Question -----------------------------------------------------

    [Fact]
    public void AllGuessesScoredAndAnswerRevealed()
    {
        var state = StartedAt(0,
        [
            Round(Question0, (MartinId, GuessMer30), (NilsId, GuessMindre50)),
            Round(Question1)
        ]);

        var events = Gwt.Given(state).When(new ScoreQuestion(GameId, 0)).Events();

        var answered = events.Answered();
        answered.CorrectDirection.Should().Be(Direction.Mer);
        answered.CorrectDifference.Should().Be((byte)40);

        var martin = events.ScoredFor(MartinId);
        martin.GuessedDirection.Should().Be(Direction.Mer);
        martin.GuessedDifferenceNormalized.Should().Be((byte)30);
        martin.DirectionCorrect.Should().BeTrue();
        martin.DifferencePoints.Should().Be((byte)10);
        martin.BonusPoints.Should().Be(-10);
        martin.RoundScore.Should().Be(0);
        martin.TotalScore.Should().Be(0);

        var nils = events.ScoredFor(NilsId);
        nils.GuessedDirection.Should().Be(Direction.Mindre);
        nils.GuessedDifferenceNormalized.Should().Be((byte)50);
        nils.DirectionCorrect.Should().BeFalse();
        nils.DifferencePoints.Should().Be((byte)10);
        nils.BonusPoints.Should().Be(0);
        nils.RoundScore.Should().Be(10);
        nils.TotalScore.Should().Be(10);
    }

    [Fact]
    public void ExactDifferenceWithCorrectDirection()
    {
        var state = StartedAt(0,
        [
            Round(Question0, (MartinId, GuessMer40), (NilsId, GuessMindre50)),
            Round(Question1)
        ]);

        var events = Gwt.Given(state).When(new ScoreQuestion(GameId, 0)).Events();

        var martin = events.ScoredFor(MartinId);
        martin.GuessedDifferenceNormalized.Should().Be((byte)40);
        martin.DirectionCorrect.Should().BeTrue();
        martin.DifferencePoints.Should().Be((byte)0);
        martin.BonusPoints.Should().Be(-10);
        martin.RoundScore.Should().Be(-10);
        martin.TotalScore.Should().Be(-10);
    }

    [Fact]
    public void ScoresAccumulateAcrossRounds()
    {
        var state = StartedAt(1,
        [
            Round(Question0, (MartinId, GuessMer30), (NilsId, GuessMindre50))
                with { CorrectDirection = Direction.Mer, CorrectDifference = 40, Scored = true, RoundScores = Scores((MartinId, 0), (NilsId, 10)) },
            Round(Question1, (MartinId, GuessMindre25), (NilsId, GuessMindre20))
        ]);

        var events = Gwt.Given(state).When(new ScoreQuestion(GameId, 1)).Events();

        var answered = events.Answered();
        answered.CorrectDirection.Should().Be(Direction.Mindre);
        answered.CorrectDifference.Should().Be((byte)20);

        var martin = events.ScoredFor(MartinId);
        martin.GuessedDifferenceNormalized.Should().Be((byte)25);
        martin.DirectionCorrect.Should().BeTrue();
        martin.DifferencePoints.Should().Be((byte)5);
        martin.RoundScore.Should().Be(-5);
        martin.TotalScore.Should().Be(-5);

        var nils = events.ScoredFor(NilsId);
        nils.GuessedDifferenceNormalized.Should().Be((byte)20);
        nils.DirectionCorrect.Should().BeTrue();
        nils.DifferencePoints.Should().Be((byte)0);
        nils.RoundScore.Should().Be(-10);
        nils.TotalScore.Should().Be(0);
    }

    [Fact]
    public void CannotScoreBeforeAllGuessesIn()
    {
        var state = StartedAt(0,
        [
            Round(Question0, (MartinId, GuessMer30)),
            Round(Question1)
        ]);

        var err = Gwt.Given(state).When(new ScoreQuestion(GameId, 0)).Error();

        (err.Error is NotAllGuessesIn).Should().BeTrue();
    }

    [Fact]
    public void CannotScoreAnAlreadyScoredQuestion()
    {
        var state = StartedAt(0,
        [
            Round(Question0, (MartinId, GuessMer30), (NilsId, GuessMindre50)) with { Scored = true },
            Round(Question1)
        ]);

        var err = Gwt.Given(state).When(new ScoreQuestion(GameId, 0)).Error();

        (err.Error is QuestionAlreadyScored).Should().BeTrue();
    }

    // --- Ask Next Question --------------------------------------------------

    [Fact]
    public void NextQuestionPresentedWhenOneRemains()
    {
        var state = StartedAt(0,
        [
            Round(Question0, (MartinId, GuessMer30), (NilsId, GuessMindre50))
                with { CorrectDirection = Direction.Mer, CorrectDifference = 40, Scored = true, RoundScores = Scores((MartinId, 0), (NilsId, 10)) },
            Round(Question1)
        ]);

        var events = Gwt.Given(state).When(new AskNextQuestion(GameId)).Events();

        var ev = events.Should().ContainSingle().Subject;
        if (ev is not NextQuestionStarted nq) { Assert.Fail("expected NextQuestionStarted"); return; }
        nq.QuestionIndex.Should().Be(1);
    }

    // --- End Game -----------------------------------------------------------

    [Fact]
    public void LowestScoreWins()
    {
        var state = StartedAt(1,
        [
            Round(Question0) with { Scored = true, RoundScores = Scores((MartinId, 0), (NilsId, 10)) },
            Round(Question1) with { Scored = true, RoundScores = Scores((MartinId, 5), (NilsId, 0)) }
        ]);

        var ended = Gwt.Given(state).When(new EndGame(GameId)).Events().Ended();

        ended.FinalScoreboard.Should().Equal(
            new ScoreboardEntry(MartinId, "Martin", 5),
            new ScoreboardEntry(NilsId, "Nils", 10));
        ended.WinnerIds.Should().Equal(MartinId);
    }

    [Fact]
    public void TiedLowestTotalsShareTheWin()
    {
        var state = StartedAt(1,
        [
            Round(Question0) with { Scored = true, RoundScores = Scores((MartinId, 5), (NilsId, 5)) },
            Round(Question1) with { Scored = true, RoundScores = Scores((MartinId, 5), (NilsId, 5)) }
        ]);

        var ended = Gwt.Given(state).When(new EndGame(GameId)).Events().Ended();

        ended.FinalScoreboard.Should().Equal(
            new ScoreboardEntry(MartinId, "Martin", 10),
            new ScoreboardEntry(NilsId, "Nils", 10));
        ended.WinnerIds.Should().Equal(MartinId, NilsId);
    }

    // --- builders -----------------------------------------------------------

    private static GameState StartedAt(int currentQuestionIndex, IReadOnlyList<QuestionRound> questions) =>
        GameState.Initial with
        {
            GameId = GameId,
            Phase = GamePhase.Started,
            Players = [HostMartin, PlayerNils],
            CurrentQuestionIndex = currentQuestionIndex,
            Questions = questions
        };

    private static QuestionRound Round(Question card, params (Guid PlayerId, Guess Guess)[] guesses) =>
        new()
        {
            Card = card,
            Guesses = guesses.ToDictionary(g => g.PlayerId, g => g.Guess)
        };

    private static Dictionary<Guid, int> Scores(params (Guid PlayerId, int Score)[] scores) =>
        scores.ToDictionary(s => s.PlayerId, s => s.Score);
}
