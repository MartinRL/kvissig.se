namespace MerEllerMindre.Web.Xm;

/// <summary>
/// The CLOSED field vocabulary the xm runtime renderer can draw. Materializers (per game,
/// e.g. AuctionSurfaces) reduce domain state + xm labels to these primitives — no domain
/// type crosses this boundary, and no component in Components/Xm may ever check a game
/// name. Adding a kind is a reviewed, plan-level code event, never a per-surface special
/// case (the inner-platform tripwire).
/// </summary>
public abstract record Field;

/// <summary>One line of text. Css picks the playful.css idiom ("pill" renders as a span
/// pill, anything else as a p: sub, busy, answer, winner-banner, "muted center").
/// Label renders answer-style ("Label: value" with the value highlighted); Strong renders
/// a highlighted name span before the text (winner banner).</summary>
public sealed record TextField(string Text, string Css = "sub", string? Label = null, string? Strong = null) : Field;

/// <summary>A player-list card (lobby roster, waiting Klara / Väntar på).</summary>
public sealed record RosterField(string? Head, IReadOnlyList<RosterRow> Rows, string? CountPill = null) : Field;

public sealed record RosterRow(string Name, string Tag, bool Pending = false);

/// <summary>A scoreboard table card (round results, final standings). Empty Head = no header row.</summary>
public sealed record TableField(IReadOnlyList<TableCell> Head, IReadOnlyList<TableRow> Rows) : Field;

public sealed record TableRow(IReadOnlyList<TableCell> Cells, bool IsWinner = false);

/// <summary>Bad appends the disqualified-mark (mark bad ✗), Ok the correct-mark (mark ok ✓),
/// after the text.</summary>
public sealed record TableCell(string Text, string? Css = null, string? DataLabel = null, bool Bad = false, bool Ok = false);

/// <summary>Two proportional bars + legend — the transformer's judgment for a revealed
/// magnitude pair (MEM xm finding 7: bars are renderer vocabulary, not spec vocabulary).
/// The smaller bar's height is SmallerPercent of the larger's; Caption is an optional
/// facit line under the legend.</summary>
public sealed record BarsField(BarRow Larger, BarRow Smaller, int SmallerPercent, string? Caption = null) : Field;

public sealed record BarRow(string BarLabel, string Legend, string? LogoUrl = null);

/// <summary>The scannable join QR, with an optional plain-text join URL + copy button.</summary>
public sealed record QrField(string Svg, string JoinUrl, bool ShowUrl) : Field;

/// <summary>Instructional how-it-works steps — static game-rules copy, deliberately
/// inexpressible in xm (it is neither data nor judgment about data).</summary>
public sealed record StepsField(string Head, IReadOnlyList<Step> Steps) : Field;

public sealed record Step(string Lead, string Rest);
