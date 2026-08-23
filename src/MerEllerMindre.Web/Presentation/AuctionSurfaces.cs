using System.Globalization;
using Blindbudet.Domain;
using MerEllerMindre.Web.Xm;
using Xmlang;

namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// The Blindbudet residue for the xm runtime renderer: the fine screen selector (xm v0.2
/// finding 8 — inexpressible in during×for), the per-surface materializers reducing
/// AuctionState + xm labels to the closed Field vocabulary, and the command bindings
/// (route table + AskNextLot/EndAuction mutual exclusion on hasNextLot). All judgment
/// that used to live only in Razor is a pure, unit-testable function here.
/// </summary>
public static class AuctionSurfaces
{
    private static readonly CultureInfo SvSe = CultureInfo.GetCultureInfo("sv-SE");

    /// <summary>(AuctionState, viewer) → xm surface name — the fine selection the xm
    /// during×for lattice deliberately leaves to the concrete stratum (xmlang v0.2).</summary>
    public static string Select(AuctionState state, Guid? viewer) =>
        state.Phase switch
        {
            AuctionPhase.Ended => "Slutställning",
            AuctionPhase.Lobby => viewer == state.HostPlayerId ? "LobbyVärd" : "LobbySpelare",
            AuctionPhase.Started => SelectStarted(state, viewer),
            // NotCreated never reaches here (the endpoint guards AuctionNotFound first).
            _ => "LobbySpelare"
        };

    private static string SelectStarted(AuctionState state, Guid? viewer)
    {
        var round = state.Lots[state.CurrentLotIndex];
        if (round.Resolved)
            return viewer == state.HostPlayerId ? "RundresultatVärd" : "RundresultatSpelare";
        return viewer is { } id && round.Bids.ContainsKey(id) ? "Väntan" : "Budgivning";
    }

