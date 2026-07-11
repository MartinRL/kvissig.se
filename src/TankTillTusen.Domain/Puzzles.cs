namespace TankTillTusen.Domain;

/// <summary>The four räknesätt a step may apply.</summary>
public enum Operator
{
    Add,
    Sub,
    Mul,
    Div
}

/// <summary>
/// One step of a solution: combine the operand at <see cref="LeftIndex"/> with the operand at
/// <see cref="RightIndex"/> using <see cref="Op"/>, appending the result as a NEW operand.
/// Indices point into the operand list as it grows (0..n-1 are the starting tal; each result
/// takes the next index).
/// </summary>
public record Step(
    int LeftIndex,
    Operator Op,
    int RightIndex
);

/// <summary>
/// A replayable build: an ordered list of steps plus the operand index the player claims as
/// the answer. Validated by replay (SolutionValidator).
/// </summary>
public record Solution(
    IReadOnlyList<Step> Steps,
    int AnswerIndex
);

/// <summary>
/// A generated puzzle: six tal, a mål, and the generator's hidden sample solution (proof the
/// puzzle is solvable, surfaced only at reveal — like BlindBudet's hidden trueWorth). Screen /
/// Puzzle shows Numbers + Target only.
/// </summary>
public record Puzzle(
    IReadOnlyList<int> Numbers,
    int Target,
    Solution SampleSolution
);

/// <summary>
/// Replays a <see cref="Solution"/> against a <see cref="Puzzle"/> — the server-side trust
/// boundary. Pure, total, no throw. Non-trivial logic → self-checked by SolutionValidatorTests.
/// </summary>
public static class SolutionValidator
{
    /// <summary>
    /// Replay the steps and return the value at AnswerIndex, or null if the build is illegal:
    /// an operand index is missing or already consumed, the two operands of a step are the same,
    /// a result is not a positive integer (− stays &gt; 0, ÷ divides evenly), or AnswerIndex is
    /// out of range.
    /// </summary>
    public static int? Validate(Puzzle puzzle, Solution solution)
    {
        var values = new List<int>(puzzle.Numbers);
        var consumed = new List<bool>(new bool[values.Count]);

        foreach (var step in solution.Steps)
        {
            if (step.LeftIndex == step.RightIndex) return null;
            if (!Available(step.LeftIndex, values.Count, consumed)) return null;
            if (!Available(step.RightIndex, values.Count, consumed)) return null;

            var result = Apply(step.Op, values[step.LeftIndex], values[step.RightIndex]);
            if (result is not { } value) return null;

            consumed[step.LeftIndex] = true;
            consumed[step.RightIndex] = true;
            values.Add(value);
            consumed.Add(false);
        }

        if (solution.AnswerIndex < 0 || solution.AnswerIndex >= values.Count) return null;
        return values[solution.AnswerIndex];
    }

    private static bool Available(int index, int count, List<bool> consumed) =>
        index >= 0 && index < count && !consumed[index];

    /// <summary>
    /// Apply an operator to two positive operands, returning the result only if it is a positive
    /// integer (else null). Div's fall-through branch is Div by construction (enum is closed).
    /// </summary>
    internal static int? Apply(Operator op, int left, int right)
    {
        if (op == Operator.Add) return left + right;
        if (op == Operator.Sub) return left > right ? left - right : null;
        if (op == Operator.Mul) return left * right;
        return right != 0 && left % right == 0 ? left / right : null; // Div
    }
}

/// <summary>
/// Bounded brute-force Countdown solver. Enumerates every positive-integer value reachable by
/// combining subsets of the tal, with one sample solution each. Pure, no throw.
///
/// ponytail: exponential in the tal count, but that count is fixed at 6 and generation runs a
/// handful of times per game — a memoised multiset search is not worth the complexity yet.
/// Non-trivial logic → self-checked by SolverTests.
/// </summary>
public static class Solver
{
    private static readonly Operator[] Operators =
        [Operator.Add, Operator.Sub, Operator.Mul, Operator.Div];

    // Mutable accumulators threaded through the recursion. NextIndex is derived, not stored: every
    // step appends exactly one operand, so the next fresh index is always Base + Steps.Count.
    private sealed record Search(int Base, List<Step> Steps, Dictionary<int, Solution> Found)
    {
        public int NextIndex => Base + Steps.Count;
    }

    /// <summary>Every reachable value mapped to its SHORTEST sample solution (fewest steps).</summary>
    public static IReadOnlyDictionary<int, Solution> Reachable(IReadOnlyList<int> numbers)
    {
        var search = new Search(numbers.Count, [], []);
        var items = numbers.Select((v, i) => (Value: v, Index: i)).ToList();
        Explore(items, search);
        return search.Found;
    }

    /// <summary>A solution reaching exactly <paramref name="target"/>, or null if unreachable.</summary>
    public static Solution? Solve(IReadOnlyList<int> numbers, int target) =>
        Reachable(numbers).TryGetValue(target, out var s) ? s : null;

