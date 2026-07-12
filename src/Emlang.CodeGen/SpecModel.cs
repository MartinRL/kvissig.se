using YamlDotNet.RepresentationModel;

namespace Emlang.CodeGen;

/// <summary>
/// One prop of a c:/e:/x: element, already mapped to the C# surface it determines
/// (name PascalCased, annotation mapped to a type). Line-tracked for findings.
/// </summary>
public record SpecProp(string Name, string Type, int Line);

/// <summary>One c:/e:/x: element lifted from an emlang spec's slice steps.</summary>
public record SpecElement(char Kind, string Name, IReadOnlyList<SpecProp> Props, int Line);

/// <summary>
/// Parses an emlang YAML spec into the record surface its c:/e:/x: elements define.
/// Only slice STEPS (direct-form list or `steps:`) carry type annotations; `tests:`
/// props are fixture values and are ignored.
/// </summary>
public static class SpecModel
{
    public static IReadOnlyList<SpecElement> Parse(string yamlText)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yamlText));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var slices = (YamlMappingNode)root.Children[new YamlScalarNode("slices")];

        var byKey = new Dictionary<(char Kind, string Name), SpecElement>();
        foreach (var step in slices.Children.Values.SelectMany(Steps))
        {
            if (ToElement(step) is not { } element)
                continue;
            // Rule: the props-richest occurrence defines the surface — elements reappear
            // bare as slice inputs (e.g. `- e: Game / BidPlaced` feeding a processor).
            var key = (element.Kind, element.Name);
            if (!byKey.TryGetValue(key, out var existing) || element.Props.Count > existing.Props.Count)
                byKey[key] = element;
        }
        return [.. byKey.Values];
    }

    private static IEnumerable<YamlMappingNode> Steps(YamlNode slice) => slice switch
    {
        YamlSequenceNode direct => direct.Children.OfType<YamlMappingNode>(),
        YamlMappingNode extended when extended.Children.TryGetValue(new YamlScalarNode("steps"), out var steps)
            => ((YamlSequenceNode)steps).Children.OfType<YamlMappingNode>(),
        _ => [],
    };

    private static readonly IReadOnlyDictionary<string, char> Kinds = new Dictionary<string, char>
    {
        ["c"] = 'c', ["command"] = 'c',
        ["e"] = 'e', ["event"] = 'e',
        ["x"] = 'x', ["exception"] = 'x',
    };

    private static SpecElement? ToElement(YamlMappingNode step)
    {
        foreach (var (keyNode, valueNode) in step.Children)
        {
            if (keyNode is not YamlScalarNode { Value: { } key } || !Kinds.TryGetValue(key, out var kind))
                continue;
            var raw = ((YamlScalarNode)valueNode).Value ?? string.Empty;
            return new SpecElement(kind, BareName(raw), Props(step), (int)keyNode.Start.Line);
        }
        return null;
    }

    /// <summary>Rule: events carry the stream prefix ("Game / LobbyOpened"); strip it.</summary>
    private static string BareName(string value) =>
        (value.Contains('/') ? value[(value.LastIndexOf('/') + 1)..] : value).Trim();

    private static IReadOnlyList<SpecProp> Props(YamlMappingNode step) =>
        step.Children.TryGetValue(new YamlScalarNode("props"), out var props) && props is YamlMappingNode map
            ? [.. map.Children.Select(p => new SpecProp(
                PascalCase(((YamlScalarNode)p.Key).Value!),
                MapType(((YamlScalarNode)p.Value).Value ?? string.Empty),
                (int)p.Key.Start.Line))]
            : [];

    /// <summary>Rule: spec props are camelCase, C# positional params are PascalCase.</summary>
    public static string PascalCase(string name) =>
        name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];

    /// <summary>
    /// The general annotation-to-C#-type mapping rules. Experiment metric: keep this list
    /// SMALL — every addition here is a counted general mapping rule (§9 experiment log).
    /// </summary>
    public static string MapType(string annotation)
    {
        var type = StripParenthesizedNote(annotation.Trim());
        return type.EndsWith("[]", StringComparison.Ordinal)
            ? $"IReadOnlyList<{type[..^2]}>" // rule: X[] -> IReadOnlyList<X> (constitution collections)
            : type;
    }

    /// <summary>Rule: "Direction (mer|mindre)" / "byte (0-100)" — the parenthesized note is prose.</summary>
    private static string StripParenthesizedNote(string annotation)
    {
        var open = annotation.IndexOf('(');
        return open < 0 ? annotation : annotation[..open].Trim();
    }
}