    public static XmScreenModel Screen(XmSpec xm, AuctionRenderContext c)
    {
        var name = Select(c.State, c.Viewer);
        var surface = xm.Surfaces.Single(s => s.Name == name);
        var labels = xm.Labels["sv"].Elements;
        return name switch
        {
            "LobbyVärd" => LobbyHost(surface, labels, c),
            "LobbySpelare" => LobbyPlayer(surface, labels, c),
            "Budgivning" => Bid(surface, labels, c),
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
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, AuctionRenderContext c)
    {
        var canStart = c.State.Players.Count >= 2;
        var fields = new Dictionary<string, Field>
        {
            ["joinCode"] = new QrField(QrCode.SvgFor(c.JoinUrl), c.JoinUrl, c.ShowJoinUrl),
            ["players"] = LobbyRoster(c, labels),
        };
        return new XmScreenModel(
            surface, fields,
            canStart ? [new CommandModel("StartAuction", labels["StartAuction"].Self!, Route(c, "start"))] : [],
            c.Token,
            Heading: labels["LobbyVärd"].Self,
            Sub: "Övriga spelare går med genom att skanna QR-koden.",
            Footer: canStart ? null : $"Behöver minst 2 spelare · {c.State.Players.Count} ansluten(a)",
            PollPath: StatePath(c, withUrl: c.ShowJoinUrl),
            Steps: HowItWorks);
    }

    private static XmScreenModel LobbyPlayer(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, AuctionRenderContext c)
    {
        var hostName = c.State.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
        var fields = new Dictionary<string, Field> { ["players"] = LobbyRoster(c, labels) };
        return new XmScreenModel(
            surface, fields, [], c.Token,
            Heading: labels["LobbySpelare"].Self,
            Sub: $"Väntar på att {hostName} startar auktionen…",
            PollPath: StatePath(c),
            Steps: HowItWorks);
    }

    /// <summary>players self-highlighting ("du") per the xm self: players.playerId.</summary>
    private static RosterField LobbyRoster(AuctionRenderContext c, IReadOnlyDictionary<string, XmLabelEntry> labels) =>
        new(labels["Screen / Auction lobby"].Fields["players"].Self,
            [.. c.State.Players.Select(p => new RosterRow(
                p.Name + (p.IsHost ? " (värd)" : ""),
                p.PlayerId == c.Viewer ? "du" : "med"))],
            CountPill: $"{c.State.Players.Count} med");

    private static XmScreenModel Bid(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, AuctionRenderContext c)
    {
        var i = c.State.CurrentLotIndex;
        var lot = c.State.Lots[i].Lot;
        // description/unit are hoisted into the heading + keypad; totalLots is composed into
        // the lotIndex pill copy — the presence contract's deliberate omissions.
        var fields = new Dictionary<string, Field>
        {
            ["lotIndex"] = new TextField($"Budrunda {i + 1} / {c.State.Lots.Count} · BlindBudet", "pill"),
        };
        return new XmScreenModel(
            surface, fields,
            [new CommandModel("PlaceBid", labels["PlaceBid"].Self!, Route(c, "bid"),
                CommandInput.Keypad, labels["PlaceBid"].Fields["amount"].Self, lot.Unit)],
            c.Token,
            Heading: lot.Description,
            Sub: "Vad tror du den är värd? Ditt bud är hemligt tills alla har bjudit.");
        // No PollPath: typed-input surface — polling would wipe a half-entered bid.
    }

    private static XmScreenModel Waiting(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, AuctionRenderContext c)
    {
        var i = c.State.CurrentLotIndex;
        var submitted = c.State.Lots[i].Bids.Keys.ToHashSet();
        var done = c.State.Players.Where(p => submitted.Contains(p.PlayerId)).ToList();
        var pending = c.State.Players.Where(p => !submitted.Contains(p.PlayerId)).ToList();
        var view = labels["Screen / Waiting for bids"].Fields;

        var fields = new Dictionary<string, Field>
        {
            ["lotIndex"] = new TextField($"Budrunda {i + 1} / {c.State.Lots.Count}", "pill"),
        };
        if (pending.Count > 0)
            fields["pendingPlayerIds"] = new RosterField(view["pendingPlayerIds"].Self,
                [.. pending.Select(p => new RosterRow(p.Name, "bjuder…", Pending: true))]);
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
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, AuctionRenderContext c)
    {
        var i = c.State.CurrentLotIndex;
        var round = c.State.Lots[i];
        var view = labels["Screen / Round results"].Fields;
        var viewerIsHost = c.Viewer == c.State.HostPlayerId;

        var fields = new Dictionary<string, Field>
        {
            ["lotIndex"] = new TextField($"Budrunda {i + 1} / {c.State.Lots.Count}", "pill"),
            ["trueWorth"] = new TextField($"{Money(round.TrueWorth ?? 0m)} {round.Lot.Unit}", "answer", Label: view["trueWorth"].Self),
            // pricePaid is composed into the winner line, never a field of its own.
            ["winnerIds"] = new TextField(WinnerLine(c.State, round, view["winnerIds"].Empty!), "sub"),
            ["playerProfits"] = ResultsTable(c.State, round, c.Viewer, view),
        };
        return new XmScreenModel(
            surface, fields,
            viewerIsHost ? [NextCommand(c, labels)] : [],
            c.Token,
            Heading: round.Lot.Description,
            Footer: viewerIsHost ? null : "Väntar på att värden går vidare…",
            PollPath: StatePath(c));
    }

    /// <summary>AskNextLot/EndAuction mutual exclusion on hasNextLot (EM finding 3: System
    /// processors in the model, host buttons in the UI). Both bind to the /next gear route.</summary>
    private static CommandModel NextCommand(AuctionRenderContext c, IReadOnlyDictionary<string, XmLabelEntry> labels) =>
        c.State.HasNextLot
            ? new CommandModel("AskNextLot", labels["AskNextLot"].Self!, Route(c, "next"))
            : new CommandModel("EndAuction", labels["EndAuction"].Self!, Route(c, "next"));

    private static string WinnerLine(AuctionState state, LotRound round, string emptyLabel)
    {
        var names = state.Players.ToDictionary(p => p.PlayerId, p => p.Name);
        var winners = round.WinnerIds.Select(id => names.GetValueOrDefault(id, "")).ToList();
        var price = $"{Money(round.PricePaid ?? 0m)} {round.Lot.Unit}";
        return winners.Count switch
        {
            0 => emptyLabel,
            1 => $"{winners[0]} vann med budet {price}.",
            _ => $"{string.Join(" och ", winners)} delade vinsten med budet {price}."
        };
    }

    private static TableField ResultsTable(
        AuctionState state, LotRound round, Guid? viewer, IReadOnlyDictionary<string, XmLabelEntry> view)
    {
        var worth = round.TrueWorth ?? 0m;
        var bidLabel = view["pricePaid"].Self!;
        var profitLabel = view["playerProfits"].Self!;
        var winners = round.WinnerIds.ToHashSet();
        var rows = state.Players.Select(p => new TableRow(
            [
                new TableCell(Who(p, state, viewer), "who"),
                new TableCell(Money(round.Bids.GetValueOrDefault(p.PlayerId)), "round", bidLabel,
                    Bad: round.Bids.TryGetValue(p.PlayerId, out var bid) && bid > worth),
                new TableCell(round.Profits.GetValueOrDefault(p.PlayerId).ToString(SvSe), "round", profitLabel),
                new TableCell(state.TotalScore(p.PlayerId).ToString(SvSe), "total"),
            ],
            IsWinner: winners.Contains(p.PlayerId)));
        return new TableField(
            [new TableCell("Spelare", "who"), new TableCell(bidLabel), new TableCell(profitLabel), new TableCell("Total", "total")],
            [.. rows]);
    }

    private static string Who(Player p, AuctionState state, Guid? viewer) =>
        p.Name + (p.PlayerId == state.HostPlayerId ? " (värd)" : "") + (p.PlayerId == viewer ? " · du" : "");

    private static XmScreenModel Standings(
        XmSurface surface, IReadOnlyDictionary<string, XmLabelEntry> labels, AuctionRenderContext c)
    {
        var winners = c.State.WinnerIds.ToHashSet();
        var names = c.State.Players.ToDictionary(p => p.PlayerId, p => p.Name);
        // HIGHEST total wins — the OPPOSITE of MEM (descending, top of the board is best).
        var rows = c.State.FinalScoreboard
            .OrderByDescending(e => e.TotalScore)
            .Select((e, idx) => new TableRow(
                [
                    new TableCell((idx + 1).ToString(SvSe), "rank"),
                    new TableCell(e.PlayerName + (e.PlayerId == c.State.HostPlayerId ? " (värd)" : ""), "who"),
                    new TableCell(e.TotalScore.ToString(SvSe), "total"),
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
            Sub: "Högst total poäng vinner.",
            PlayAgainHref: "/blindbudet",
            ShareText: "Vi spelade just BlindBudet, auktionsspelet där ni lägger hemliga bud utan att bjuda över; närmast (eller exakt) vinner. Online men tillsammans i samma rum. Testa själv!");
    }

    /// <summary>Static game-rules copy — neither data nor judgment about data, so it is
    /// deliberately inexpressible in xm and lives here as residue.</summary>
    private static readonly StepsField HowItWorks = new("Så funkar det",
    [
        new Step("Lägg ett hemligt bud", "i varje budrunda: vad tror du det sanna värdet är?"),
        new Step("Bjud inte över", ": det högsta budet som inte överstiger sant värde vinner. Pricka sant värde exakt så får du 10 poäng. Bjuder du över får du 0."),
        new Step("Högst total vinner.", ""),
    ]);

    private static string Route(AuctionRenderContext c, string action) =>
        $"/blindbudet/{c.State.JoinCode:N}/{action}";

    private static string StatePath(AuctionRenderContext c, bool withUrl = false) =>
        $"/blindbudet/{c.State.JoinCode:N}/state{(withUrl ? "?url" : "")}";

    private static string Money(decimal value) => value.ToString("0.###", SvSe);
}
