namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Shared fixtures, named exactly as the spec's `tests:` reference them (the generated
/// SpecTests resolve bare words to Fixtures.* — a missing name is a CS0117).
/// question0 reveals mer/40, question1 mindre/20 (both mx=100); host = martinId.
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

    // question0 -> mer, 40 (mx=100); question1 -> mindre, 20 (mx=100). Both mx=100 so a
    // raw guessedDifference normalizes to itself.
    public static readonly Question Question0 =
        new("question0", "question0A", "question0B", 100m, 60m, "question0U", "question0D");

    public static readonly Question Question1 =
        new("question1", "question1A", "question1B", 80m, 100m, "question1U", "question1D");

    public static readonly QuestionPack Pack =
        new("mer-eller-mindre", "Mer eller Mindre", [Question0, Question1]);

    /// <summary>A fixed clock so timestamp pins are deterministic in tests.</summary>
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    // Timestamps the decider stamps from the fixed clock — all Now under Context.
    public static readonly DateTimeOffset CreatedAt = Now;
    public static readonly DateTimeOffset JoinedAt = Now;
    public static readonly DateTimeOffset StartedAt = Now;
    public static readonly DateTimeOffset SubmittedAt = Now;
    public static readonly DateTimeOffset EndedAt = Now;

    /// <summary>A join code / game id that resolves to nothing.</summary>
    public static readonly Guid Unknown = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

    // QuestionRound fixtures, named as the spec's `tests:` reference them (q0Fresh, …).
    public static readonly QuestionRound Q0Fresh = Round(Question0);
    public static readonly QuestionRound Q1Fresh = Round(Question1);
    public static readonly QuestionRound Q0NilsMer = Round(Question0,
        directions: new Dictionary<Guid, Direction> { [NilsId] = Direction.Mer });
    public static readonly QuestionRound Q0MartinDirectionOnly = Round(Question0,
        directions: new Dictionary<Guid, Direction> { [MartinId] = Direction.Mer });
    public static readonly QuestionRound Q0BothDirectionsIn = Round(Question0,
        directions: new Dictionary<Guid, Direction> { [MartinId] = Direction.Mer, [NilsId] = Direction.Mindre });
    public static readonly QuestionRound Q0DirectionRevealed = Q0BothDirectionsIn with
    {
        CorrectDirection = Direction.Mer,
        DirectionScores = new Dictionary<Guid, int> { [MartinId] = -10, [NilsId] = 0 }
    };
    public static readonly QuestionRound Q0NilsSized = Q0DirectionRevealed with
    {
        Differences = new Dictionary<Guid, decimal> { [NilsId] = 50m }
    };
    public static readonly QuestionRound Q0MartinSizedOnly = Q0DirectionRevealed with
    {
        Differences = new Dictionary<Guid, decimal> { [MartinId] = 30m }
    };
    public static readonly QuestionRound Q0BothSized = Q0DirectionRevealed with
    {
        Differences = new Dictionary<Guid, decimal> { [MartinId] = 30m, [NilsId] = 50m }
    };
    public static readonly QuestionRound Q0MartinExact = Q0DirectionRevealed with
    {
        Differences = new Dictionary<Guid, decimal> { [MartinId] = 40m, [NilsId] = 50m }
    };
    /// <summary>Question0 fully scored (both reveals + round scores). Carries its bids-in
    /// history so the AllDifferencesIn guard passes before the already-scored guard fires.</summary>
    public static readonly QuestionRound Q0Scored = Q0BothSized with
    {
        CorrectDifference = 40,
        RoundScores = new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 },
        Scored = true
    };
    public static readonly QuestionRound Q1BothSized = Round(Question1,
        directions: new Dictionary<Guid, Direction> { [MartinId] = Direction.Mindre, [NilsId] = Direction.Mindre },
        correctDirection: Direction.Mindre,
        directionScores: new Dictionary<Guid, int> { [MartinId] = -10, [NilsId] = -10 },
        differences: new Dictionary<Guid, decimal> { [MartinId] = 25m, [NilsId] = 20m });
    public static readonly QuestionRound Q0Scores0And10 = Round(Question0,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 0, [NilsId] = 10 }, scored: true);
    public static readonly QuestionRound Q1Scores5And0 = Round(Question1,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 0 }, scored: true);
    public static readonly QuestionRound Q0Scores5Each = Round(Question0,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 5 }, scored: true);
    public static readonly QuestionRound Q1Scores5Each = Round(Question1,
        roundScores: new Dictionary<Guid, int> { [MartinId] = 5, [NilsId] = 5 }, scored: true);

    // View-row fixtures (Outstanding directions/differences, results, scoreboards).
    public static readonly OutstandingDirection Od0AllPending = new(0, [MartinId, NilsId], false);
    public static readonly OutstandingDirection Od1AllPending = new(1, [MartinId, NilsId], false);
    public static readonly OutstandingDirection Od0NilsPending = new(0, [NilsId], false);
    public static readonly OutstandingDirection Od0AllIn = new(0, [], true);

    public static readonly OutstandingDifference Of0NilsPending = new(0, [NilsId], false, DirectionRevealed: true);
    public static readonly OutstandingDifference Of1AllPending = new(1, [MartinId, NilsId], false, DirectionRevealed: false);
    public static readonly OutstandingDifference Of0AllIn = new(0, [], true, DirectionRevealed: true);

    public static readonly PlayerDirectionResult PdMartin =
        new(MartinId, Direction.Mer, DirectionCorrect: true, BonusPoints: -10, TotalSoFar: -10);
    public static readonly PlayerDirectionResult PdNils =
        new(NilsId, Direction.Mindre, DirectionCorrect: false, BonusPoints: 0, TotalSoFar: 0);

    public static readonly PlayerScore PsMartin0 = new(MartinId, 0, 0);
    public static readonly PlayerScore PsNils10 = new(NilsId, 10, 10);

    public static readonly ScoreboardEntry SbMartin5 = new(MartinId, "Martin", 5);
    public static readonly ScoreboardEntry SbMartin10 = new(MartinId, "Martin", 10);
    public static readonly ScoreboardEntry SbNils10 = new(NilsId, "Nils", 10);

    /// <summary>
    /// Stub context: a fixed clock at Now; FindPack resolves the 2-card fixture pack for
    /// "mer-eller-mindre", else null. NewGuid is real (minted values asserted only by
    /// presence). NextRandom is never exercised (2-card pool &lt;= game size uses the whole pack).
    /// </summary>
    public static readonly GameContext Context = new(
        NewGuid: Guid.NewGuid,
        Now: () => Now,
        FindPack: slug => slug == "mer-eller-mindre" ? Pack : null,
        NextRandom: Random.Shared.Next
    );

    /// <summary>Build a QuestionRound inline, mirroring the spec's flow-map fixtures.</summary>
    public static QuestionRound Round(
        Question card,
        IReadOnlyDictionary<Guid, Direction>? directions = null,
        Direction? correctDirection = null,
        IReadOnlyDictionary<Guid, int>? directionScores = null,
        IReadOnlyDictionary<Guid, decimal>? differences = null,
        byte? correctDifference = null,
        IReadOnlyDictionary<Guid, int>? roundScores = null,
        bool scored = false) =>
        new()
        {
            Card = card,
            Directions = directions ?? new Dictionary<Guid, Direction>(),
            CorrectDirection = correctDirection,
            DirectionScores = directionScores ?? new Dictionary<Guid, int>(),
            Differences = differences ?? new Dictionary<Guid, decimal>(),
            CorrectDifference = correctDifference,
            RoundScores = roundScores ?? new Dictionary<Guid, int>(),
            Scored = scored
        };
}
