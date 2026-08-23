using MerEllerMindre.Domain;
using MerEllerMindre.Web.Xm;
using Xmlang;
// The how-it-works Step (Xm) name is unambiguous here, but keep the sister-file alias style.
using XmStep = MerEllerMindre.Web.Xm.Step;

namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// The Mer eller Mindre residue for the xm runtime renderer: the fine screen selector
/// (tvåstegsraket stage split, xm finding 4 — inexpressible in during×for), the per-surface
/// materializers reducing GameState + xm labels to the closed Field vocabulary, and the
/// command bindings (AskNextQuestion/EndGame mutual exclusion on hasNextQuestion, finding 3).
/// Riktningsfråga + Skillnadsfråga OPT OUT to the hand-written picker/slider idiom
/// (QuestionScreen, finding 5) and Riktningsavslöjande to the mellansteg's GET tap-through
/// (DirectionResultsScreen, finding 8) — this class still builds their view-models.
/// Sister to AuctionSurfaces/TankSurfaces. LOWEST total wins.
/// </summary>
public static class GameSurfaces
{
    /// <summary>(GameState, viewer) → xm surface name — the fine selection the xm
    /// during×for lattice deliberately leaves to the concrete stratum (xmlang v0.2).</summary>
    public static string Select(GameState state, Guid? viewer) =>
        state.Phase switch
        {
            GamePhase.Ended => "Slutställning",
            GamePhase.Lobby => viewer == state.HostPlayerId ? "LobbyVärd" : "LobbySpelare",
            GamePhase.Started => SelectStarted(state, viewer),
            // NotCreated never reaches here (the endpoint guards GameNotFound first).
            _ => "LobbySpelare"
        };

    private static string SelectStarted(GameState state, Guid? viewer)
    {
        var i = state.CurrentQuestionIndex;
        var round = state.Questions[i];
        if (round.Scored)
            return viewer == state.HostPlayerId ? "RundresultatVärd" : "RundresultatSpelare";
        if (!state.DirectionRevealed(i))
            return viewer is { } id && round.Directions.ContainsKey(id) ? "Väntan" : "Riktningsfråga";
        // Stage 2: sized players wait; the rest sit on the mellansteg until they tap through.
        return viewer is { } pid && round.Differences.ContainsKey(pid) ? "Väntan" : "Riktningsavslöjande";
    }

    public static XmScreenModel Screen(XmSpec xm, RenderContext c)
    {
        var name = Select(c.State, c.Viewer);
        var surface = xm.Surfaces.Single(s => s.Name == name);
        var labels = xm.Labels["sv"].Elements;
        return name switch
        {
            "LobbyVärd" => LobbyHost(surface, labels, c),
            "LobbySpelare" => LobbyPlayer(surface, labels, c),
            "Riktningsfråga" or "Riktningsavslöjande" => throw new InvalidOperationException(
                "Riktningsfråga/Riktningsavslöjande opt out to the hand-written idioms (QuestionScreen/DirectionResultsScreen)."),
            "Väntan" => Waiting(surface, labels, c),
            "RundresultatVärd" or "RundresultatSpelare" => RoundResults(surface, labels, c),
            _ => Standings(surface, labels, c)
        };
    }

    public static string Label(XmSpec xm, string element) =>
        xm.Labels["sv"].Elements[element].Self!;

    public static string Label(XmSpec xm, string element, string field) =>
        xm.Labels["sv"].Elements[element].Fields[field].Self!;

    private static XmScreenModel LobbyHost(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, RenderContext c)
    {
        var canStart = c.State.Players.Count >= 2;
        var fields = new Dictionary<string, Field>
        {
            ["joinCode"] = new QrField(QrCode.SvgFor(c.JoinUrl), c.JoinUrl, c.ShowJoinUrl),
            ["players"] = LobbyRoster(c, labels),
        };
        return new XmScreenModel(
            surface, fields,
            canStart ? [new CommandModel("StartGame", labels["StartGame"].Self!, Route(c, "start"))] : [],
            c.Token,
            Heading: labels["LobbyVärd"].Self,
            Sub: "Övriga spelare går med genom att skanna QR-koden.",
            Footer: canStart ? null : $"Behöver minst 2 spelare · {c.State.Players.Count} ansluten(a)",
            PollPath: StatePath(c, withUrl: c.ShowJoinUrl),
            Steps: HowItWorks);
    }

    private static XmScreenModel LobbyPlayer(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, RenderContext c)
    {
        var hostName = c.State.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
        var fields = new Dictionary<string, Field> { ["players"] = LobbyRoster(c, labels) };
        return new XmScreenModel(
            surface, fields, [], c.Token,
            Heading: labels["LobbySpelare"].Self,
            Sub: $"Väntar på att {hostName} startar omgången…",
            PollPath: StatePath(c),
            Steps: HowItWorks);
    }

