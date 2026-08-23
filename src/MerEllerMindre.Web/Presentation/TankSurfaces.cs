using MerEllerMindre.Web.Xm;
using TankTillTusen.Domain;
using Xmlang;
// The how-it-works Step (Xm) collides with the domain's solution Step.
using XmStep = MerEllerMindre.Web.Xm.Step;

namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// The Tänk Till Tusen residue for the xm runtime renderer: the fine screen selector
/// (started-phase split, xm finding 7 — inexpressible in during×for), the per-surface
/// materializers reducing TankState + xm labels to the closed Field vocabulary, and the
/// command bindings (AskNextPuzzle/EndGame mutual exclusion on hasNextPuzzle). The Pussel
/// surface OPTS OUT to the hand-written räknartejp idiom (PuzzleScreen, xm finding 5) —
/// this class still builds its view-model. Sister to AuctionSurfaces. LOWEST total wins.
/// </summary>
public static class TankSurfaces
{
    /// <summary>(TankState, viewer) → xm surface name — the fine selection the xm
    /// during×for lattice deliberately leaves to the concrete stratum (xmlang v0.2).</summary>
    public static string Select(TankState state, Guid? viewer) =>
        state.Phase switch
        {
            TankPhase.Ended => "Slutställning",
            TankPhase.Lobby => viewer == state.HostPlayerId ? "LobbyVärd" : "LobbySpelare",
            TankPhase.Started => SelectStarted(state, viewer),
            // NotCreated never reaches here (the endpoint guards GameNotFound first).
            _ => "LobbySpelare"
        };

    private static string SelectStarted(TankState state, Guid? viewer)
    {
        var round = state.Rounds[state.CurrentRoundIndex];
        if (round.Scored)
            return viewer == state.HostPlayerId ? "RundresultatVärd" : "RundresultatSpelare";
        return viewer is { } id && round.Solutions.ContainsKey(id) ? "Väntan" : "Pussel";
    }

    public static XmScreenModel Screen(XmSpec xm, TankRenderContext c)
    {
        var name = Select(c.State, c.Viewer);
        var surface = xm.Surfaces.Single(s => s.Name == name);
        var labels = xm.Labels["sv"].Elements;
        return name switch
        {
            "LobbyVärd" => LobbyHost(surface, labels, c),
            "LobbySpelare" => LobbyPlayer(surface, labels, c),
            "Pussel" => throw new InvalidOperationException(
                "Pussel opts out to the hand-written räknartejp idiom (PuzzleScreen)."),
            "Väntan" => Waiting(surface, labels, c),
            "RundresultatVärd" or "RundresultatSpelare" => RoundResults(surface, labels, c),
            _ => Standings(surface, labels, c)
        };
    }

    public static string Label(XmSpec xm, string element) =>
        xm.Labels["sv"].Elements[element].Self!;

    public static string Label(XmSpec xm, string element, string field) =>
        xm.Labels["sv"].Elements[element].Fields[field].Self!;

    /// <summary>The räknartejp idiom's view-model (tiles, operators, tape, countdown) —
    /// the one screen the xm renderer does not draw.</summary>
    public static TankPuzzleVm Puzzle(TankState state, DateTimeOffset now, string token)
    {
        var i = state.CurrentRoundIndex;
        var puzzle = state.Rounds[i].Puzzle;
        return new TankPuzzleVm(
            state.JoinCode, i + 1, state.Rounds.Count,
            puzzle.Numbers, puzzle.Target,
            RemainingSeconds(state, i, now), token);
    }

    private static int RemainingSeconds(TankState state, int roundIndex, DateTimeOffset now) =>
        state.Deadline(roundIndex) is { } deadline
            ? Math.Clamp((int)Math.Ceiling((deadline - now).TotalSeconds), 0, Decider.CountdownSeconds)
            : Decider.CountdownSeconds;

