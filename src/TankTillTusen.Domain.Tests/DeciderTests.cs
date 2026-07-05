using AwesomeAssertions;
using Xunit;
using static TankTillTusen.Domain.Tests.Fixtures;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Decider-true GWTs: given the folded State / Game props `decide` reads, when a command, then
/// events | rejection. One test per `tests:` case in specs/tank-till-tusen-event-model.yaml.
/// Union case checks use the CONCRETE `is`-pattern (the union runtime-type trap).
/// </summary>
public class DeciderTests
{
    private static TankState State(
        TankPhase phase,
        IReadOnlyList<Player> players,
        int currentRoundIndex,
        params PuzzleRound[] rounds) =>
        new()
        {
            GameId = GameId,
            JoinCode = JoinCode,
            Phase = phase,
            HostPlayerId = MartinId,
            Players = players,
            CurrentRoundIndex = currentRoundIndex,
            Rounds = rounds
        };

    // --- Open Lobby ----------------------------------------------------------

    [Fact]
    public void game_can_be_created()
    {
        var opened = Gwt.GivenInitial()
            .When(new OpenLobby("Martin"))
            .Events()
            .Opened();

        opened.HostName.Should().Be("Martin");
        opened.Puzzles.Should().Equal(Puzzle0, Puzzle1);
        opened.HostPlayerId.Should().NotBe(Guid.Empty);
        opened.JoinCode.Should().NotBe(Guid.Empty);
    }

    // --- Join Game -----------------------------------------------------------

    [Fact]
    public void player_can_join_lobby()
    {
        var joined = Gwt.Given(State(TankPhase.Lobby, [HostMartin], -1))
            .When(new JoinGame(JoinCode, "Nils"))
            .Events()
            .Joined();

        joined.PlayerName.Should().Be("Nils");
        joined.PlayerId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void cannot_join_nonexistent_game()
    {
        var error = Gwt.GivenInitial()
            .When(new JoinGame(JoinCode, "Nils"))
            .Error();

        (error.Error is GameNotFound).Should().BeTrue();
    }

    [Fact]
    public void cannot_join_started_game()
    {
        var error = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0))
            .When(new JoinGame(JoinCode, "Sven"))
            .Error();

