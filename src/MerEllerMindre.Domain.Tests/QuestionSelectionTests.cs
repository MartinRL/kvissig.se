using AwesomeAssertions;
using Xunit;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Tests for the pure balanced-selection algorithm. RNG is a deterministic stub
/// (next => 0) so the band histogram (shuffle-invariant) is asserted exactly.
/// </summary>
public class QuestionSelectionTests
{
    // mx is always 100 here, so norm == |A - B|. Pick A=100, B=100-norm to land a band.
    // Items are unique per id so the item-distinct guard doesn't collapse band quotas.
    private static Question Card(int norm, int id) =>
        new($"q{id}", $"A{id}", $"B{id}", 100m, 100m - norm, "u", "d");

    private static int Band(Question q)
    {
        var norm = Decider.NormalizeDifference(Math.Abs(q.ValueA - q.ValueB), Math.Max(q.ValueA, q.ValueB));
        return norm switch { <= 20 => 0, <= 60 => 1, <= 85 => 2, _ => 3 };
    }

    [Fact]
    public void SmallPoolIsReturnedAsIs()
    {
        var pool = new List<Question> { Card(10, 0), Card(40, 1), Card(70, 2) };

        var selected = QuestionSelection.PickBalanced(pool, Decider.FullGameSize, _ => 0);

        selected.Should().BeSameAs(pool);
    }

    [Fact]
    public void FullPoolHitsTheBandQuota()
    {
        var pool = new List<Question>();
        var id = 0;
        foreach (var norm in new[] { 10, 40, 70, 95 }) // one norm per band
            for (var k = 0; k < 25; k++)
                pool.Add(Card(norm, id++));

        var selected = QuestionSelection.PickBalanced(pool, 21, _ => 0);

        selected.Should().HaveCount(21);
        selected.Distinct().Should().HaveCount(21);

        var hist = selected.GroupBy(Band).ToDictionary(g => g.Key, g => g.Count());
        hist[0].Should().Be(3);
        hist[1].Should().Be(9);
        hist[2].Should().Be(6);
        hist[3].Should().Be(3);
    }

    [Fact]
    public void UnderfullBandDeficitIsFilledFromLeftover()
    {
        var pool = new List<Question>();
        var id = 0;
        pool.Add(Card(10, id++)); // band 0: only 1 card, quota is 3 -> deficit 2
        for (var k = 0; k < 50; k++) pool.Add(Card(40, id++)); // band 1
        for (var k = 0; k < 50; k++) pool.Add(Card(70, id++)); // band 2
        for (var k = 0; k < 50; k++) pool.Add(Card(95, id++)); // band 3

        var selected = QuestionSelection.PickBalanced(pool, 21, _ => 0);

        selected.Should().HaveCount(21);
        selected.Distinct().Should().HaveCount(21);
        selected.Count(q => Band(q) == 0).Should().Be(1); // band 0 exhausted, no overdraw
    }

    [Fact]
    public void OverRepresentedItemAppearsAtMostOnce()
    {
        var pool = new List<Question>();
        var id = 0;
        // Plenty of item-distinct cards across bands...
        foreach (var norm in new[] { 10, 40, 70, 95 })
            for (var k = 0; k < 25; k++)
                pool.Add(Card(norm, id++));
        // ...plus many cards all sharing the item "Globen" (the bug: it landed 3x/game).
        for (var k = 0; k < 30; k++)
            pool.Add(new($"globen{k}", "Globen", $"Other{k}", 100m, 40m, "u", "d"));

        var selected = QuestionSelection.PickBalanced(pool, 21, _ => 0);

        selected.Should().HaveCount(21);
        selected.SelectMany(q => new[] { q.ItemA, q.ItemB })
            .GroupBy(x => x)
            .All(g => g.Count() == 1).Should().BeTrue();
    }

    [Fact]
    public void NoTopicDominatesTheRound()
    {
        // 3 topics, plenty of item-distinct cards each; mini round of 7 -> cap = ceil(7/3) = 3.
        var pool = new List<Question>();
        var id = 0;
        foreach (var topic in new[] { "börsvärde", "ålder", "anställda" })
            for (var k = 0; k < 20; k++)
                pool.Add(new(topic, $"{topic}A{id}", $"{topic}B{id++}", 100m, 60m, "u", "d"));

        var selected = QuestionSelection.PickBalanced(pool, 7, _ => 0);

        selected.Should().HaveCount(7);
        selected.GroupBy(q => q.QuestionText)
            .All(g => g.Count() <= 3).Should().BeTrue();
    }
}
