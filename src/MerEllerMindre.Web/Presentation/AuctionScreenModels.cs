namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// Web-layer presentation view-models for Blindbudet (the sealed-bid auction). Primitive-only
/// on purpose — the Razor components never touch Blindbudet.Domain, they see ONLY these models
/// (built from AuctionState by <see cref="AuctionScreens"/>). Sister to MEM's ScreenModels.
/// Money-ish decimals are pre-formatted to sv-SE strings here so the components stay dumb.
/// </summary>
public sealed record AuctionPackVm(string PackId, string Name, int LotCount);

public sealed record AuctionCatalogVm(IReadOnlyList<AuctionPackVm> Packs);

public sealed record AuctionHostFormVm(string PackId, string PackName, string AntiforgeryToken);

public sealed record AuctionJoinFormVm(Guid JoinCode, string HostName, string AntiforgeryToken);

public sealed record AuctionLobbyPlayerVm(string Name, bool IsHost, bool IsYou);

public sealed record AuctionLobbyVm(
    Guid JoinCode,
    string HostName,
    string JoinUrl,
    string QrSvg,
    IReadOnlyList<AuctionLobbyPlayerVm> Players,
    bool ViewerIsHost,
    bool CanStart,
    bool ShowJoinUrl,
    string AntiforgeryToken);

public sealed record AuctionBidVm(
    Guid JoinCode,
    int LotNumber,
    int TotalLots,
    string Description,
    string Unit,
    string AntiforgeryToken);

public sealed record AuctionWaitingPlayerVm(string Name, bool IsYou);

public sealed record AuctionWaitingVm(
    Guid JoinCode,
    int LotNumber,
    int TotalLots,
    int DoneCount,
    int TotalCount,
    IReadOnlyList<AuctionWaitingPlayerVm> Done,
    IReadOnlyList<AuctionWaitingPlayerVm> Pending);

public sealed record AuctionRoundResultRowVm(
    string Name,
    bool IsYou,
    bool IsHost,
    string Bid,
    bool IsWinner,
    int Profit,
    int TotalSoFar);

public sealed record AuctionRoundResultsVm(
    Guid JoinCode,
    int LotNumber,
    int TotalLots,
    string Description,
    string Unit,
    string TrueWorth,
    string WinnerName,
    string PricePaid,
    IReadOnlyList<AuctionRoundResultRowVm> Rows,
    bool ViewerIsHost,
    bool HasNextLot,
    string AntiforgeryToken);

public sealed record AuctionStandingRowVm(int Rank, string Name, bool IsHost, int TotalScore, bool IsWinner);

public sealed record AuctionStandingsVm(
    Guid JoinCode,
    IReadOnlyList<AuctionStandingRowVm> Rows,
    IReadOnlyList<string> WinnerNames);