        (error.Error is GameAlreadyStarted).Should().BeTrue();
    }

    [Fact]
    public void cannot_join_with_name_already_taken()
    {
        var error = Gwt.Given(State(TankPhase.Lobby, [HostMartin, PlayerNils], -1))
            .When(new JoinGame(JoinCode, "Nils"))
            .Error();

        (error.Error is NameAlreadyTaken).Should().BeTrue();
    }

    // --- Start Game ----------------------------------------------------------

    [Fact]
    public void game_can_be_started()
    {
        var started = Gwt.Given(State(TankPhase.Lobby, [HostMartin, PlayerNils], -1))
            .When(new StartGame(GameId))
            .Events()
            .Started();

        started.FirstRoundIndex.Should().Be(0);
    }

    [Fact]
    public void cannot_start_nonexistent_game()
    {
        var error = Gwt.GivenInitial()
            .When(new StartGame(GameId))
            .Error();

        (error.Error is GameNotFound).Should().BeTrue();
    }

    [Fact]
    public void cannot_start_without_enough_players()
    {
        var error = Gwt.Given(State(TankPhase.Lobby, [HostMartin], -1))
            .When(new StartGame(GameId))
            .Error();

        (error.Error is NotEnoughPlayers).Should().BeTrue();
    }

    // --- Submit Solution -----------------------------------------------------

    [Fact]
    public void solution_submitted_successfully()
    {
        var submitted = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Puzzle0, startedAt: StartedAt)))
            .When(new SubmitSolution(GameId, NilsId, 0, SolHit))
            .Events()
            .Submitted();

        submitted.PlayerId.Should().Be(NilsId);
        submitted.RoundIndex.Should().Be(0);
        submitted.Solution.Should().Be(SolHit);
    }

    [Fact]
    public void cannot_submit_in_nonexistent_game()
    {
        var error = Gwt.GivenInitial()
            .When(new SubmitSolution(GameId, NilsId, 0, SolHit))
            .Error();

        (error.Error is GameNotFound).Should().BeTrue();
    }

    [Fact]
    public void invalid_solution_rejected()
    {
        var error = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Puzzle0, startedAt: StartedAt)))
            .When(new SubmitSolution(GameId, NilsId, 0, SolBad)) // 10−10=0, not positive
            .Error();

        (error.Error is InvalidSolution).Should().BeTrue();
    }

    [Fact]
    public void cannot_submit_twice_on_same_round()
    {
        var error = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Puzzle0, startedAt: StartedAt, solutions: new Dictionary<Guid, Solution> { [NilsId] = SolMiss })))
            .When(new SubmitSolution(GameId, NilsId, 0, SolHit))
            .Error();

        (error.Error is AlreadySubmitted).Should().BeTrue();
    }

    [Fact]
    public void cannot_submit_after_the_deadline()
    {
        var error = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Puzzle0, startedAt: StartedAtExpired)))
            .When(new SubmitSolution(GameId, NilsId, 0, SolHit))
            .Error();

        (error.Error is DeadlinePassed).Should().BeTrue();
    }

    [Fact]
    public void cannot_submit_to_a_scored_round()
    {
        var scored = Round(Puzzle0, startedAt: StartedAt,
            solutions: new Dictionary<Guid, Solution> { [MartinId] = SolMiss, [NilsId] = SolHit },
            sampleSolution: SampleSol0,
            reachedValues: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 100 },
            roundScores: new Dictionary<Guid, int> { [MartinId] = 80, [NilsId] = 0 },
            scored: true);

        var error = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0, scored))
            .When(new SubmitSolution(GameId, MartinId, 0, SolHit))
            .Error();

        (error.Error is RoundAlreadyScored).Should().BeTrue();
    }

    // --- Score Round ---------------------------------------------------------

    [Fact]
    public void exact_solution_scores_zero_a_miss_scores_by_distance()
    {
        var events = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Puzzle0, startedAt: StartedAt,
                    solutions: new Dictionary<Guid, Solution> { [MartinId] = SolMiss, [NilsId] = SolHit }),
                Round(Puzzle1)))
            .When(new ScoreRound(GameId, 0))
            .Events();

        events.Revealed().SampleSolution.Should().Be(SampleSol0);
        events.ScoredFor(MartinId).Should().BeEquivalentTo(new { ReachedValue = (int?)20, RoundScore = 80, TotalScore = 80 });
        events.ScoredFor(NilsId).Should().BeEquivalentTo(new { ReachedValue = (int?)100, RoundScore = 0, TotalScore = 0 });
    }

    [Fact]
    public void a_non_submitter_scores_the_worst_once_the_deadline_passes()
    {
        var events = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Puzzle0, startedAt: StartedAtExpired,
                    solutions: new Dictionary<Guid, Solution> { [MartinId] = SolHit }),
                Round(Puzzle1)))
            .When(new ScoreRound(GameId, 0))
            .Events();

        events.Revealed().SampleSolution.Should().Be(SampleSol0);
        events.ScoredFor(MartinId).Should().BeEquivalentTo(new { ReachedValue = (int?)100, RoundScore = 0, TotalScore = 0 });
        events.ScoredFor(NilsId).Should().BeEquivalentTo(new { ReachedValue = (int?)null, RoundScore = 100, TotalScore = 100 });
    }

    [Fact]
    public void scores_accumulate_across_rounds()
    {
        var round0 = Round(Puzzle0, sampleSolution: SampleSol0,
            reachedValues: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 100 },
            roundScores: new Dictionary<Guid, int> { [MartinId] = 80, [NilsId] = 0 },
            scored: true);
        var round1 = Round(Puzzle1, startedAt: StartedAt,
            solutions: new Dictionary<Guid, Solution> { [MartinId] = SolHit, [NilsId] = SolMiss });

        var events = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 1, round0, round1))
            .When(new ScoreRound(GameId, 1))
            .Events();

        events.Revealed().SampleSolution.Should().Be(SampleSol1);
        // solHit 5×20=100 (exact); running 80 + 0.
        events.ScoredFor(MartinId).Should().BeEquivalentTo(new { ReachedValue = (int?)100, RoundScore = 0, TotalScore = 80 });
        // solMiss 5+20=25 on puzzle1; |25−100|/100*100 = 75; running 0 + 75.
        events.ScoredFor(NilsId).Should().BeEquivalentTo(new { ReachedValue = (int?)25, RoundScore = 75, TotalScore = 75 });
    }

    [Fact]
    public void cannot_score_before_ready()
    {
        var error = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0,
                Round(Puzzle0, startedAt: StartedAt,
                    solutions: new Dictionary<Guid, Solution> { [MartinId] = SolHit }), // nilsId pending, within window
                Round(Puzzle1)))
            .When(new ScoreRound(GameId, 0))
            .Error();

        (error.Error is NotReadyToScore).Should().BeTrue();
    }

    [Fact]
    public void cannot_score_an_already_scored_round()
    {
        var scored = Round(Puzzle0, startedAt: StartedAt,
            solutions: new Dictionary<Guid, Solution> { [MartinId] = SolMiss, [NilsId] = SolHit },
            sampleSolution: SampleSol0,
            reachedValues: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 100 },
            roundScores: new Dictionary<Guid, int> { [MartinId] = 80, [NilsId] = 0 },
            scored: true);

        var error = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0, scored, Round(Puzzle1)))
            .When(new ScoreRound(GameId, 0))
            .Error();

        (error.Error is RoundAlreadyScored).Should().BeTrue();
    }

    // --- Ask Next Puzzle -----------------------------------------------------

    [Fact]
    public void next_puzzle_presented_when_one_remains()
    {
        var round0 = Round(Puzzle0, sampleSolution: SampleSol0,
            reachedValues: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 100 },
            roundScores: new Dictionary<Guid, int> { [MartinId] = 80, [NilsId] = 0 },
            scored: true);

        var next = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 0, round0, Round(Puzzle1)))
            .When(new AskNextPuzzle(GameId))
            .Events()
            .NextPuzzle();

        next.RoundIndex.Should().Be(1);
    }

    // --- End Game ------------------------------------------------------------

    [Fact]
    public void lowest_score_wins()
    {
        var round0 = Round(Puzzle0, roundScores: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 80 }, scored: true);
        var round1 = Round(Puzzle1, roundScores: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 0 }, scored: true);

        var ended = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 1, round0, round1))
            .When(new EndGame(GameId))
            .Events()
            .Ended();

        ended.FinalScoreboard.Should().BeEquivalentTo(new[]
        {
            new ScoreboardEntry(MartinId, "Martin", 5),
            new ScoreboardEntry(NilsId, "Nils", 80)
        }, o => o.WithStrictOrdering());
        ended.WinnerIds.Should().Equal(MartinId);
    }

    [Fact]
    public void tied_lowest_totals_share_the_win()
    {
        var round0 = Round(Puzzle0, roundScores: new Dictionary<Guid, int> { [MartinId] = 10, [NilsId] = 10 }, scored: true);
        var round1 = Round(Puzzle1, roundScores: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 5 }, scored: true);

        var ended = Gwt.Given(State(TankPhase.Started, [HostMartin, PlayerNils], 1, round0, round1))
            .When(new EndGame(GameId))
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
