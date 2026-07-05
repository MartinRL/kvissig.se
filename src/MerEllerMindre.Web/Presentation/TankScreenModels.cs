namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// Web-layer presentation view-models for Tänk Till Tusen (the Countdown-style number game).
/// Primitive-only on purpose — the Razor components never touch TankTillTusen.Domain, they see
/// ONLY these models (built from TankState by <see cref="TankScreens"/>). Sister to MEM's
/// ScreenModels + Blindbudet's AuctionScreenModels. LOWEST total wins (like MEM).
/// </summary>
public sealed record TankHostFormVm(string AntiforgeryToken);

public sealed record TankJoinFormVm(Guid JoinCode, string HostName, string AntiforgeryToken);

public sealed record TankLobbyPlayerVm(string Name, bool IsHost, bool IsYou);

public sealed record TankLobbyVm(
    Guid JoinCode,
    string HostName,
    string JoinUrl,
    string QrSvg,
    IReadOnlyList<TankLobbyPlayerVm> Players,
    bool ViewerIsHost,
    bool CanStart,
    bool ShowJoinUrl,
    string AntiforgeryToken);

public sealed record TankPuzzleVm(
    Guid JoinCode,
    int RoundNumber,
    int TotalRounds,
    IReadOnlyList<int> Numbers,
    int Target,
    int RemainingSeconds,
    string AntiforgeryToken);

public sealed record TankWaitingPlayerVm(string Name, bool IsYou);

public sealed record TankWaitingVm(
    Guid JoinCode,
    int RoundNumber,
    int TotalRounds,
    int DoneCount,
    int TotalCount,
    IReadOnlyList<TankWaitingPlayerVm> Done,
    IReadOnlyList<TankWaitingPlayerVm> Pending);

public sealed record TankRoundResultRowVm(
    string Name,
    bool IsYou,
    bool IsHost,
    string Reached,
    bool Missed,
    int RoundScore,
    int TotalSoFar);

public sealed record TankRoundResultsVm(
    Guid JoinCode,
    int RoundNumber,
    int TotalRounds,
    int Target,
    string SampleSolution,
    IReadOnlyList<TankRoundResultRowVm> Rows,
    bool ViewerIsHost,
    bool HasNextPuzzle,
    string AntiforgeryToken);

public sealed record TankStandingRowVm(int Rank, string Name, bool IsHost, int TotalScore, bool IsWinner);

public sealed record TankStandingsVm(
    Guid JoinCode,
    IReadOnlyList<TankStandingRowVm> Rows,
    IReadOnlyList<string> WinnerNames);