    /// <summary>players self-highlighting ("du") per the xm self: players.playerId.</summary>
    private static RosterField LobbyRoster(RenderContext c, IReadOnlyDictionary<string, XmLabelEntry> labels) =>
        new(labels["Roster"].Fields["players"].Self,
            [.. c.State.Players.Select(p => new RosterRow(
                p.Name + (p.IsHost ? " (värd)" : ""),
                p.PlayerId == c.Viewer ? "du" : "med"))],
            CountPill: $"{c.State.Players.Count} med");

    private static XmScreenModel Waiting(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, RenderContext c)
    {
        var i = c.State.CurrentQuestionIndex;
        var round = c.State.Questions[i];
        // Pending follows the active stage: directions before the reveal, differences after.
        var submitted = c.State.DirectionRevealed(i)
            ? (IReadOnlySet<Guid>)round.Differences.Keys.ToHashSet()
            : round.Directions.Keys.ToHashSet();
        var done = c.State.Players.Where(p => submitted.Contains(p.PlayerId)).ToList();
        var pending = c.State.Players.Where(p => !submitted.Contains(p.PlayerId)).ToList();
        var view = labels["Guess progress"].Fields;

        var fields = new Dictionary<string, Field>
        {
            ["questionIndex"] = new TextField($"Fråga {i + 1} / {c.State.Questions.Count}", "pill"),
        };
        if (pending.Count > 0)
            fields["pendingPlayerIds"] = new RosterField(view["pendingPlayerIds"].Self,
                [.. pending.Select(p => new RosterRow(p.Name, "gissar…", Pending: true))]);
        if (done.Count > 0)
            fields["submittedPlayerIds"] = new RosterField(view["submittedPlayerIds"].Self,
                [.. done.Select(p => new RosterRow(p.Name, p.PlayerId == c.Viewer ? "du · klar" : "klar"))]);

        return new XmScreenModel(
            surface, fields, [], c.Token,
            Heading: labels["Väntan"].Self,
            Sub: $"{done.Count} av {c.State.Players.Count} klara",
            PollPath: StatePath(c));
    }

    private static XmScreenModel RoundResults(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, RenderContext c)
    {
        var i = c.State.CurrentQuestionIndex;
        var round = c.State.Questions[i];
        var card = round.Card;
        var viewerIsHost = c.Viewer == c.State.HostPlayerId;
        var logo = LogoResolver(c.State, c.ResolveLogo);
        var aLarger = card.ValueA >= card.ValueB;
        var largerItem = aLarger ? card.ItemA : card.ItemB;
        var smallerItem = aLarger ? card.ItemB : card.ItemA;
        var largerValue = Math.Max(card.ValueA, card.ValueB);
        var smallerValue = Math.Min(card.ValueA, card.ValueB);
        var smallerPercent = largerValue == 0 ? 0 : (int)Math.Round(smallerValue / largerValue * 100, MidpointRounding.AwayFromZero);

        var fields = new Dictionary<string, Field>
        {
            ["questionIndex"] = new TextField($"Fråga {i + 1} / {c.State.Questions.Count} · resultat", "pill"),
            ["correctDirection"] = new TextField("är MER", "answer", Strong: largerItem),
            ["correctDifference"] = new BarsField(
                new BarRow(Fmt(largerValue), $"{largerItem} · {Fmt(largerValue)} {card.Unit}", logo(largerItem)),
                new BarRow($"{Fmt(smallerValue)} · {smallerPercent}%", $"{smallerItem} · {Fmt(smallerValue)} {card.Unit}", logo(smallerItem)),
                smallerPercent,
                Caption: $"Facit: mindre är {smallerPercent}% av mer"),
            ["playerScores"] = ScoresTable(c.State, round, largerValue),
        };
        return new XmScreenModel(
            surface, fields,
            viewerIsHost ? [NextCommand(c, labels)] : [],
            c.Token,
            Heading: Heading(c.State, card.QuestionText),
            Footer: viewerIsHost
                ? "Rätt på mer/mindre ger −10 bonus; poängen är hur många procent du gissade fel."
                : "Värden visar strax nästa fråga…",
            PollPath: StatePath(c),
            Source: card.Source);
    }

    /// <summary>AskNextQuestion/EndGame mutual exclusion on hasNextQuestion (xm finding 3: System
    /// processors in the model, host buttons in the UI). Both bind to the /next gear route.</summary>
    private static CommandModel NextCommand(RenderContext c, IReadOnlyDictionary<string, XmLabelEntry> labels) =>
        c.State.HasNextQuestion
            ? new CommandModel("AskNextQuestion", labels["AskNextQuestion"].Self!, Route(c, "next"))
            : new CommandModel("EndGame", labels["EndGame"].Self!, Route(c, "next"));

