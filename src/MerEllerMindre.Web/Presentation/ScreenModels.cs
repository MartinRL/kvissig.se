using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// Web-layer presentation view-models. These are richer than the Domain's pure read-model
/// projections (which stay GT-verified and untouched): they fold in the viewer's "you"
/// perspective, the host/player split, slider bounds in the card's own unit, rank ordering,
/// and the raw values revealed only on the results screen. Built from <see cref="GameState"/>
/// by <see cref="GameScreens"/>; the Razor components see ONLY these models.
/// </summary>
public sealed record PackVm(string PackId, string Name, int QuestionCount);

public sealed record CatalogVm(IReadOnlyList<PackVm> Packs);

public sealed record HostFormVm(string PackId, string PackName, string AntiforgeryToken);

public sealed record JoinVm(Guid JoinCode, string HostName, string AntiforgeryToken);

public sealed record LobbyPlayerVm(string Name, bool IsHost, bool IsYou);

public sealed record LobbyVm(
    Guid JoinCode,
    string HostName,
    string JoinUrl,
    string QrSvg,
    IReadOnlyList<LobbyPlayerVm> Players,
    bool ViewerIsHost,
    bool CanStart,
    string AntiforgeryToken);

/// <summary>Which half of the tvåstegsraket a question screen renders.</summary>
public enum QuestionStage
{
    Direction,
    Difference
}

public sealed record QuestionVm(
    Guid JoinCode,
    int QuestionNumber,
    int TotalQuestions,
    string QuestionText,
    string ItemA,
    string ItemB,
    string DifferencePrompt,
    string Unit,
    decimal SliderMax,
    decimal SliderStep,
    QuestionStage Stage,
    // Stage 2 only: the now-revealed direction so the slider fixes the taller (MER) bar.
    Direction? RevealedDirection,
    string AntiforgeryToken);

public sealed record DirectionResultRowVm(
    string Name,
    bool IsYou,
    bool IsHost,
    Direction GuessedDirection,
    bool DirectionCorrect,
    int BonusPoints,
    int TotalSoFar);

public sealed record DirectionResultsVm(
    Guid JoinCode,
    int QuestionNumber,
    int TotalQuestions,
    string QuestionText,
    string MerItem,
    string MindreItem,
    Direction CorrectDirection,
    IReadOnlyList<DirectionResultRowVm> Rows,
    string AntiforgeryToken);

public sealed record WaitingPlayerVm(string Name, bool IsYou);

public sealed record WaitingVm(
    Guid JoinCode,
    int QuestionNumber,
    int TotalQuestions,
    int DoneCount,
    int TotalCount,
    IReadOnlyList<WaitingPlayerVm> Done,
    IReadOnlyList<WaitingPlayerVm> Pending);

public sealed record ResultRowVm(
    int Rank,
    string Name,
    bool IsYou,
    bool IsHost,
    Direction GuessedDirection,
    byte GuessedDifferenceNormalized,
    int RoundScore,
    int TotalScore);

public sealed record ResultsVm(
    Guid JoinCode,
    int QuestionNumber,
    int TotalQuestions,
    string QuestionText,
    string LargerItem,
    string SmallerItem,
    decimal LargerValue,
    decimal SmallerValue,
    string Unit,
    Direction CorrectDirection,
    byte CorrectDifference,
    int SmallerBarPercent,
    IReadOnlyList<ResultRowVm> Rows,
    bool ViewerIsHost,
    bool HasNextQuestion,
    string AntiforgeryToken);

public sealed record StandingRowVm(int Rank, string Name, bool IsYou, bool IsHost, int TotalScore, bool IsWinner);

public sealed record StandingsVm(
    Guid JoinCode,
    IReadOnlyList<StandingRowVm> Rows,
    IReadOnlyList<string> WinnerNames);
