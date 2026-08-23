namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// Web-layer presentation view-models for the two Tänk Till Tusen screens that stay
/// hand-written after the xm cut-over: the host form (opts out — difficulty + roundCount
/// slider are richer than the defaults contract) and the räknartejp puzzle idiom.
/// Primitive-only on purpose — the Razor components never touch TankTillTusen.Domain.
/// Everything else renders through SurfaceRenderer via <see cref="TankSurfaces"/>.
/// </summary>
public sealed record TankHostFormVm(string AntiforgeryToken, string Difficulty);

public sealed record TankPuzzleVm(
    Guid JoinCode,
    int RoundNumber,
    int TotalRounds,
    IReadOnlyList<int> Numbers,
    int Target,
    int RemainingSeconds,
    string AntiforgeryToken);
