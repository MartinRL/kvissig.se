using AwesomeAssertions;
using Xunit;
using static TankTillTusen.Domain.Tests.Fixtures;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Projection GTs: given prior Game-stream events (folded via Decider.Fold), then the view.
/// One test per read-model `tests:` case in specs/tank-till-tusen-event-model.yaml.
/// </summary>
public class ProjectionTests
{
    private static readonly DateTimeOffset Deadline = StartedAt.AddSeconds(Decider.CountdownSeconds);

    private static LobbyOpened Opened() =>
        new(GameId, MartinId, "Martin", JoinCode, Difficulty.Klassisk, [Puzzle0, Puzzle1], StartedAt);

    private static TankState Fold(params TankEvent[] events) => Decider.Fold(events);

    [Fact]
    public void lobby_lists_the_host_and_joined_players()
    {
        var view = Projections.GameLobby(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt)));

        view.JoinCode.Should().Be(JoinCode);
        view.Players.Should().Equal(HostMartin, PlayerNils);
    }

    [Fact]
    public void current_puzzle_presented_with_tal_mal_and_progress()
    {
        var view = Projections.Puzzle(Fold(
            Opened(),
            new GameStarted(GameId, 0, StartedAt)));

        view.RoundIndex.Should().Be(0);
        view.TotalRounds.Should().Be(2);
        view.Numbers.Should().Equal(10, 10);
        view.Target.Should().Be(100);
        view.Deadline.Should().Be(Deadline);
    }

    [Fact]
    public void shows_who_has_submitted_and_who_is_still_pending()
    {
        var view = Projections.WaitingForOthers(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new SolutionSubmitted(GameId, MartinId, 0, SolMiss, StartedAt)));

        view.RoundIndex.Should().Be(0);
        view.SubmittedPlayerIds.Should().Equal(MartinId);
        view.PendingPlayerIds.Should().Equal(NilsId);
    }

    [Fact]
    public void every_round_opens_for_every_player_when_the_game_starts()
    {
        var view = Projections.OutstandingSolutions(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt)));

        view.Rounds.Should().BeEquivalentTo(new[]
        {
            new OutstandingSolution(0, [MartinId, NilsId], false, Deadline),
            new OutstandingSolution(1, [MartinId, NilsId], false, null)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void a_submitted_solution_checks_off_that_player_on_its_round()
    {
        var view = Projections.OutstandingSolutions(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new SolutionSubmitted(GameId, MartinId, 0, SolMiss, StartedAt)));

        view.Rounds.Should().BeEquivalentTo(new[]
        {
            new OutstandingSolution(0, [NilsId], false, Deadline),
            new OutstandingSolution(1, [MartinId, NilsId], false, null)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void a_round_shows_all_solutions_in_once_every_player_has_submitted()
    {
        var view = Projections.OutstandingSolutions(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new SolutionSubmitted(GameId, MartinId, 0, SolMiss, StartedAt),
            new SolutionSubmitted(GameId, NilsId, 0, SolHit, StartedAt)));

        view.Rounds.Should().BeEquivalentTo(new[]
        {
            new OutstandingSolution(0, [], true, Deadline),
            new OutstandingSolution(1, [MartinId, NilsId], false, null)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void reveals_the_target_sample_solution_and_per_player_result_once_scored()
    {
        var view = Projections.RoundResults(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new SolutionSubmitted(GameId, MartinId, 0, SolMiss, StartedAt),
            new SolutionSubmitted(GameId, NilsId, 0, SolHit, StartedAt),
            new PuzzleRevealed(GameId, 0, SampleSol0),
            new RoundScored(GameId, 0, MartinId, 20, 80, 80),
            new RoundScored(GameId, 0, NilsId, 100, -10, -10)));

        view.RoundIndex.Should().Be(0);
        view.Target.Should().Be(100);
        view.SampleSolution.Should().Be(SampleSol0);
        view.PlayerResults.Should().BeEquivalentTo(new[]
        {
            new PlayerResult(MartinId, 20, 80, 80),
            new PlayerResult(NilsId, 100, -10, -10)
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void progress_shows_a_next_puzzle_while_rounds_remain()
    {
        var view = Projections.GameProgress(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new PuzzleRevealed(GameId, 0, SampleSol0)));

        view.RoundIndex.Should().Be(0);
        view.TotalRounds.Should().Be(2);
        view.ResolvedRoundCount.Should().Be(1);
        view.HasNextPuzzle.Should().BeTrue();
    }

    [Fact]
    public void progress_shows_no_next_puzzle_once_the_last_is_scored()
    {
        var view = Projections.GameProgress(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameStarted(GameId, 0, StartedAt),
            new PuzzleRevealed(GameId, 0, SampleSol0),
            new NextPuzzleStarted(GameId, 1, StartedAt),
            new PuzzleRevealed(GameId, 1, SampleSol1)));

        view.RoundIndex.Should().Be(1);
        view.TotalRounds.Should().Be(2);
        view.ResolvedRoundCount.Should().Be(2);
        view.HasNextPuzzle.Should().BeFalse();
    }

    [Fact]
    public void shows_the_final_scoreboard_and_winner()
    {
        var scoreboard = new[]
        {
            new ScoreboardEntry(MartinId, "Martin", 5),
            new ScoreboardEntry(NilsId, "Nils", 80)
        };

        var view = Projections.FinalStandings(Fold(
            Opened(),
            new PlayerJoined(GameId, NilsId, "Nils", StartedAt),
            new GameEnded(GameId, scoreboard, [MartinId], StartedAt)));

        view.FinalScoreboard.Should().BeEquivalentTo(scoreboard, o => o.WithStrictOrdering());
        view.WinnerIds.Should().Equal(MartinId);
    }
}
