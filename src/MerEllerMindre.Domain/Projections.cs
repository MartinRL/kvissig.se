namespace MerEllerMindre.Domain;

/// <summary>
/// Read-model views (spec `v:` elements). Each is a pure projection of the folded
/// GameState — a separate concern from the decision model, so view data is DERIVED,
/// never stored (consistent with GameState's derive-all members). Per-player ordering
/// follows GameState.Players order.
/// </summary>
public record GameLobbyView(
    Guid GameId,
    Guid JoinCode,
    IReadOnlyList<Player> Players
);

public record QuestionView(
    Guid GameId,
    int QuestionIndex,
    int TotalQuestions,
    string QuestionText,
    string ItemA,
    string ItemB
);

public record WaitingForOthersView(
    Guid GameId,
    int QuestionIndex,
    IReadOnlyList<Guid> SubmittedPlayerIds,
    IReadOnlyList<Guid> PendingPlayerIds
);

public record RoundResultsView(
    Guid GameId,
    int QuestionIndex,
    Direction CorrectDirection,
    byte CorrectDifference,
    IReadOnlyList<PlayerScore> PlayerScores
);

public record FinalStandingsView(
    Guid GameId,
    IReadOnlyList<ScoreboardEntry> FinalScoreboard,
    IReadOnlyList<Guid> WinnerIds
);

/// <summary>
/// One row of the Outstanding-guesses todo list: which players still owe a guess on a
/// given question and whether the question is complete.
/// </summary>
public record OutstandingQuestion(
    int QuestionIndex,
    IReadOnlyList<Guid> PendingPlayerIds,
    bool AllGuessesIn
);

public record OutstandingGuessesView(
    Guid GameId,
    IReadOnlyList<OutstandingQuestion> Questions
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
    public static GameLobbyView GameLobby(GameState state) =>
        new(state.GameId, state.JoinCode, state.Players);

    public static QuestionView Question(GameState state)
    {
        var card = state.Questions[state.CurrentQuestionIndex].Card;
        return new QuestionView(
            state.GameId,
            state.CurrentQuestionIndex,
            state.Questions.Count,
            card.QuestionText,
            card.ItemA,
            card.ItemB);
    }

    public static WaitingForOthersView WaitingForOthers(GameState state)
    {
        var i = state.CurrentQuestionIndex;
        var guesses = state.Questions[i].Guesses;
        var submitted = state.Players
            .Where(p => guesses.ContainsKey(p.PlayerId))
            .Select(p => p.PlayerId)
            .ToList();
        return new WaitingForOthersView(state.GameId, i, submitted, state.PendingPlayerIds(i));
    }

    public static RoundResultsView RoundResults(GameState state)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        var playerScores = state.Players
            .Select(p => new PlayerScore(p.PlayerId, round.RoundScores[p.PlayerId], RunningTotal(state, p.PlayerId, i)))
            .ToList();
        return new RoundResultsView(
            state.GameId,
            i,
            round.CorrectDirection!.Value,
            round.CorrectDifference!.Value,
            playerScores);
    }

    public static FinalStandingsView FinalStandings(GameState state) =>
        new(state.GameId, state.FinalScoreboard, state.WinnerIds);

    public static OutstandingGuessesView OutstandingGuesses(GameState state)
    {
        var questions = state.Questions
            .Select((_, i) => new OutstandingQuestion(i, state.PendingPlayerIds(i), state.AllGuessesIn(i)))
            .ToList();
        return new OutstandingGuessesView(state.GameId, questions);
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