    private static XmScreenModel LobbyHost(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, TankRenderContext c)
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
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, TankRenderContext c)
    {
        var hostName = c.State.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
        var fields = new Dictionary<string, Field> { ["players"] = LobbyRoster(c, labels) };
        return new XmScreenModel(
            surface, fields, [], c.Token,
            Heading: labels["LobbySpelare"].Self,
            Sub: $"Väntar på att {hostName} startar spelet…",
            PollPath: StatePath(c),
            Steps: HowItWorks);
    }

    /// <summary>players self-highlighting ("du") per the xm self: players.playerId.</summary>
    private static RosterField LobbyRoster(TankRenderContext c, IReadOnlyDictionary<string, XmLabelEntry> labels) =>
        new(labels["Roster"].Fields["players"].Self,
            [.. c.State.Players.Select(p => new RosterRow(
                p.Name + (p.IsHost ? " (värd)" : ""),
                p.PlayerId == c.Viewer ? "du" : "med"))],
            CountPill: $"{c.State.Players.Count} med");

    private static XmScreenModel Waiting(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, TankRenderContext c)
    {
        var i = c.State.CurrentRoundIndex;
        var submitted = c.State.Rounds[i].Solutions.Keys.ToHashSet();
        var done = c.State.Players.Where(p => submitted.Contains(p.PlayerId)).ToList();
        var pending = c.State.Players.Where(p => !submitted.Contains(p.PlayerId)).ToList();
        var view = labels["Solution progress"].Fields;

        var fields = new Dictionary<string, Field>
        {
            ["roundIndex"] = RoundPill(c, i),
        };
        if (pending.Count > 0)
            fields["pendingPlayerIds"] = new RosterField(view["pendingPlayerIds"].Self,
                [.. pending.Select(p => new RosterRow(p.Name, "räknar…", Pending: true))]);
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
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, TankRenderContext c)
    {
        var i = c.State.CurrentRoundIndex;
        var round = c.State.Rounds[i];
        var view = labels["Round scores"].Fields;
        var viewerIsHost = c.Viewer == c.State.HostPlayerId;

        var fields = new Dictionary<string, Field>
        {
            ["roundIndex"] = RoundPill(c, i),
            ["target"] = new TextField(round.Puzzle.Target.ToString(), "answer", Label: view["target"].Self),
            ["sampleSolution"] = new TextField(
                FormatSolution(round.Puzzle, round.SampleSolution!), "answer", Label: view["sampleSolution"].Self),
            ["playerResults"] = ResultsTable(c.State, round, c.Viewer),
        };
        return new XmScreenModel(
            surface, fields,
            viewerIsHost ? [NextCommand(c, labels)] : [],
            c.Token,
            Footer: viewerIsHost ? null : "Väntar på att värden går vidare…",
            PollPath: StatePath(c));
    }

    /// <summary>AskNextPuzzle/EndGame mutual exclusion on hasNextPuzzle (xm finding 3: System
    /// processors in the model, host buttons in the UI). Both bind to the /next gear route.</summary>
    private static CommandModel NextCommand(TankRenderContext c, IReadOnlyDictionary<string, XmLabelEntry> labels) =>
        c.State.HasNextPuzzle
            ? new CommandModel("AskNextPuzzle", labels["AskNextPuzzle"].Self!, Route(c, "next"))
            : new CommandModel("EndGame", labels["EndGame"].Self!, Route(c, "next"));

    private static TableField ResultsTable(TankState state, PuzzleRound round, Guid? viewer)
    {
        var rows = state.Players
            .Select(p => new
            {
                p.PlayerId,
                Who = Who(p, state, viewer),
                Reached = round.ReachedValues.TryGetValue(p.PlayerId, out var v) ? v.ToString() : "–",
                Missed = !round.ReachedValues.ContainsKey(p.PlayerId),
                RoundScore = round.RoundScores.TryGetValue(p.PlayerId, out var s) ? s : 100,
                Total = state.TotalScore(p.PlayerId),
            })
            .OrderBy(r => r.Total)
            .Select(r => new TableRow(
            [
                new TableCell(r.Who, "who"),
                new TableCell(r.Reached, "round", "Nådde", Bad: r.Missed),
                new TableCell(r.RoundScore.ToString(), "round", "Poäng"),
                new TableCell(r.Total.ToString(), "total"),
            ]));
        return new TableField(
            [new TableCell("Spelare", "who"), new TableCell("Nådde"), new TableCell("Poäng"), new TableCell("Total", "total")],
            [.. rows]);
    }

    private static string Who(Player p, TankState state, Guid? viewer) =>
        p.Name + (p.PlayerId == state.HostPlayerId ? " (värd)" : "") + (p.PlayerId == viewer ? " · du" : "");

    private static XmScreenModel Standings(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, TankRenderContext c)
    {
        var winners = c.State.WinnerIds.ToHashSet();
        var names = c.State.Players.ToDictionary(p => p.PlayerId, p => p.Name);
        // LOWEST total wins (like MEM) — ascending, top of the board is best.
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
            PlayAgainHref: "/tank-till-tusen",
            ShareText: "Vi spelade just Tänk Till Tusen, sifferspelet där alla får samma sex tal och samma mål och kappas mot klockan om att nå det. Online men tillsammans i samma rum. Testa själv!");
    }

    private static TextField RoundPill(TankRenderContext c, int roundIndex) =>
        new($"Pussel {roundIndex + 1} / {c.State.Rounds.Count}", "pill");

    /// <summary>Replay the sample solution into a readable tape: "50 × 8 = 400 · 400 + 12 = 412".</summary>
    private static string FormatSolution(Puzzle puzzle, Solution solution)
    {
        var operands = puzzle.Numbers.ToList();
        var lines = new List<string>();
        foreach (var step in solution.Steps)
        {
            var a = operands[step.LeftIndex];
            var b = operands[step.RightIndex];
            var res = Apply(a, step.Op, b);
            lines.Add($"{a} {Symbol(step.Op)} {b} = {res}");
            operands.Add(res);
        }
        return lines.Count == 0 ? operands[solution.AnswerIndex].ToString() : string.Join(" · ", lines);
    }

    private static int Apply(int a, Operator op, int b) => op switch
    {
        Operator.Add => a + b,
        Operator.Sub => a - b,
        Operator.Mul => a * b,
        _ => a / b
    };

    private static string Symbol(Operator op) => op switch
    {
        Operator.Add => "+",
        Operator.Sub => "−",
        Operator.Mul => "×",
        _ => "÷"
    };

    /// <summary>Static game-rules copy — neither data nor judgment about data, so it is
    /// deliberately inexpressible in xm and lives here as residue.</summary>
    private static readonly StepsField HowItWorks = new("Så funkar det",
    [
        new XmStep("Alla får samma sex tal", "och samma mål när tiden startar."),
        new XmStep("Nå målet", "med de fyra räknesätten: kombinera två tal i taget till ett nytt. Pricka målet exakt så får du -10 poäng (bonus). Annars är ditt avstånd till målet din poäng: 5 ifrån ger 5 poäng, som mest 100. Missar någon med mer än 100 skalas rundan om, den som ligger längst ifrån får 100 och alla andra färre i förhållande till sitt avstånd. Ingen runda kostar mer än 100."),
        new XmStep("Lägst total vinner.", ""),
    ]);

    private static string Route(TankRenderContext c, string action) =>
        $"/tank-till-tusen/{c.State.JoinCode:N}/{action}";

    private static string StatePath(TankRenderContext c, bool withUrl = false) =>
        $"/tank-till-tusen/{c.State.JoinCode:N}/state{(withUrl ? "?url" : "")}";
}
