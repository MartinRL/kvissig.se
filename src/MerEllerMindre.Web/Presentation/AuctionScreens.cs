using System.Globalization;
using Blindbudet.Domain;

namespace MerEllerMindre.Web.Presentation;

/// <summary>Which Blindbudet screen a viewer sees, derived purely from auction state + viewer.</summary>
public enum AuctionScreenKind
{
    LobbyHost,
    LobbyPlayer,
    Bid,
    Waiting,
    RoundResults,
    Standings
}

/// <summary>
/// Pure mapping from <see cref="AuctionState"/> (+ the viewing player) to the screen to render
/// and its view-model. All the "which screen / you-perspective / winner / rank" logic lives
/// here so the Razor components stay dumb. Sister to MEM's GameScreens. HIGHEST total wins.
/// </summary>
public static class AuctionScreens
{
    private static readonly CultureInfo SvSe = CultureInfo.GetCultureInfo("sv-SE");

    public static AuctionScreenKind Select(AuctionState state, Guid? viewer) =>
        state.Phase switch
        {
            AuctionPhase.Ended => AuctionScreenKind.Standings,
            AuctionPhase.Lobby => viewer == state.HostPlayerId ? AuctionScreenKind.LobbyHost : AuctionScreenKind.LobbyPlayer,
            AuctionPhase.Started => SelectStarted(state, viewer),
            // NotCreated never reaches here (the endpoint guards AuctionNotFound first).
            _ => AuctionScreenKind.LobbyPlayer
        };

    private static AuctionScreenKind SelectStarted(AuctionState state, Guid? viewer)
    {
        var round = state.Lots[state.CurrentLotIndex];
        if (round.Resolved)
            return AuctionScreenKind.RoundResults;
        if (viewer is { } id && round.Bids.ContainsKey(id))
            return AuctionScreenKind.Waiting;
        return AuctionScreenKind.Bid;
    }

    public static AuctionLobbyVm Lobby(AuctionState state, Guid? viewer, string token, string joinUrl, bool showJoinUrl)
    {
        var players = state.Players
            .Select(p => new AuctionLobbyPlayerVm(p.Name, p.IsHost, p.PlayerId == viewer))
            .ToList();
        return new AuctionLobbyVm(
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

    public static AuctionBidVm Bid(AuctionState state, string token)
    {
        var i = state.CurrentLotIndex;
        var lot = state.Lots[i].Lot;
        return new AuctionBidVm(state.JoinCode, i + 1, state.Lots.Count, lot.Description, lot.Unit, token);
    }

    public static AuctionWaitingVm Waiting(AuctionState state, Guid? viewer)
    {
        var i = state.CurrentLotIndex;
        var submitted = state.Lots[i].Bids.Keys.ToHashSet();
        var done = state.Players
            .Where(p => submitted.Contains(p.PlayerId))
            .Select(p => new AuctionWaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        var pending = state.Players
            .Where(p => !submitted.Contains(p.PlayerId))
            .Select(p => new AuctionWaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        return new AuctionWaitingVm(state.JoinCode, i + 1, state.Lots.Count, done.Count, state.Players.Count, done, pending);
    }

    public static AuctionRoundResultsVm RoundResults(AuctionState state, Guid? viewer, string token)
    {
        var i = state.CurrentLotIndex;
        var round = state.Lots[i];
        var names = state.Players.ToDictionary(p => p.PlayerId, p => p.Name);

        var winners = round.WinnerIds.ToHashSet();
        var worth = round.TrueWorth ?? 0m;

        var rows = state.Players
            .Select(p => new AuctionRoundResultRowVm(
                p.Name,
                p.PlayerId == viewer,
                p.PlayerId == state.HostPlayerId,
                Money(round.Bids.TryGetValue(p.PlayerId, out var b) ? b : 0m),
                winners.Contains(p.PlayerId),
                round.Bids.TryGetValue(p.PlayerId, out var bid) && bid > worth,
                round.Profits.TryGetValue(p.PlayerId, out var prof) ? prof : 0,
                state.TotalScore(p.PlayerId)))
            .ToList();

        var winnerNames = round.WinnerIds
            .Select(id => names.TryGetValue(id, out var n) ? n : "")
            .ToList();

        return new AuctionRoundResultsVm(
            state.JoinCode,
            LotNumber: i + 1,
            TotalLots: state.Lots.Count,
            round.Lot.Description,
            round.Lot.Unit,
            TrueWorth: Money(worth),
            WinnerNames: winnerNames,
            PricePaid: Money(round.PricePaid ?? 0m),
            rows,
            ViewerIsHost: viewer == state.HostPlayerId,
            state.HasNextLot,
            token);
    }

    public static AuctionStandingsVm Standings(AuctionState state, Guid? viewer)
    {
        var winners = state.WinnerIds.ToHashSet();
        var names = state.Players.ToDictionary(p => p.PlayerId, p => p.Name);
        // HIGHEST total wins — the OPPOSITE of MEM (descending order, top of the board is best).
        var rows = state.FinalScoreboard
            .OrderByDescending(e => e.TotalScore)
            .Select((e, idx) => new AuctionStandingRowVm(idx + 1, e.PlayerName, e.PlayerId == state.HostPlayerId, e.TotalScore, winners.Contains(e.PlayerId)))
            .ToList();
        var winnerNames = state.WinnerIds
            .Select(id => names.TryGetValue(id, out var n) ? n : "")
            .ToList();
        return new AuctionStandingsVm(state.JoinCode, rows, winnerNames);
    }

    private static string Money(decimal value) => value.ToString("0.###", SvSe);
}
