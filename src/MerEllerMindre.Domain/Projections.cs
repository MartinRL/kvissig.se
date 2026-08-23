namespace MerEllerMindre.Domain;

/// <summary>
/// Read-model views (spec `v:` elements). Each is a pure projection of the folded
/// GameState — a separate concern from the decision model, so view data is DERIVED,
/// never stored (consistent with GameState's derive-all members). Per-player ordering
/// follows GameState.Players order.
/// </summary>
public record RosterView(
    Guid GameId,
    Guid JoinCode,
    IReadOnlyList<Player> Players
);

public record QuestionCardView(
    Guid GameId,
    int QuestionIndex,
    int TotalQuestions,
    string QuestionText,
    string ItemA,
    string ItemB
);

public record GuessProgressView(
    Guid GameId,
    int QuestionIndex,
    IReadOnlyList<Guid> SubmittedPlayerIds,
    IReadOnlyList<Guid> PendingPlayerIds
);

/// <summary>
/// The mellansteg between the two stages: direction revealed + the -10 bonus dealt.
/// Per-player directionCorrect, bonus, and the running total SO FAR (includes the just-
/// awarded bonus, for drama).
/// </summary>
public record PlayerDirectionResult(
    Guid PlayerId,
    Direction GuessedDirection,
    bool DirectionCorrect,
    int BonusPoints,
    int TotalSoFar
);

public record DirectionRevealView(
    Guid GameId,
    int QuestionIndex,
    Direction CorrectDirection,
    IReadOnlyList<PlayerDirectionResult> PlayerDirections
);

public record RoundScoresView(
    Guid GameId,
    int QuestionIndex,
    Direction CorrectDirection,
    byte CorrectDifference,
    IReadOnlyList<PlayerScore> PlayerScores
);

public record ScoreboardView(
    Guid GameId,
    IReadOnlyList<ScoreboardEntry> FinalScoreboard,
    IReadOnlyList<Guid> WinnerIds
);

/// <summary>
/// One row of the Outstanding-directions todo list (stage-1 gear): which players still owe
/// a direction on a given question and whether the question is complete.
/// </summary>
public record OutstandingDirection(
    int QuestionIndex,
    IReadOnlyList<Guid> PendingPlayerIds,
    bool AllDirectionsIn
);

public record OutstandingDirectionsView(
    Guid GameId,
    IReadOnlyList<OutstandingDirection> Questions
);

/// <summary>
/// One row of the Outstanding-differences todo list (stage-2 gear): which players still owe
/// a difference on a given question, whether it is complete, and whether stage 1 closed.
/// </summary>
public record OutstandingDifference(
    int QuestionIndex,
    IReadOnlyList<Guid> PendingPlayerIds,
    bool AllDifferencesIn,
    bool DirectionRevealed
);

public record OutstandingDifferencesView(
    Guid GameId,
    IReadOnlyList<OutstandingDifference> Questions
);

public record GameProgressView(
    Guid GameId,
    int QuestionIndex,
    int TotalQuestions,
    int ScoredQuestionCount,
    bool HasNextQuestion
);

/// <summary>
/// Pure projections (GameState -> View). The Web shell folds the event stream via
/// Decider.Fold, then projects the view it needs to render.
/// </summary>
public static class Projections
{
    public static RosterView Roster(GameState state) =>
        new(state.GameId, state.JoinCode, state.Players);

    public static QuestionCardView QuestionCard(GameState state)
    {
        var card = state.Questions[state.CurrentQuestionIndex].Card;
        return new QuestionCardView(
            state.GameId,
            state.CurrentQuestionIndex,
            state.Questions.Count,
            card.QuestionText,
            card.ItemA,
            card.ItemB);
    }

    public static GuessProgressView GuessProgress(GameState state)
    {
        var i = state.CurrentQuestionIndex;
        // Pending is derived from the ACTIVE stage: directions while stage 1 is open
        // (before the reveal), differences after.
        var (submittedSet, pending) = state.DirectionRevealed(i)
            ? (state.Questions[i].Differences.Keys, state.PendingDifferencePlayerIds(i))
            : (state.Questions[i].Directions.Keys, state.PendingDirectionPlayerIds(i));
        var submitted = state.Players
            .Where(p => submittedSet.Contains(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();
        return new GuessProgressView(state.GameId, i, submitted, pending);
    }

    public static DirectionRevealView DirectionReveal(GameState state)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        var playerDirections = state.Players
            .Select(p => new PlayerDirectionResult(
                p.PlayerId,
                round.Directions[p.PlayerId],
                round.CorrectDirection == round.Directions[p.PlayerId],
                round.DirectionScores[p.PlayerId],
                state.TotalScore(p.PlayerId) + round.DirectionScores[p.PlayerId]))
            .ToList();
        return new DirectionRevealView(state.GameId, i, round.CorrectDirection!.Value, playerDirections);
    }

    public static RoundScoresView RoundScores(GameState state)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        var playerScores = state.Players
            .Select(p => new PlayerScore(p.PlayerId, round.RoundScores[p.PlayerId], RunningTotal(state, p.PlayerId, i)))
            .ToList();
        return new RoundScoresView(
            state.GameId,
            i,
            round.CorrectDirection!.Value,
            round.CorrectDifference!.Value,
            playerScores);
    }

    public static ScoreboardView Scoreboard(GameState state) =>
        new(state.GameId, state.FinalScoreboard, state.WinnerIds);

    public static OutstandingDirectionsView OutstandingDirections(GameState state)
    {
        var questions = state.Questions
            .Select((_, i) => new OutstandingDirection(i, state.PendingDirectionPlayerIds(i), state.AllDirectionsIn(i)))
            .ToList();
        return new OutstandingDirectionsView(state.GameId, questions);
    }

    public static OutstandingDifferencesView OutstandingDifferences(GameState state)
    {
        var questions = state.Questions
            .Select((_, i) => new OutstandingDifference(i, state.PendingDifferencePlayerIds(i), state.AllDifferencesIn(i), state.DirectionRevealed(i)))
            .ToList();
        return new OutstandingDifferencesView(state.GameId, questions);
    }

    public static GameProgressView GameProgress(GameState state) =>
        new(
            state.GameId,
            state.CurrentQuestionIndex,
            state.Questions.Count,
            state.Questions.Count(q => q.Scored),
            state.HasNextQuestion);

    /// <summary>Running total at round i: sum of a player's round scores over scored questions up to and including i.</summary>
    private static int RunningTotal(GameState state, Guid playerId, int upToIndex) =>
        state.Questions
            .Where((q, i) => q.Scored && i <= upToIndex)
            .Sum(q => q.RoundScores.TryGetValue(playerId, out var s) ? s : 0);
}
