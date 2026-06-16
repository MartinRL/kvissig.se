using Xunit;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Shared fixtures (named to match the spec's GWT cases) plus a bespoke Given/When/Then
/// scaffold for the decider-true GWTs. No third-party GWT/approval dependency.
/// </summary>
public static class Fixtures
{
    // Fixed ids so tests read like the spec (martinId, nilsId, …).
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
        new("mer-eller-mindre", "Mer eller mindre", [Question0, Question1]);

    public static readonly Guess GuessMer30 = new(Direction.Mer, 30m);
    public static readonly Guess GuessMer40 = new(Direction.Mer, 40m);
    public static readonly Guess GuessMindre25 = new(Direction.Mindre, 25m);
    public static readonly Guess GuessMindre50 = new(Direction.Mindre, 50m);
    public static readonly Guess GuessMindre20 = new(Direction.Mindre, 20m);

    /// <summary>
    /// Stub context: FindPack resolves the 2-card fixture pack for "mer-eller-mindre",
    /// else null. NewGuid/Now are real (minted values are asserted only by presence).
    /// </summary>
    public static readonly GameContext Context = new(
        NewGuid: Guid.NewGuid,
        Now: () => DateTimeOffset.UtcNow,
        FindPack: slug => slug == "mer-eller-mindre" ? Pack : null,
        NextRandom: Random.Shared.Next
    );
}

/// <summary>Entry point for the decider GWT scaffold: Gwt.Given(state).When(command).</summary>
public static class Gwt
{
    public static GivenState Given(GameState state) => new(state);
    public static GivenState GivenInitial() => new(GameState.Initial);
}

public sealed record GivenState(GameState State)
{
    public Result<GameEvent[]> When(GameCommand command) =>
        Decider.Decide(State, command, Fixtures.Context);
}

/// <summary>
/// Result/union extractors. Union case checks MUST use the `is`-pattern against a CONCRETE
/// case type (the union's runtime type is the union, not the case — a generic `is T` would
/// fall back to isinst and never match). So these helpers take concrete types only.
/// </summary>
public static class ResultAssertions
{
    public static GameEvent[] Events(this Result<GameEvent[]> result)
    {
        if (result is Ok<GameEvent[]> ok)
            return ok.Value;
        Assert.Fail("expected Ok (events), got an error");
        return [];
    }

    public static Err Error(this Result<GameEvent[]> result)
    {
        if (result is Err err)
            return err;
        Assert.Fail("expected Err, got Ok (events)");
        return null!;
    }

    public static QuestionAnswered Answered(this GameEvent[] events)
    {
        foreach (var e in events)
            if (e is QuestionAnswered a)
                return a;
        Assert.Fail("no QuestionAnswered event");
        return null!;
    }

    public static QuestionScored ScoredFor(this GameEvent[] events, Guid playerId)
    {
        foreach (var e in events)
            if (e is QuestionScored s && s.PlayerId == playerId)
                return s;
        Assert.Fail($"no QuestionScored for {playerId}");
        return null!;
    }

    public static GameEnded Ended(this GameEvent[] events)
    {
        foreach (var e in events)
            if (e is GameEnded g)
                return g;
        Assert.Fail("no GameEnded event");
        return null!;
    }
}
