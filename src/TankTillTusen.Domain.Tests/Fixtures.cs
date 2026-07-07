using Xunit;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Shared fixtures (named to match the spec's GWT cases) plus a Given/When/Then scaffold for
/// the decider-true GWTs. puzzle0 = [10,10] target 100; puzzle1 = [5,20] target 100; host =
/// martinId. Solution fixtures merge operand 0 with operand 1 (answerIndex 2 = the result).
/// </summary>
public static class Fixtures
{
    public static readonly Guid MartinId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NilsId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SvenId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid GameId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid JoinCode = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Player HostMartin = new(MartinId, "Martin", IsHost: true);
    public static readonly Player PlayerNils = new(NilsId, "Nils", IsHost: false);
    public static readonly Player PlayerSven = new(SvenId, "Sven", IsHost: false);

    // Sample solutions (the generator's proof), one step each, answer = the result operand.
    public static readonly Solution SampleSol0 = new([new Step(0, Operator.Mul, 1)], 2); // 10×10=100
    public static readonly Solution SampleSol1 = new([new Step(0, Operator.Mul, 1)], 2); // 5×20=100

    public static readonly Puzzle Puzzle0 = new([10, 10], 100, SampleSol0);
    public static readonly Puzzle Puzzle1 = new([5, 20], 100, SampleSol1);

    // Submission fixtures (on puzzle0 = [10,10], target 100).
    public static readonly Solution SolHit = new([new Step(0, Operator.Mul, 1)], 2);  // 10×10=100 (exact)
    public static readonly Solution SolMiss = new([new Step(0, Operator.Add, 1)], 2); // 10+10=20 (5+20=25 on puzzle1)
    public static readonly Solution SolBad = new([new Step(0, Operator.Sub, 1)], 2);  // 10−10=0, not > 0 -> invalid

    // Hybrid-scoring fixtures (on puzzle800 = [500, 300, 20], target 800; declared before the
    // puzzle so the sample-solution field initializer sees a non-null value).
    public static readonly Solution SolExact800 = new([new Step(0, Operator.Add, 1)], 3); // 500+300=800 (exact)
    public static readonly Solution Sol780 = new([new Step(0, Operator.Add, 1), new Step(3, Operator.Sub, 2)], 4); // 500+300−20=780 (Δ20)
    public static readonly Solution Sol520 = new([new Step(0, Operator.Add, 2)], 3); // 500+20=520 (Δ280)

    public static readonly Puzzle Puzzle800 = new([500, 300, 20], 800, SolExact800);

    /// <summary>A fixed clock so the 45s deadline is deterministic in tests.</summary>
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A round start within the 45s window (deadline = Now + 45s, still in the future).</summary>
    public static readonly DateTimeOffset StartedAt = Now;

    /// <summary>A round start whose deadline (startedAt + 45s) is BEFORE Now.</summary>
    public static readonly DateTimeOffset StartedAtExpired = Now.AddSeconds(-(Decider.CountdownSeconds + 1));

    /// <summary>
    /// Stub context: a fixed clock at Now, and GeneratePuzzles returns the two fixture puzzles so
    /// the OpenLobby GWT can assert the stamped set. NewGuid is real (minted values asserted only
    /// by presence).
    /// </summary>
    public static readonly TankContext Context = new(
        NewGuid: Guid.NewGuid,
        Now: () => Now,
        GeneratePuzzles: () => [Puzzle0, Puzzle1]
    );

    /// <summary>Build a PuzzleRound inline, mirroring the spec's flow-map fixtures.</summary>
    public static PuzzleRound Round(
        Puzzle puzzle,
        DateTimeOffset? startedAt = null,
        IReadOnlyDictionary<Guid, Solution>? solutions = null,
        Solution? sampleSolution = null,
        IReadOnlyDictionary<Guid, int>? reachedValues = null,
        IReadOnlyDictionary<Guid, int>? roundScores = null,
        bool scored = false) =>
        new()
        {
            Puzzle = puzzle,
            StartedAt = startedAt,
            Solutions = solutions ?? new Dictionary<Guid, Solution>(),
            SampleSolution = sampleSolution,
            ReachedValues = reachedValues ?? new Dictionary<Guid, int>(),
            RoundScores = roundScores ?? new Dictionary<Guid, int>(),
            Scored = scored
        };
}

/// <summary>Entry point for the decider GWT scaffold: Gwt.Given(state).When(command).</summary>
public static class Gwt
{
    public static GivenState Given(TankState state) => new(state);
    public static GivenState GivenInitial() => new(TankState.Initial);
}

public sealed record GivenState(TankState State)
{
    public Result<TankEvent[]> When(TankCommand command) =>
        Decider.Decide(State, command, Fixtures.Context);
}

/// <summary>
/// Result/union extractors. Union case checks MUST use the `is`-pattern against a CONCRETE case
/// type (the union's runtime type is the union, not the case — a generic `is T` would fall back
/// to isinst and never match). So these helpers take concrete types only.
/// </summary>
public static class ResultAssertions
{
    public static TankEvent[] Events(this Result<TankEvent[]> result)
    {
        if (result is Ok<TankEvent[]> ok)
            return ok.Value;
        Assert.Fail("expected Ok (events), got an error");
        return [];
    }

    public static Err Error(this Result<TankEvent[]> result)
    {
        if (result is Err err)
            return err;
        Assert.Fail("expected Err, got Ok (events)");
        return null!;
    }

    public static LobbyOpened Opened(this TankEvent[] events)
    {
        foreach (var e in events)
            if (e is LobbyOpened a)
                return a;
        Assert.Fail("no LobbyOpened event");
        return null!;
    }

    public static PlayerJoined Joined(this TankEvent[] events)
    {
        foreach (var e in events)
            if (e is PlayerJoined a)
                return a;
        Assert.Fail("no PlayerJoined event");
        return null!;
    }

    public static GameStarted Started(this TankEvent[] events)
    {
        foreach (var e in events)
            if (e is GameStarted a)
                return a;
        Assert.Fail("no GameStarted event");
        return null!;
    }

    public static SolutionSubmitted Submitted(this TankEvent[] events)
    {
        foreach (var e in events)
            if (e is SolutionSubmitted a)
                return a;
        Assert.Fail("no SolutionSubmitted event");
        return null!;
    }

    public static PuzzleRevealed Revealed(this TankEvent[] events)
    {
        foreach (var e in events)
            if (e is PuzzleRevealed a)
                return a;
        Assert.Fail("no PuzzleRevealed event");
        return null!;
    }

    public static RoundScored ScoredFor(this TankEvent[] events, Guid playerId)
    {
        foreach (var e in events)
            if (e is RoundScored s && s.PlayerId == playerId)
                return s;
        Assert.Fail($"no RoundScored for {playerId}");
        return null!;
    }

    public static NextPuzzleStarted NextPuzzle(this TankEvent[] events)
    {
        foreach (var e in events)
            if (e is NextPuzzleStarted a)
                return a;
        Assert.Fail("no NextPuzzleStarted event");
        return null!;
    }

    public static GameEnded Ended(this TankEvent[] events)
    {
        foreach (var e in events)
            if (e is GameEnded a)
                return a;
        Assert.Fail("no GameEnded event");
        return null!;
    }
}
