using AwesomeAssertions;
using Xunit;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Self-checks for the bounded brute-force Solver and the PuzzleGenerator. Non-trivial logic:
/// the solver's sample solution must actually replay to the target, and every generated puzzle
/// must be solvable (the generator's core guarantee).
/// </summary>
public class SolverTests
{
    [Fact]
    public void solve_hits_a_reachable_target_and_the_sample_replays_to_it()
    {
        var numbers = new[] { 10, 10 };
        var solution = Solver.Solve(numbers, 100);

        solution.Should().NotBeNull();
        SolutionValidator.Validate(new Puzzle(numbers, 100, solution!), solution!).Should().Be(100);
    }

    [Fact]
    public void solve_returns_null_for_an_unreachable_target()
    {
        // From {2, 3}: reachable via +−×÷ = {1, 2, 3, 5, 6}. 7 is unreachable.
        Solver.Solve([2, 3], 7).Should().BeNull();
    }

    [Fact]
    public void reachable_includes_the_starting_numbers_and_their_combinations()
    {
        var reachable = Solver.Reachable([2, 3]).Keys;
        reachable.Should().Contain([2, 3, 5, 6, 1]); // 3−2=1, 2+3=5, 2×3=6
    }

    [Fact]
    public void reachable_keeps_the_shortest_route_per_value()
    {
        // 24 = 2×3×4 (2 steps) but also 4×(2+3)+... — the kept sample must be minimal.
        var reachable = Solver.Reachable([2, 3, 4]);
        reachable[2].Steps.Should().BeEmpty();          // a starting tal needs 0 steps
        reachable[6].Steps.Should().HaveCount(1);       // 2×3
        reachable[24].Steps.Should().HaveCount(2);      // 2×3×4
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void every_generated_puzzle_is_solvable(int seed)
    {
        var rng = new Random(seed);
        var puzzle = PuzzleGenerator.Generate(Difficulty.Klassisk, rng.Next);

        puzzle.Numbers.Should().HaveCount(6);
        puzzle.Target.Should().BeInRange(101, 999);
        // The stamped sample solution must replay exactly to the target.
        SolutionValidator.Validate(puzzle, puzzle.SampleSolution).Should().Be(puzzle.Target);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void familj_targets_are_reachable_in_at_most_two_steps(int seed)
    {
        var rng = new Random(seed);
        var puzzle = PuzzleGenerator.Generate(Difficulty.Familj, rng.Next);

        // Reachable keeps the shortest route, so the stamped sample IS the minimum.
        puzzle.SampleSolution.Steps.Count.Should().BeLessThanOrEqualTo(2);
        SolutionValidator.Validate(puzzle, puzzle.SampleSolution).Should().Be(puzzle.Target);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void svår_targets_require_at_least_four_steps(int seed)
    {
        var rng = new Random(seed);
        var puzzle = PuzzleGenerator.Generate(Difficulty.Svår, rng.Next);

        // The shortest route needs >= 4 steps — no two-op shortcut exists to the target.
        puzzle.SampleSolution.Steps.Count.Should().BeGreaterThanOrEqualTo(4);
        SolutionValidator.Validate(puzzle, puzzle.SampleSolution).Should().Be(puzzle.Target);
    }
}