    private static void Explore(List<(int Value, int Index)> items, Search search)
    {
        // Keep the shortest route per value: DFS visits every combine order, so the minimum
        // Steps.Count over all visits IS the true minimum — the difficulty knob reads it.
        foreach (var it in items)
            if (!search.Found.TryGetValue(it.Value, out var known) || known.Steps.Count > search.Steps.Count)
                search.Found[it.Value] = new Solution([.. search.Steps], it.Index);

        for (var a = 0; a < items.Count; a++)
        for (var b = a + 1; b < items.Count; b++)
            Combine(items, a, b, search);
    }

    /// <summary>Try every operator on the pair (a, b), recursing into the reduced item list.</summary>
    private static void Combine(List<(int Value, int Index)> items, int a, int b, Search search)
    {
        // Order operands high-then-low so − and ÷ stay positive / integer.
        var (hi, lo) = items[a].Value >= items[b].Value ? (items[a], items[b]) : (items[b], items[a]);
        var rest = Without(items, a, b);

        foreach (var op in Operators)
        {
            if (SolutionValidator.Apply(op, hi.Value, lo.Value) is not { } value) continue;

            rest.Add((value, search.NextIndex));
            search.Steps.Add(new Step(hi.Index, op, lo.Index));
            Explore(rest, search);
            search.Steps.RemoveAt(search.Steps.Count - 1);
            rest.RemoveAt(rest.Count - 1);
        }
    }

    /// <summary>The item list with the two combined operands (at a, b) removed.</summary>
    private static List<(int Value, int Index)> Without(List<(int Value, int Index)> items, int a, int b)
    {
        var rest = new List<(int Value, int Index)>();
        for (var k = 0; k < items.Count; k++)
            if (k != a && k != b)
                rest.Add(items[k]);
        return rest;
    }
}

/// <summary>
/// The catalog's three nivåer. The generation knob is the MINIMUM number of steps the target
/// requires (the true difficulty dial of a Countdown puzzle): Familj &lt;= 2, Klassisk
/// unfiltered, Svår &gt;= 4.
/// </summary>
public enum Difficulty
{
    Familj,
    Klassisk,
    Svår
}

/// <summary>
/// Draws a solvable puzzle: six tal from the Countdown pool + a mål in 101..999 that is
/// guaranteed reachable (the target is picked FROM the solver's reachable set, so the sample
/// solution always hits it), filtered by the difficulty knob. Pure over the injected random
/// source. Non-trivial logic → self-checked by PuzzleGeneratorTests.
/// </summary>
public static class PuzzleGenerator
{
    // Countdown pool: two of each 1..10 plus the four "large" tal.
    private static readonly IReadOnlyList<int> Pool =
        [.. Enumerable.Range(1, 10), .. Enumerable.Range(1, 10), 25, 50, 75, 100];

    private const int NumbersPerPuzzle = 6;
    private const int MinTarget = 101;
    private const int MaxTarget = 999;

    /// <summary>Generate <paramref name="count"/> independent solvable puzzles.</summary>
    public static IReadOnlyList<Puzzle> GenerateSet(int count, Difficulty difficulty, Func<int, int> nextRandom) =>
        [.. Enumerable.Range(0, count).Select(_ => Generate(difficulty, nextRandom))];

    public static Puzzle Generate(Difficulty difficulty, Func<int, int> nextRandom)
    {
        // ponytail: redraw when a hand has no in-range target matching the difficulty knob.
        // Terminates with probability 1 (most 6-tal hands reach many in-range values on every
        // nivå); no fixed cap needed.
        while (true)
        {
            var numbers = Draw(nextRandom);
            var reachable = Solver.Reachable(numbers);
            var targets = reachable
                .Where(kv => kv.Key >= MinTarget && kv.Key <= MaxTarget
                             && MatchesDifficulty(difficulty, kv.Value.Steps.Count))
                .Select(kv => kv.Key)
                .ToList();
            if (targets.Count == 0)
                continue;

            var target = targets[nextRandom(targets.Count)];
            return new Puzzle(numbers, target, reachable[target]);
        }
    }

    /// <summary>
    /// The difficulty filter over a target's MINIMUM solution length (Reachable keeps the
    /// shortest route). Klassisk's fall-through is Klassisk by construction (enum is closed).
    /// </summary>
    private static bool MatchesDifficulty(Difficulty difficulty, int minSteps)
    {
        if (difficulty == Difficulty.Familj) return minSteps <= 2;
        if (difficulty == Difficulty.Svår) return minSteps >= 4;
        return true; // Klassisk — unfiltered
    }

    /// <summary>Fisher-Yates shuffle a copy of the pool, then take the first six.</summary>
    private static IReadOnlyList<int> Draw(Func<int, int> next)
    {
        var copy = Pool.ToList();
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var j = next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.Take(NumbersPerPuzzle).ToList();
    }
}
