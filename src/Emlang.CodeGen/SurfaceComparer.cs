namespace Emlang.CodeGen;

/// <summary>
/// One spec-to-code surface mismatch. <see cref="Key"/> is stable so a shadow test can
/// allowlist a logged finding without matching the human-readable detail text.
/// </summary>
public record Divergence(string Game, char Kind, string Element, string Code, string Detail, int? SpecLine)
{
    public string Key => $"{Game}:{Kind}:{Element}:{Code}";
}

/// <summary>
/// Compares an emlang spec's c:/e:/x: surface against the committed Commands.cs /
/// Events.cs / Errors.cs, both directions. Output is DATA for the step-0 experiment —
/// divergences are classified as mapping rules, manifest facts, or logged findings.
/// </summary>
public static class SurfaceComparer
{
    public static IReadOnlyList<Divergence> Compare(GameManifest manifest)
    {
        var root = RepoRoot.Locate();
        return Compare(manifest,
            File.ReadAllText(Path.Combine(root, manifest.SpecPath)),
            File.ReadAllText(Path.Combine(root, manifest.CommandsFile)),
            File.ReadAllText(Path.Combine(root, manifest.EventsFile)),
            File.ReadAllText(Path.Combine(root, manifest.ErrorsFile)));
    }

    public static IReadOnlyList<Divergence> Compare(
        GameManifest manifest, string specYaml, string commandsSource, string eventsSource, string errorsSource)
    {
        var spec = SpecModel.Parse(specYaml);
        var divergences = new List<Divergence>();
        CompareKind(manifest.Game, 'c', spec, commandsSource, manifest.CommandUnion, divergences);
        CompareKind(manifest.Game, 'e', spec, eventsSource, manifest.EventUnion, divergences);
        CompareKind(manifest.Game, 'x', spec, errorsSource, manifest.ErrorUnion, divergences);
        foreach (var source in new[] { commandsSource, eventsSource, errorsSource })
            CompareNamespace(manifest, source, divergences);
        return divergences;
    }

    private static void CompareKind(
        string game, char kind, IReadOnlyList<SpecElement> spec, string source, string unionName,
        List<Divergence> output)
    {
        var elements = spec.Where(e => e.Kind == kind).ToList();
        var records = CodeSurface.Records(source)
            .Where(r => r.Name != unionName)
            .ToDictionary(r => r.Name);

        foreach (var element in elements)
        {
            if (records.TryGetValue(element.Name, out var record))
                CompareProps(game, element, record, output);
            else
                output.Add(new(game, kind, element.Name, "missing-record",
                    "spec element has no committed record", element.Line));
        }

        foreach (var orphan in records.Keys.Except(elements.Select(e => e.Name)))
            output.Add(new(game, kind, orphan, "unspecced-record",
                "committed record has no spec element", null));

        CompareUnion(game, kind, elements, source, unionName, output);
    }

    private static void CompareProps(string game, SpecElement element, CodeRecord record, List<Divergence> output)
    {
        var specNames = element.Props.Select(p => p.Name).ToList();
        var codeNames = record.Parameters.Select(p => p.Name).ToList();

        foreach (var prop in element.Props.Where(p => !codeNames.Contains(p.Name)))
            output.Add(new(game, element.Kind, element.Name, $"missing-prop:{prop.Name}",
                $"spec prop {prop.Name}: {prop.Type} has no positional param", prop.Line));

        foreach (var param in record.Parameters.Where(p => !specNames.Contains(p.Name)))
            output.Add(new(game, element.Kind, element.Name, $"extra-param:{param.Name}",
                $"positional param {param.Name}: {param.Type} has no spec prop", element.Line));

        foreach (var (prop, param) in element.Props
            .Join(record.Parameters, p => p.Name, p => p.Name, (prop, param) => (prop, param))
            .Where(pair => pair.prop.Type != pair.param.Type))
            output.Add(new(game, element.Kind, element.Name, $"prop-type:{prop.Name}",
                $"spec says {prop.Type}, code says {param.Type}", prop.Line));

        var common = specNames.Intersect(codeNames).ToList();
        if (!specNames.Where(common.Contains).SequenceEqual(codeNames.Where(common.Contains)))
            output.Add(new(game, element.Kind, element.Name, "prop-order",
                $"spec order [{string.Join(", ", specNames)}] vs code order [{string.Join(", ", codeNames)}]",
                element.Line));
    }

    /// <summary>The namespace is a MANIFEST fact (the spec cannot say it) — verify the
    /// committed files agree, so this check is strictly stronger than the old
    /// assembly-lookup regex facts it supersedes.</summary>
    private static void CompareNamespace(GameManifest manifest, string source, List<Divergence> output)
    {
        var actual = CodeSurface.Namespace(source);
        if (actual != manifest.Namespace)
            output.Add(new(manifest.Game, 'n', actual ?? "(none)", "wrong-namespace",
                $"expected namespace {manifest.Namespace}", null));
    }

    private static void CompareUnion(
        string game, char kind, IReadOnlyList<SpecElement> elements, string source, string unionName,
        List<Divergence> output)
    {
        if (CodeSurface.Union(source, unionName) is not { } union)
        {
            output.Add(new(game, kind, unionName, "missing-union", "union declaration not found", null));
            return;
        }

        var names = elements.Select(e => e.Name).ToList();
        foreach (var missing in names.Except(union.Members))
            output.Add(new(game, kind, missing, "union-missing",
                $"spec element is not a case of union {unionName}", null));
        foreach (var extra in union.Members.Except(names))
            output.Add(new(game, kind, extra, "union-extra",
                $"union {unionName} case has no spec element", null));
    }
}
