using TankTillTusen.Domain;

namespace MerEllerMindre.Web.Presentation;

/// <summary>Which Tänk Till Tusen screen a viewer sees, derived purely from state + viewer.</summary>
public enum TankScreenKind
{
    LobbyHost,
    LobbyPlayer,
    Puzzle,
    Waiting,
    RoundResults,
    Standings
}

/// <summary>
/// Pure mapping from <see cref="TankState"/> (+ the viewing player + wall clock) to the screen
/// to render and its view-model. All the which-screen / you-perspective / winner logic lives
/// here so the Razor components stay dumb. Sister to MEM's GameScreens. LOWEST total wins.
/// </summary>
public static class TankScreens
{
    public static TankScreenKind Select(TankState state, Guid? viewer) =>
        state.Phase switch
        {
            TankPhase.Ended => TankScreenKind.Standings,
            TankPhase.Lobby => viewer == state.HostPlayerId ? TankScreenKind.LobbyHost : TankScreenKind.LobbyPlayer,
            TankPhase.Started => SelectStarted(state, viewer),
            // NotCreated never reaches here (the endpoint guards GameNotFound first).
            _ => TankScreenKind.LobbyPlayer
        };

    private static TankScreenKind SelectStarted(TankState state, Guid? viewer)
    {
        var round = state.Rounds[state.CurrentRoundIndex];
        if (round.Scored)
            return TankScreenKind.RoundResults;
        if (viewer is { } id && round.Solutions.ContainsKey(id))
            return TankScreenKind.Waiting;
        return TankScreenKind.Puzzle;
    }

    public static TankLobbyVm Lobby(TankState state, Guid? viewer, string token, string joinUrl, bool showJoinUrl)
    {
        var players = state.Players
            .Select(p => new TankLobbyPlayerVm(p.Name, p.IsHost, p.PlayerId == viewer))
            .ToList();
        return new TankLobbyVm(
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

    public static TankPuzzleVm Puzzle(TankState state, DateTimeOffset now, string token)
    {
        var i = state.CurrentRoundIndex;
        var puzzle = state.Rounds[i].Puzzle;
        return new TankPuzzleVm(
            state.JoinCode, i + 1, state.Rounds.Count,
            puzzle.Numbers, puzzle.Target,
            RemainingSeconds(state, i, now), token);
    }

    public static TankWaitingVm Waiting(TankState state, Guid? viewer)
    {
        var i = state.CurrentRoundIndex;
        var submitted = state.Rounds[i].Solutions.Keys.ToHashSet();
        var done = state.Players
            .Where(p => submitted.Contains(p.PlayerId))
            .Select(p => new TankWaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        var pending = state.Players
            .Where(p => !submitted.Contains(p.PlayerId))
            .Select(p => new TankWaitingPlayerVm(p.Name, p.PlayerId == viewer))
            .ToList();
        return new TankWaitingVm(state.JoinCode, i + 1, state.Rounds.Count, done.Count, state.Players.Count, done, pending);
    }

    public static TankRoundResultsVm RoundResults(TankState state, Guid? viewer, string token)
    {
        var i = state.CurrentRoundIndex;
        var round = state.Rounds[i];

        var rows = state.Players
            .Select(p => new TankRoundResultRowVm(
                p.Name,
                p.PlayerId == viewer,
                p.PlayerId == state.HostPlayerId,
                round.ReachedValues.TryGetValue(p.PlayerId, out var v) ? v.ToString() : "–",
                Missed: !round.ReachedValues.ContainsKey(p.PlayerId),
                round.RoundScores.TryGetValue(p.PlayerId, out var s) ? s : 100,
                state.TotalScore(p.PlayerId)))
            .OrderBy(r => r.TotalSoFar)
            .ToList();

        return new TankRoundResultsVm(
            state.JoinCode,
            RoundNumber: i + 1,
            TotalRounds: state.Rounds.Count,
            round.Puzzle.Target,
            FormatSolution(round.Puzzle, round.SampleSolution!),
            rows,
            ViewerIsHost: viewer == state.HostPlayerId,
            state.HasNextPuzzle,
            token);
    }

    public static TankStandingsVm Standings(TankState state)
    {
        var winners = state.WinnerIds.ToHashSet();
        var names = state.Players.ToDictionary(p => p.PlayerId, p => p.Name);
        // LOWEST total wins (like MEM) — ascending, top of the board is best.
        var rows = state.FinalScoreboard
            .OrderBy(e => e.TotalScore)
            .Select((e, idx) => new TankStandingRowVm(idx + 1, e.PlayerName, e.PlayerId == state.HostPlayerId, e.TotalScore, winners.Contains(e.PlayerId)))
            .ToList();
        var winnerNames = state.WinnerIds
            .Select(id => names.TryGetValue(id, out var n) ? n : "")
            .ToList();
        return new TankStandingsVm(state.JoinCode, rows, winnerNames);
    }

    private static int RemainingSeconds(TankState state, int roundIndex, DateTimeOffset now) =>
        state.Deadline(roundIndex) is { } deadline
            ? Math.Clamp((int)Math.Ceiling((deadline - now).TotalSeconds), 0, Decider.CountdownSeconds)
            : Decider.CountdownSeconds;

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
}
