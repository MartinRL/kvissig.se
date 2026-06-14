using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Presentation;

/// <summary>Which screen a viewer sees, derived purely from game state + who is looking.</summary>
public enum ScreenKind
{
    LobbyHost,
    LobbyPlayer,
    Question,
    Waiting,
    Results,
    Standings
}

/// <summary>
/// Pure mapping from <see cref="GameState"/> (+ the viewing player) to the screen to render and
/// its view-model. Keeps all the "which screen / you-perspective / slider bounds / rank" logic
/// in one tested place so the Razor components stay dumb.
/// </summary>
public static class GameScreens
{
    public static ScreenKind Select(GameState state, Guid? viewer) =>
        state.Phase switch
        {
            GamePhase.Ended => ScreenKind.Standings,
            GamePhase.Lobby => viewer == state.HostPlayerId ? ScreenKind.LobbyHost : ScreenKind.LobbyPlayer,
            GamePhase.Started => SelectStarted(state, viewer),
            // NotCreated never reaches here (the endpoint guards GameNotFound first).
            _ => ScreenKind.LobbyPlayer
        };

    private static ScreenKind SelectStarted(GameState state, Guid? viewer)
    {
        var i = state.CurrentQuestionIndex;
        if (state.Questions[i].Scored)
            return ScreenKind.Results;
        if (viewer is { } id && state.Questions[i].Guesses.ContainsKey(id))
            return ScreenKind.Waiting;
        return ScreenKind.Question;
    }

    public static LobbyVm Lobby(GameState state, Guid? viewer, string token, string joinUrl)
    {
        var players = state.Players
            .Select(p => new LobbyPlayerVm(p.Name, p.IsHost, p.PlayerId == viewer))
            .ToList();
        return new LobbyVm(
            state.JoinCode,
            state.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "",
            joinUrl,
            QrCode.SvgFor(joinUrl),
            players,
            ViewerIsHost: viewer == state.HostPlayerId,
            CanStart: state.Players.Count >= 2,
            token);
    }

    public static QuestionVm Question(GameState state, string token)
    {
        var i = state.CurrentQuestionIndex;
        var card = state.Questions[i].Card;
        var sliderMax = Math.Max(card.ValueA, card.ValueB);
        return new QuestionVm(
            state.JoinCode,
            QuestionNumber: i + 1,
            TotalQuestions: state.Questions.Count,
            card.QuestionText,
            card.ItemA,
            card.ItemB,
            card.DifferencePrompt,
            card.Unit,
            sliderMax,
            SliderStep(sliderMax),
            token);
    }

    public static WaitingVm Waiting(GameState state, Guid? viewer)
    {
        var i = state.CurrentQuestionIndex;
        var guesses = state.Questions[i].Guesses;
        var done = state.Players
            .Where(p => guesses.ContainsKey(p.PlayerId))
            .Select(p => new WaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        var pending = state.Players
            .Where(p => !guesses.ContainsKey(p.PlayerId))
            .Select(p => new WaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        return new WaitingVm(state.JoinCode, i + 1, state.Questions.Count, done.Count, state.Players.Count, done, pending);
    }

    public static ResultsVm Results(GameState state, Guid? viewer, string token)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        var card = round.Card;
        var aLarger = card.ValueA >= card.ValueB;
        var largerValue = Math.Max(card.ValueA, card.ValueB);
        var smallerValue = Math.Min(card.ValueA, card.ValueB);
        var smallerPercent = largerValue == 0 ? 0 : (int)Math.Round(smallerValue / largerValue * 100, MidpointRounding.AwayFromZero);

        var rows = state.Players
            .Select(p =>
            {
                var guess = round.Guesses[p.PlayerId];
                return new ResultRowVm(
                    Rank: 0,
                    p.Name,
                    p.PlayerId == viewer,
                    p.PlayerId == state.HostPlayerId,
                    guess.Direction,
                    Decider.NormalizeDifference(guess.GuessedDifference, largerValue),
                    round.RoundScores[p.PlayerId],
                    state.TotalScore(p.PlayerId));
            })
            .OrderBy(r => r.TotalScore)
            .Select((r, idx) => r with { Rank = idx + 1 })
            .ToList();

        return new ResultsVm(
            state.JoinCode,
            QuestionNumber: i + 1,
            TotalQuestions: state.Questions.Count,
            card.QuestionText,
            LargerItem: aLarger ? card.ItemA : card.ItemB,
            SmallerItem: aLarger ? card.ItemB : card.ItemA,
            largerValue,
            smallerValue,
            card.Unit,
            round.CorrectDirection!.Value,
            round.CorrectDifference!.Value,
            smallerPercent,
            rows,
            ViewerIsHost: viewer == state.HostPlayerId,
            state.HasNextQuestion,
            token);
    }

    public static StandingsVm Standings(GameState state, Guid? viewer)
    {
        var winners = state.WinnerIds.ToHashSet();
        var names = state.Players.ToDictionary(p => p.PlayerId, p => p.Name);
        var rows = state.FinalScoreboard
            .OrderBy(e => e.TotalScore)
            .Select((e, idx) => new StandingRowVm(idx + 1, e.PlayerName, e.PlayerId == viewer, e.PlayerId == state.HostPlayerId, e.TotalScore, winners.Contains(e.PlayerId)))
            .ToList();
        var winnerNames = state.WinnerIds
            .Select(id => names.TryGetValue(id, out var n) ? n : "")
            .ToList();
        return new StandingsVm(state.JoinCode, rows, winnerNames);
    }

    /// <summary>
    /// Slider step in the card's own unit: coarse for big magnitudes (km², kr), 1 for medium
    /// (mil, °C), fine for small (miljoner). Simplicity over a per-card range column.
    /// </summary>
    public static decimal SliderStep(decimal max) =>
        max >= 200 ? Math.Round(max / 100)
        : max >= 20 ? 1
        : 0.1m;
}
