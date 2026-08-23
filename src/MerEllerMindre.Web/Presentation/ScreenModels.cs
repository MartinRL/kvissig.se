using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// Web-layer presentation view-models for the screens that OPT OUT of the xm renderer
/// (catalog/host form residue and the tvåstegsraket picker/slider + mellansteg idioms).
/// Built from <see cref="GameState"/> by <see cref="GameSurfaces"/>; the Razor components
/// see ONLY these models. Everything else renders through SurfaceRenderer's Field bag.
/// </summary>
public sealed record PackVm(string PackId, string Name, int QuestionCount, bool IsNew = false);

public sealed record CatalogVm(IReadOnlyList<PackVm> Packs);

public sealed record HostFormVm(string PackId, string PackName, string AntiforgeryToken, int DefaultRoundCount);

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
    decimal SliderMax,
    decimal SliderStep,
    QuestionStage Stage,
    // Stage 2 only: the now-revealed direction so the slider fixes the taller (MER) bar.
    Direction? RevealedDirection,
    string AntiforgeryToken,
    // Logo mode (loggor-* packs): PNG URLs for ItemA/ItemB; null in text mode.
    string? LogoA = null,
    string? LogoB = null);

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
    string AntiforgeryToken,
    // Logo mode: PNGs for the Mer/Mindre items (direction AND names revealed — the screen shows
    // logo + name now that the direction guess is locked).
    string? MerLogo = null,
    string? MindreLogo = null);
