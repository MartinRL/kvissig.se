namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Shared fixtures, named exactly as the spec's `tests:` reference them (the generated
/// SpecTests resolve bare words to Fixtures.* — a missing name is a CS0117). puzzle0 =
/// [10,10] target 100; puzzle1 = [5,20] target 100; host = martinId. Solution fixtures
/// merge operand 0 with operand 1 (answerIndex 2 = the result).
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

    /// <summary>A fixed clock so the 60s deadline is deterministic in tests.</summary>
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A round start within the 60s window (deadline = Now + 60s, still in the future).</summary>
    public static readonly DateTimeOffset StartedAt = Now;

    /// <summary>A round start whose deadline + grace is BEFORE Now (fully expired).</summary>
    public static readonly DateTimeOffset StartedAtExpired = Now.AddSeconds(-(Decider.CountdownSeconds + Decider.GraceSeconds + 1));

    /// <summary>A round start whose deadline passed 1s ago — still INSIDE the grace window.</summary>
    public static readonly DateTimeOffset StartedAtJustExpired = Now.AddSeconds(-(Decider.CountdownSeconds + 1));

    // Timestamps the decider stamps from the fixed clock — all Now under Context.
    public static readonly DateTimeOffset OpenedAt = Now;
    public static readonly DateTimeOffset JoinedAt = Now;
    public static readonly DateTimeOffset SubmittedAt = Now;
    public static readonly DateTimeOffset EndedAt = Now;

    /// <summary>Round 0's deadline once started at StartedAt (= what the projections show).</summary>
    public static readonly DateTimeOffset Deadline = StartedAt.AddSeconds(Decider.CountdownSeconds);

    /// <summary>A join code / game id that resolves to nothing.</summary>
    public static readonly Guid Unknown = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

    // PuzzleRound fixtures, named as the spec's `tests:` reference them (round0Fresh, …).
    public static readonly PuzzleRound Round0Fresh = Round(Puzzle0, startedAt: StartedAt);
    public static readonly PuzzleRound Round0NilsMiss = Round(Puzzle0, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [NilsId] = SolMiss });
    public static readonly PuzzleRound Round0JustExpired = Round(Puzzle0, startedAt: StartedAtJustExpired);
    public static readonly PuzzleRound Round0Expired = Round(Puzzle0, startedAt: StartedAtExpired);
    public static readonly PuzzleRound Round0BothIn = Round(Puzzle0, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = SolMiss, [NilsId] = SolHit });
    public static readonly PuzzleRound Round0Scored = Round(Puzzle0, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = SolMiss, [NilsId] = SolHit },
        sampleSolution: SampleSol0,
        reachedValues: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 100 },
        roundScores: new Dictionary<Guid, int> { [MartinId] = 80, [NilsId] = -10 },
        scored: true);
    public static readonly PuzzleRound Round0ScoredUnstarted = Round(Puzzle0,
        sampleSolution: SampleSol0,
        reachedValues: new Dictionary<Guid, int> { [MartinId] = 20, [NilsId] = 100 },
        roundScores: new Dictionary<Guid, int> { [MartinId] = 80, [NilsId] = -10 },
        scored: true);
    public static readonly PuzzleRound Round0MartinHitOnly = Round(Puzzle0, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = SolHit });
    public static readonly PuzzleRound Round0MartinHitExpired = Round(Puzzle0, startedAt: StartedAtExpired,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = SolHit });
    public static readonly PuzzleRound Round0MartinMiss = Round(Puzzle0, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = SolMiss });
    public static readonly PuzzleRound Round0Scores0And80 = Round(Puzzle0,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 80 }, scored: true);
    public static readonly PuzzleRound Round0Scores10Each = Round(Puzzle0,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 10, [NilsId] = 10 }, scored: true);
    public static readonly PuzzleRound Round1Fresh = Round(Puzzle1);
    public static readonly PuzzleRound Round1BothIn = Round(Puzzle1, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = SolHit, [NilsId] = SolMiss });
    public static readonly PuzzleRound Round1Scores5And0 = Round(Puzzle1,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 0 }, scored: true);
    public static readonly PuzzleRound Round1Scores5Each = Round(Puzzle1,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 5 }, scored: true);
    public static readonly PuzzleRound Round800Close = Round(Puzzle800, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = Sol780, [NilsId] = SolExact800 });
    public static readonly PuzzleRound Round800Wild = Round(Puzzle800, startedAt: StartedAt,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = Sol780, [NilsId] = Sol520 });
    public static readonly PuzzleRound Round800MartinOnly = Round(Puzzle800, startedAt: StartedAtExpired,
        solutions: new Dictionary<Guid, Solution> { [MartinId] = Sol780 });

    // View-row fixtures (Outstanding solutions / Round results / scoreboards).
    public static readonly OutstandingSolution Os0AllPending = new(0, [MartinId, NilsId], false, Deadline);
    public static readonly OutstandingSolution Os0NilsPending = new(0, [NilsId], false, Deadline);
    public static readonly OutstandingSolution Os0AllIn = new(0, [], true, Deadline);
    public static readonly OutstandingSolution Os1AllPending = new(1, [MartinId, NilsId], false, null);

    public static readonly PlayerResult PrMartin = new(MartinId, 20, 80, 80);
    public static readonly PlayerResult PrNils = new(NilsId, 100, -10, -10);

    public static readonly ScoreboardEntry SbMartin5 = new(MartinId, "Martin", 5);
    public static readonly ScoreboardEntry SbNils80 = new(NilsId, "Nils", 80);
    public static readonly ScoreboardEntry SbMartin15 = new(MartinId, "Martin", 15);
    public static readonly ScoreboardEntry SbNils15 = new(NilsId, "Nils", 15);

    /// <summary>
    /// Stub context: a fixed clock at Now, and GeneratePuzzles returns the two fixture puzzles so
    /// the OpenLobby GWT can assert the stamped set. NewGuid is real (minted values asserted only
    /// by presence).
    /// </summary>
    public static readonly TankContext Context = new(
        NewGuid: Guid.NewGuid,
        Now: () => Now,
        GeneratePuzzles: _ => [Puzzle0, Puzzle1]
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
