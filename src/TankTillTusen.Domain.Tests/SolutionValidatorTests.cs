using AwesomeAssertions;
using Xunit;
using static TankTillTusen.Domain.Tests.Fixtures;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Self-check for the trust-boundary replay (SolutionValidator). Non-trivial logic: rejects
/// reused/missing operands, uneven ÷, non-positive − results, and out-of-range answers.
/// </summary>
public class SolutionValidatorTests
{
    private static Puzzle P(params int[] numbers) => new(numbers, 0, SampleSol0);

    [Fact]
    public void happy_replay_returns_the_answer_operand()
    {
        SolutionValidator.Validate(Puzzle0, SolHit).Should().Be(100); // 10×10
        SolutionValidator.Validate(Puzzle0, SolMiss).Should().Be(20); // 10+10
    }

    [Fact]
    public void a_starting_operand_is_a_valid_answer()
    {
        SolutionValidator.Validate(Puzzle0, new Solution([], 0)).Should().Be(10);
    }

    [Fact]
    public void subtraction_that_is_not_positive_is_rejected()
    {
        SolutionValidator.Validate(Puzzle0, SolBad).Should().BeNull(); // 10−10=0
    }

    [Fact]
    public void uneven_division_is_rejected()
    {
        SolutionValidator.Validate(P(10, 3), new Solution([new Step(0, Operator.Div, 1)], 2)).Should().BeNull();
    }

    [Fact]
    public void reusing_a_consumed_operand_is_rejected()
    {
        // step0 consumes operands 0 and 1; step1 reuses operand 0 -> illegal.
        var solution = new Solution([new Step(0, Operator.Add, 1), new Step(0, Operator.Mul, 2)], 3);
        SolutionValidator.Validate(Puzzle0, solution).Should().BeNull();
    }

    [Fact]
    public void combining_an_operand_with_itself_is_rejected()
    {
        SolutionValidator.Validate(Puzzle0, new Solution([new Step(0, Operator.Mul, 0)], 2)).Should().BeNull();
    }

    [Fact]
    public void a_missing_operand_index_is_rejected()
    {
        SolutionValidator.Validate(Puzzle0, new Solution([new Step(0, Operator.Mul, 5)], 2)).Should().BeNull();
    }

    [Fact]
    public void an_answer_index_out_of_range_is_rejected()
    {
        SolutionValidator.Validate(Puzzle0, new Solution([new Step(0, Operator.Mul, 1)], 9)).Should().BeNull();
    }
}