    private static TableField ScoresTable(GameState state, QuestionRound round, decimal largerValue)
    {
        var correct = round.CorrectDirection!.Value;
        var rows = state.Players
            .Select(p => new
            {
                Who = p.Name + (p.PlayerId == state.HostPlayerId ? " (värd)" : ""),
                Dir = round.Directions[p.PlayerId],
                Norm = Decider.NormalizeDifference(round.Differences[p.PlayerId], largerValue),
                RoundScore = round.RoundScores[p.PlayerId],
                Total = state.TotalScore(p.PlayerId),
            })
            .OrderBy(r => r.Total)
            .Select((r, idx) => new TableRow(
            [
                new TableCell((idx + 1).ToString(), "rank"),
                new TableCell(r.Who, "who"),
                new TableCell(r.Dir == Direction.Mer ? "mer" : "mindre", "round", "Mer eller Mindre",
                    Bad: r.Dir != correct, Ok: r.Dir == correct),
                new TableCell($"{100 - r.Norm}%", "round", "Mindre/mer"),
                new TableCell(Signed(r.RoundScore), "round", "Rond"),
                new TableCell(r.Total.ToString(), "total"),
            ]));
        return new TableField(
            [new TableCell("#", "rank"), new TableCell("Spelare"), new TableCell("Mer eller Mindre?", "round"), new TableCell("Mindre av mer", "round"), new TableCell("Rond", "round"), new TableCell("Total", "total")],
            [.. rows]);
    }

    private static XmScreenModel Standings(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, RenderContext c)
    {
        var winners = c.State.WinnerIds.ToHashSet();
        var names = c.State.Players.ToDictionary(p => p.PlayerId, p => p.Name);
        // LOWEST total wins — ascending, top of the board is best.
        var rows = c.State.FinalScoreboard
            .OrderBy(e => e.TotalScore)
            .Select((e, idx) => new TableRow(
                [
                    new TableCell((idx + 1).ToString(), "rank"),
                    new TableCell(e.PlayerName + (e.PlayerId == c.State.HostPlayerId ? " (värd)" : ""), "who"),
                    new TableCell(e.TotalScore.ToString(), "total"),
                ],
                IsWinner: winners.Contains(e.PlayerId)));
        var winnerNames = c.State.WinnerIds.Select(id => names.GetValueOrDefault(id, "")).ToList();

        var fields = new Dictionary<string, Field>
        {
            ["winnerIds"] = new TextField(
                (winnerNames.Count > 1 ? "delar segern!" : "vann!") + " 🏆", "winner-banner",
                Strong: string.Join(" & ", winnerNames)),
            ["finalScoreboard"] = new TableField([], [.. rows]),
        };
        return new XmScreenModel(
            surface, fields, [], c.Token,
            Heading: labels["Slutställning"].Self,
            Sub: "Lägst total poäng vinner.",
            PlayAgainHref: "/",
            ShareText: "Vi spelade just Mer eller Mindre, frågespelet där ni gissar mer/mindre och sedan hur stor skillnaden är. Online men tillsammans i samma rum. Testa själv!");
    }

    /// <summary>The picker/slider idiom's view-model (both tvåstegsraket stages) — the two
    /// surfaces the xm renderer does not draw (finding 5).</summary>
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

    /// <summary>The mellansteg's view-model — self-paced GET tap-through, so it stays a
    /// hand-written screen (finding 8).</summary>
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

    /// <summary>
    /// Logo lookup, but only for loggor-* packs (slug-prefix convention) — text packs always
    /// resolve to null so they render names. GameSurfaces stays IO-free: the caller injects the
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

    /// <summary>
    /// Slider step in the card's own unit: coarse for big magnitudes (km², kr), 1 for medium
    /// (mil, °C), fine for small (miljoner). Simplicity over a per-card range column.
    /// </summary>
    public static decimal SliderStep(decimal max) =>
        max >= 200 ? Math.Round(max / 100)
        : max >= 20 ? 1
        : 0.1m;

    private static string Fmt(decimal d) => d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string Signed(int n) => n > 0 ? $"+{n}" : n.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Static game-rules copy — neither data nor judgment about data, so it is
    /// deliberately inexpressible in xm and lives here as residue.</summary>
    private static readonly StepsField HowItWorks = new("Så funkar det",
    [
        new XmStep("Gissa två gånger.", "Först: är A mer eller mindre än B? Sedan: hur stor är skillnaden?"),
        new XmStep("Lägst vinner.", "Ju närmare facit, desto lägre poäng, och rätt riktning ger bonus."),
    ]);

    private static string Route(RenderContext c, string action) =>
        $"/games/{c.State.JoinCode:N}/{action}";

    private static string StatePath(RenderContext c, bool withUrl = false) =>
        $"/games/{c.State.JoinCode:N}/state{(withUrl ? "?url" : "")}";
}
