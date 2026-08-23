using Xmlang;

namespace MerEllerMindre.Web.Xm;

/// <summary>The closed set of command input idioms. None = a plain submit button.
/// Grows only by plan-level decision (MEM adds DirectionPicker/DifferenceSlider).</summary>
public enum CommandInput { None, Keypad }

/// <summary>A composed command materialized for rendering: its xm $self label, the
/// hand-written endpoint route it posts to, and its input idiom. The interpreter renders
/// the FORM only — binding records, endpoints and Decider dispatch stay hand-written.</summary>
public sealed record CommandModel(
    string Name,
    string Label,
    string Route,
    CommandInput Input = CommandInput.None,
    string? InputLabel = null,
    string? Unit = null);

/// <summary>
/// Everything SurfaceRenderer needs for one screen: the xm surface (composition order,
/// salience tiers), the materialized FieldBag keyed by EM field names, the commands, and
/// the residue chrome slots. Presence contract: the xm owns order and tier; the BAG owns
/// presence — a field the materializer leaves out (e.g. data composed into another field's
/// copy, or an on-demand id nobody renders) is a deliberate, unit-tested judgment, never a
/// render-time fallback.
/// </summary>
public sealed record XmScreenModel(
    XmSurface Surface,
    IReadOnlyDictionary<string, Field> Fields,
    IReadOnlyList<CommandModel> Commands,
    string Token,
    string? Heading = null,
    string? Sub = null,
    string? Footer = null,
    string? PollPath = null,
    string? PlayAgainHref = null,
    string? ShareText = null);
