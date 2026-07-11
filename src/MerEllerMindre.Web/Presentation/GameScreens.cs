using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Presentation;

/// <summary>Which screen a viewer sees, derived purely from game state + who is looking.</summary>
public enum ScreenKind
{
    LobbyHost,
    LobbyPlayer,
    Question,
    Waiting,
    DirectionResults,
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
        var round = state.Questions[i];
        if (round.Scored)
            return ScreenKind.Results;

        if (!state.DirectionRevealed(i))
        {
            // Stage 1: pick a direction, then wait for the rest.
            if (viewer is { } id && round.Directions.ContainsKey(id))
                return ScreenKind.Waiting;
            return ScreenKind.Question;
        }

        // Stage 2: direction revealed. Once a player has sized the difference they wait;
        // otherwise they sit on the mellansteg until they tap through to the slider.
        if (viewer is { } pid && round.Differences.ContainsKey(pid))
            return ScreenKind.Waiting;
        return ScreenKind.DirectionResults;
    }

    public static LobbyVm Lobby(GameState state, Guid? viewer, string token, string joinUrl, bool showJoinUrl)
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
            ShowJoinUrl: showJoinUrl,
            token);
    }

    public static QuestionVm Question(GameState state, string token, QuestionStage stage, Func<string, string?>? resolveLogo = null)
    {
        var i = state.CurrentQuestionIndex;
        var card = state.Questions[i].Card;
        var sliderMax = Math.Max(card.ValueA, card.ValueB);
        var logo = LogoResolver(state, resolveLogo);
        return new QuestionVm(
            state.JoinCode,
            QuestionNumber: i + 1,
            TotalQuestions: state.Questions.Count,
            Heading(state, card.QuestionText),
            card.ItemA,
            card.ItemB,
            sliderMax,
            SliderStep(sliderMax),
            stage,
            RevealedDirection: stage == QuestionStage.Difference ? state.Questions[i].CorrectDirection : null,
            token,
            LogoA: logo(card.ItemA),
            LogoB: logo(card.ItemB));
    }

    public static DirectionResultsVm DirectionResults(GameState state, Guid? viewer, Func<string, string?>? resolveLogo = null)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        var card = round.Card;
        var correct = round.CorrectDirection!.Value;
        var logo = LogoResolver(state, resolveLogo);
        var merItem = correct == Direction.Mer ? card.ItemA : card.ItemB;
        var mindreItem = correct == Direction.Mer ? card.ItemB : card.ItemA;

        var rows = state.Players
            .Select(p =>
            {
                var guessed = round.Directions[p.PlayerId];
                var bonus = round.DirectionScores[p.PlayerId];
                return new DirectionResultRowVm(
                    p.Name,
                    p.PlayerId == viewer,
                    p.PlayerId == state.HostPlayerId,
                    guessed,
                    guessed == correct,
                    bonus,
                    state.TotalScore(p.PlayerId) + bonus);
            })
            .ToList();

        return new DirectionResultsVm(
            state.JoinCode,
            QuestionNumber: i + 1,
            TotalQuestions: state.Questions.Count,
            Heading(state, card.QuestionText),
            MerItem: merItem,
            MindreItem: mindreItem,
            correct,
            rows,
            // No token needed: the only action is a GET to the slider screen.
            AntiforgeryToken: "",
            MerLogo: logo(merItem),
            MindreLogo: logo(mindreItem));
    }

    public static WaitingVm Waiting(GameState state, Guid? viewer)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        // Pending follows the active stage: directions before the reveal, differences after.
        var submitted = state.DirectionRevealed(i)
            ? (IReadOnlySet<Guid>)round.Differences.Keys.ToHashSet()
            : round.Directions.Keys.ToHashSet();
        var done = state.Players
            .Where(p => submitted.Contains(p.PlayerId))
            .Select(p => new WaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        var pending = state.Players
            .Where(p => !submitted.Contains(p.PlayerId))
            .Select(p => new WaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        return new WaitingVm(state.JoinCode, i + 1, state.Questions.Count, done.Count, state.Players.Count, done, pending);
    }

    public static ResultsVm Results(GameState state, Guid? viewer, string token, Func<string, string?>? resolveLogo = null)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        var card = round.Card;
        var aLarger = card.ValueA >= card.ValueB;
        var logo = LogoResolver(state, resolveLogo);
        var largerItem = aLarger ? card.ItemA : card.ItemB;
        var smallerItem = aLarger ? card.ItemB : card.ItemA;
        var largerValue = Math.Max(card.ValueA, card.ValueB);
        var smallerValue = Math.Min(card.ValueA, card.ValueB);
        var smallerPercent = largerValue == 0 ? 0 : (int)Math.Round(smallerValue / largerValue * 100, MidpointRounding.AwayFromZero);

        var rows = state.Players
            .Select(p =>
                new ResultRowVm(
                    Rank: 0,
                    p.Name,
                    p.PlayerId == viewer,
                    p.PlayerId == state.HostPlayerId,
                    round.Directions[p.PlayerId],
                    Decider.NormalizeDifference(round.Differences[p.PlayerId], largerValue),
                    round.RoundScores[p.PlayerId],
                    state.TotalScore(p.PlayerId)))
            .OrderBy(r => r.TotalScore)
            .Select((r, idx) => r with { Rank = idx + 1 })
            .ToList();

        return new ResultsVm(
            state.JoinCode,
            QuestionNumber: i + 1,
            TotalQuestions: state.Questions.Count,
            Heading(state, card.QuestionText),
            LargerItem: largerItem,
            SmallerItem: smallerItem,
            largerValue,
            smallerValue,
            card.Unit,
            round.CorrectDirection!.Value,
            round.CorrectDifference!.Value,
            smallerPercent,
            rows,
            ViewerIsHost: viewer == state.HostPlayerId,
            state.HasNextQuestion,
            token,
            LargerLogo: logo(largerItem),
            SmallerLogo: logo(smallerItem),
            Source: card.Source);
    }

    /// <summary>
    /// Logo lookup, but only for loggor-* packs (slug-prefix convention) — text packs always
    /// resolve to null so they render names. GameScreens stays IO-free: the caller injects the
    /// catalog lookup as a func.
    /// </summary>
    private static Func<string, string?> LogoResolver(GameState state, Func<string, string?>? resolveLogo) =>
        state.QuestionPackId.StartsWith("loggor-", StringComparison.Ordinal)
            ? resolveLogo ?? (_ => null)
            : _ => null;

    /// <summary>
    /// The game is "loggor" but the cards are authored with "märke". On both screens call it
    /// "företag". Only loggor-* packs are relabelled; text packs keep their authored question.
    /// ponytail: string-replace over the fixed loggor stems, not a grammar engine. "företag" is
    /// ett-gender like "märke" so Vilket/värt stay put.
    /// </summary>
    private static string Heading(GameState state, string fråga) =>
        !state.QuestionPackId.StartsWith("loggor-", StringComparison.Ordinal)
            ? fråga
            : fråga.Replace("av märkena", "av företagen")
                   .Replace("Vilket märke", "Vilket företag");

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
