using AwesomeAssertions;
using Xunit;
using static MerEllerMindre.Domain.Tests.Fixtures;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Decider-true GWTs from specs/mer-eller-mindre-event-model.yaml: given a GameState built DIRECTLY
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

    // --- Submit Direction (stage 1) -----------------------------------------

    [Fact]
    public void DirectionSubmittedSuccessfully()
    {
        var state = StartedAt(0, [Round(Question0)]);

        var events = Gwt.Given(state)
            .When(new SubmitDirection(GameId, NilsId, Direction.Mer))
            .Events();

        var ev = events.Should().ContainSingle().Subject;
        if (ev is not DirectionSubmitted ds) { Assert.Fail("expected DirectionSubmitted"); return; }
        ds.PlayerId.Should().Be(NilsId);
        ds.QuestionIndex.Should().Be(0);
        ds.Direction.Should().Be(Direction.Mer);
    }

    [Fact]
    public void CannotSubmitDirectionInNonexistentGame()
    {
        var err = Gwt.GivenInitial()
            .When(new SubmitDirection(GameId, NilsId, Direction.Mer))
            .Error();

        (err.Error is GameNotFound).Should().BeTrue();
    }

    [Fact]
    public void CannotSubmitDirectionBeforeGameStarts()
    {
        var state = GameState.Initial with
        {
            GameId = GameId,
            Phase = GamePhase.Lobby,
            Players = [HostMartin, PlayerNils]
        };

        var err = Gwt.Given(state)
            .When(new SubmitDirection(GameId, NilsId, Direction.Mer))
            .Error();

        (err.Error is GameNotStarted).Should().BeTrue();
    }

    [Fact]
    public void CannotSubmitDirectionAsNonMember()
    {
        var state = StartedAt(0, [Round(Question0)]);

        var err = Gwt.Given(state)
            .When(new SubmitDirection(GameId, SvenId, Direction.Mer))
            .Error();

        (err.Error is PlayerNotInGame).Should().BeTrue();
    }

    [Fact]
    public void CannotSubmitDirectionTwiceOnSameQuestion()
    {
        var state = StartedAt(0, [Round(Question0, Dirs((NilsId, Direction.Mer)))]);

        var err = Gwt.Given(state)
            .When(new SubmitDirection(GameId, NilsId, Direction.Mindre))
            .Error();

        (err.Error is AlreadySubmittedDirection).Should().BeTrue();
    }

    // --- Reveal Direction (mellansteg) --------------------------------------

    [Fact]
    public void DirectionRevealedAndBonusDealt()
    {
        var state = StartedAt(0,
        [
            Round(Question0, Dirs((MartinId, Direction.Mer), (NilsId, Direction.Mindre))),
            Round(Question1)
        ]);

        var events = Gwt.Given(state).When(new RevealDirection(GameId, 0)).Events();

        events.DirectionRevealed().CorrectDirection.Should().Be(Direction.Mer);

        var martin = events.DirectionScoredFor(MartinId);
        martin.GuessedDirection.Should().Be(Direction.Mer);
        martin.DirectionCorrect.Should().BeTrue();
        martin.BonusPoints.Should().Be(-10);

        var nils = events.DirectionScoredFor(NilsId);
        nils.GuessedDirection.Should().Be(Direction.Mindre);
        nils.DirectionCorrect.Should().BeFalse();
        nils.BonusPoints.Should().Be(0);
    }

    [Fact]
    public void CannotRevealBeforeAllDirectionsIn()
    {
        var state = StartedAt(0,
        [
            Round(Question0, Dirs((MartinId, Direction.Mer))),
            Round(Question1)
        ]);

        var err = Gwt.Given(state).When(new RevealDirection(GameId, 0)).Error();

        (err.Error is NotAllDirectionsIn).Should().BeTrue();
    }

    [Fact]
    public void CannotRevealAnAlreadyRevealedDirection()
    {
        var state = StartedAt(0,
        [
            Round(Question0, Dirs((MartinId, Direction.Mer), (NilsId, Direction.Mindre)))
                with { CorrectDirection = Direction.Mer },
            Round(Question1)
        ]);

        var err = Gwt.Given(state).When(new RevealDirection(GameId, 0)).Error();

        (err.Error is DirectionAlreadyRevealed).Should().BeTrue();
    }

    // --- Submit Difference (stage 2) ----------------------------------------

    [Fact]
    public void DifferenceSubmittedSuccessfully()
    {
        var state = StartedAt(0, [Revealed(Question0, Direction.Mer)]);

        var events = Gwt.Given(state)
            .When(new SubmitDifference(GameId, NilsId, GuessedDifference: 30m))
            .Events();

        var ev = events.Should().ContainSingle().Subject;
        if (ev is not DifferenceSubmitted ds) { Assert.Fail("expected DifferenceSubmitted"); return; }
        ds.PlayerId.Should().Be(NilsId);
        ds.QuestionIndex.Should().Be(0);
        ds.GuessedDifference.Should().Be(30m);
    }

    [Fact]
    public void CannotSubmitDifferenceBeforeDirectionRevealed()
    {
        var state = StartedAt(0, [Round(Question0, Dirs((MartinId, Direction.Mer), (NilsId, Direction.Mindre)))]);

        var err = Gwt.Given(state)
            .When(new SubmitDifference(GameId, NilsId, 30m))
            .Error();

        (err.Error is DirectionNotRevealed).Should().BeTrue();
    }

    [Fact]
    public void CannotSubmitDifferenceTwiceOnSameQuestion()
    {
        var state = StartedAt(0,
        [
            Revealed(Question0, Direction.Mer) with { Differences = Diffs((NilsId, 30m)) }
        ]);

        var err = Gwt.Given(state)
            .When(new SubmitDifference(GameId, NilsId, 20m))
            .Error();

        (err.Error is AlreadySubmittedDifference).Should().BeTrue();
    }

    [Fact]
    public void NegativeDifferenceRejected()
    {
        var state = StartedAt(0, [Revealed(Question0, Direction.Mer)]);

        var err = Gwt.Given(state)
            .When(new SubmitDifference(GameId, NilsId, GuessedDifference: -5m))
            .Error();

        (err.Error is DifferenceOutOfRange).Should().BeTrue();
    }

    // --- Score Difference ---------------------------------------------------

    [Fact]
    public void AllDifferencesScoredAndAnswerRevealed()
    {
        var state = StartedAt(0,
        [
            Revealed(Question0, Direction.Mer, (MartinId, Direction.Mer), (NilsId, Direction.Mindre))
                with { Differences = Diffs((MartinId, 30m), (NilsId, 50m)) },
            Round(Question1)
        ]);

        var events = Gwt.Given(state).When(new ScoreDifference(GameId, 0)).Events();

        events.DifferenceRevealed().CorrectDifference.Should().Be((byte)40);

        var martin = events.DifferenceScoredFor(MartinId);
        martin.GuessedDifferenceNormalized.Should().Be((byte)30);
        martin.DifferencePoints.Should().Be((byte)10);
        martin.RoundScore.Should().Be(0);
        martin.TotalScore.Should().Be(0);

        var nils = events.DifferenceScoredFor(NilsId);
        nils.GuessedDifferenceNormalized.Should().Be((byte)50);
        nils.DifferencePoints.Should().Be((byte)10);
        nils.RoundScore.Should().Be(10);
        nils.TotalScore.Should().Be(10);
    }

    [Fact]
    public void ExactDifferenceWithCorrectDirection()
    {
        var state = StartedAt(0,
        [
            Revealed(Question0, Direction.Mer, (MartinId, Direction.Mer), (NilsId, Direction.Mindre))
                with { Differences = Diffs((MartinId, 40m), (NilsId, 50m)) },
            Round(Question1)
        ]);

        var events = Gwt.Given(state).When(new ScoreDifference(GameId, 0)).Events();

        var martin = events.DifferenceScoredFor(MartinId);
        martin.GuessedDifferenceNormalized.Should().Be((byte)40);
        martin.DifferencePoints.Should().Be((byte)0);
        martin.RoundScore.Should().Be(-10);
        martin.TotalScore.Should().Be(-10);
    }

    [Fact]
    public void ScoresAccumulateAcrossRounds()
    {
        var state = StartedAt(1,
        [
            Round(Question0) with { CorrectDirection = Direction.Mer, CorrectDifference = 40, Scored = true, RoundScores = Scores((MartinId, 0), (NilsId, 10)) },
            Revealed(Question1, Direction.Mindre, (MartinId, Direction.Mindre), (NilsId, Direction.Mindre))
                with { Differences = Diffs((MartinId, 25m), (NilsId, 20m)) }
        ]);

        var events = Gwt.Given(state).When(new ScoreDifference(GameId, 1)).Events();

        events.DifferenceRevealed().CorrectDifference.Should().Be((byte)20);

        var martin = events.DifferenceScoredFor(MartinId);
        martin.GuessedDifferenceNormalized.Should().Be((byte)25);
        martin.DifferencePoints.Should().Be((byte)5);
        martin.RoundScore.Should().Be(-5);
        martin.TotalScore.Should().Be(-5);

        var nils = events.DifferenceScoredFor(NilsId);
        nils.GuessedDifferenceNormalized.Should().Be((byte)20);
        nils.DifferencePoints.Should().Be((byte)0);
        nils.RoundScore.Should().Be(-10);
        nils.TotalScore.Should().Be(0);
    }

    [Fact]
    public void CannotScoreBeforeAllDifferencesIn()
    {
        var state = StartedAt(0,
        [
            Revealed(Question0, Direction.Mer) with { Differences = Diffs((MartinId, 30m)) },
            Round(Question1)
        ]);

        var err = Gwt.Given(state).When(new ScoreDifference(GameId, 0)).Error();

        (err.Error is NotAllDifferencesIn).Should().BeTrue();
    }

    [Fact]
    public void CannotScoreAnAlreadyScoredQuestion()
    {
        var state = StartedAt(0,
        [
            Revealed(Question0, Direction.Mer) with { Differences = Diffs((MartinId, 30m), (NilsId, 50m)), Scored = true },
            Round(Question1)
        ]);

        var err = Gwt.Given(state).When(new ScoreDifference(GameId, 0)).Error();

        (err.Error is QuestionAlreadyScored).Should().BeTrue();
    }

    // --- Ask Next Question --------------------------------------------------

    [Fact]
    public void NextQuestionPresentedWhenOneRemains()
    {
        var state = StartedAt(0,
        [
            Round(Question0) with { CorrectDirection = Direction.Mer, CorrectDifference = 40, Scored = true, RoundScores = Scores((MartinId, 0), (NilsId, 10)) },
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

    private static QuestionRound Round(Question card, IReadOnlyDictionary<Guid, Direction>? directions = null) =>
        new()
        {
            Card = card,
            Directions = directions ?? new Dictionary<Guid, Direction>()
        };

    /// <summary>A round whose stage-1 direction is already revealed: both players answered,
    /// CorrectDirection set, and the −10/0 bonus folded into DirectionScores.</summary>
    private static QuestionRound Revealed(
        Question card, Direction correctDirection, params (Guid PlayerId, Direction Direction)[] answers)
    {
        var dirs = answers.Length > 0
            ? answers.ToDictionary(a => a.PlayerId, a => a.Direction)
            : new Dictionary<Guid, Direction> { [MartinId] = correctDirection, [NilsId] = correctDirection };
        return new QuestionRound
        {
            Card = card,
            Directions = dirs,
            CorrectDirection = correctDirection,
            DirectionScores = dirs.ToDictionary(kv => kv.Key, kv => kv.Value == correctDirection ? -10 : 0)
        };
    }

    private static Dictionary<Guid, Direction> Dirs(params (Guid PlayerId, Direction Direction)[] directions) =>
        directions.ToDictionary(d => d.PlayerId, d => d.Direction);

    private static Dictionary<Guid, decimal> Diffs(params (Guid PlayerId, decimal Difference)[] differences) =>
        differences.ToDictionary(d => d.PlayerId, d => d.Difference);

    private static Dictionary<Guid, int> Scores(params (Guid PlayerId, int Score)[] scores) =>
        scores.ToDictionary(s => s.PlayerId, s => s.Score);
}
