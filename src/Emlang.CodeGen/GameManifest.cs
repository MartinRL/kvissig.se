using System.Runtime.CompilerServices;

namespace Emlang.CodeGen;

/// <summary>
/// The per-game facts the spec does NOT carry (a finding of the experiment, by
/// construction): namespace, union names, file paths. Three literal instances; no config.
/// </summary>
public record GameManifest(
    string Game,
    string SpecPath,
    string Namespace,
    string CommandsFile,
    string EventsFile,
    string ErrorsFile,
    string CommandUnion,
    string EventUnion,
    string ErrorUnion)
{
    public static readonly GameManifest MerEllerMindre = new(
        "MerEllerMindre",
        "specs/mer-eller-mindre-event-model.yaml",
        "MerEllerMindre.Domain",
        "src/MerEllerMindre.Domain/Commands.cs",
        "src/MerEllerMindre.Domain/Events.cs",
        "src/MerEllerMindre.Domain/Errors.cs",
        "GameCommand", "GameEvent", "GameError");

    public static readonly GameManifest Blindbudet = new(
        "Blindbudet",
        "specs/blindbudet-event-model.yaml",
        "Blindbudet.Domain",
        "src/Blindbudet.Domain/Commands.cs",
        "src/Blindbudet.Domain/Events.cs",
        "src/Blindbudet.Domain/Errors.cs",
        "AuctionCommand", "AuctionEvent", "AuctionError");

    public static readonly GameManifest TankTillTusen = new(
        "TankTillTusen",
        "specs/tank-till-tusen-event-model.yaml",
        "TankTillTusen.Domain",
        "src/TankTillTusen.Domain/Commands.cs",
        "src/TankTillTusen.Domain/Events.cs",
        "src/TankTillTusen.Domain/Errors.cs",
        "TankCommand", "TankEvent", "TankError");
}

/// <summary>Locates the repo root (the dir with Directory.Build.props) from this source file.</summary>
public static class RepoRoot
{
    public static string Locate([CallerFilePath] string thisFile = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(thisFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException($"repo root not found walking up from {thisFile}");
    }
}
