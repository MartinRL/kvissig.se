using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Fitness functions for the Tänk Till Tusen sister-domain — the mechanically-checkable subset
/// of the constitution/ADRs, plus the spec↔domain contract binding
/// specs/tank-till-tusen-event-model.yaml to the tank unions. Mirror of MEM's / BlindBudet's
/// ArchitectureTests (scoped to the TankTillTusen.Domain assembly + its spec).
/// </summary>
public class TankArchitectureTests
{
    private static readonly Assembly Domain = typeof(Decider).Assembly;

    // C# 15 union types: their runtime shape is not a record, so they're excluded.
    private static readonly string[] UnionTypeNames = ["TankCommand", "TankEvent", "TankError", "Result`1"];

    [Fact]
    public void All_public_domain_types_are_records()
    {
        var offenders = Domain.GetExportedTypes()
            .Where(t => !t.IsEnum)
            .Where(t => !(t.IsAbstract && t.IsSealed)) // static classes
            .Where(t => !t.Name.Contains('<'))         // compiler-generated
            .Where(t => !UnionTypeNames.Contains(t.Name))
            .Where(t => !IsRecord(t))
            .Select(t => t.Name)
            .ToList();

        offenders.Should().BeEmpty("all public domain types must be records (constitution)");
    }

    [Fact]
    public void Public_collection_members_are_readonly()
    {
        var offenders = (
            from t in Domain.GetExportedTypes()
            from p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            where IsCollection(p.PropertyType) && !IsReadOnlyCollection(p.PropertyType)
            select $"{t.Name}.{p.Name} : {p.PropertyType.Name}"
        ).ToList();

        offenders.Should().BeEmpty(
            "public collection members must be IReadOnlyList<>/IReadOnlyDictionary<> (constitution)");
    }

    [Fact]
    public void Decider_is_total_and_synchronous()
    {
        var decider = Read("src/TankTillTusen.Domain/Decider.cs");

        decider.Should().NotContain("throw ", "the functional core is total — failures are Result values");
        decider.Should().NotContain("async ", "the functional core is synchronous");
        decider.Should().NotContain("await ", "the functional core is synchronous");
    }

    [Fact]
    public void No_reflection_or_dynamic_in_domain()
    {
        string[] banned = ["dynamic ", "typeof(", ".GetType(", "System.Reflection"];

        var offenders = (
            from file in SourceFiles("src/TankTillTusen.Domain", "*.cs")
            let text = File.ReadAllText(file)
            from bad in banned
            where text.Contains(bad, StringComparison.Ordinal)
            select $"{Path.GetFileName(file)} uses {bad.Trim()}"
        ).ToList();

        offenders.Should().BeEmpty("the domain must not use reflection or dynamic");
    }

    [Fact]
    public void The_forbidden_price_word_appears_nowhere_in_src()
    {
        // ponytail: the banned word is assembled from fragments so THIS file never contains it literally.
        var banned = "grat" + "is";

        var offenders = (
            from file in SourceFiles("src", "*.cs")
                .Concat(SourceFiles("src", "*.razor"))
                .Concat(SourceFiles("src", "*.cshtml"))
                .Concat(SourceFiles("src", "*.csv"))
            where File.ReadAllText(file).Contains(banned, StringComparison.OrdinalIgnoreCase)
            select Path.GetFileName(file)
        ).ToList();

        offenders.Should().BeEmpty("the forbidden price word must never appear (hard rule) — the game may be monetised");
    }

    // --- emlang spec-coverage cross-check (both directions) -----------------------------

    [Fact]
    public void Every_spec_element_has_a_code_type()
    {
        var missing = SpecElementNames()
            .Where(name => Domain.GetType("TankTillTusen.Domain." + name) is null)
            .ToList();

        missing.Should().BeEmpty("every c:/e:/x: in the spec must resolve to a Domain type");
    }

    [Fact]
    public void Every_union_case_appears_in_the_spec()
    {
        var spec = SpecElementNames();

        var cases = UnionMembers(Read("src/TankTillTusen.Domain/Commands.cs"), "TankCommand")
            .Concat(UnionMembers(Read("src/TankTillTusen.Domain/Events.cs"), "TankEvent"))
            .Concat(UnionMembers(Read("src/TankTillTusen.Domain/Errors.cs"), "TankError"));

        var orphans = cases.Where(c => !spec.Contains(c)).ToList();

        orphans.Should().BeEmpty("every union case must appear as a c:/e:/x: in the spec");
    }

    // --- helpers ------------------------------------------------------------------------

    private static bool IsRecord(Type t) =>
        t.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null
        || t.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static bool IsCollection(Type t) =>
        t != typeof(string)
        && (t.IsArray
            || (t.IsGenericType
                && t.GetInterfaces().Append(t).Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))));

    private static bool IsReadOnlyCollection(Type t) =>
        t.IsGenericType
        && (t.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
            || t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

    private static IReadOnlyList<string> UnionMembers(string source, string unionName)
    {
        var match = Regex.Match(source, $@"union\s+{unionName}\s*\((?<body>[^)]*)\)", RegexOptions.Singleline);
        match.Success.Should().BeTrue($"union {unionName}(...) must exist in source");
        return match.Groups["body"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static readonly Regex SpecElementPattern =
        new(@"^\s*-?\s*(?<kind>[cex]):\s*(?<value>\S.*?)\s*$", RegexOptions.Multiline);

    private static HashSet<string> SpecElementNames()
    {
        var yaml = Read("specs/tank-till-tusen-event-model.yaml");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in SpecElementPattern.Matches(yaml))
        {
            var value = m.Groups["value"].Value;
            var comment = value.IndexOf('#'); // strip inline "# ..." comments
            if (comment >= 0)
                value = value[..comment];
            value = value.Trim();
            // Events carry the "Game / " stream prefix; commands/exceptions are bare.
            var name = value.Contains('/') ? value[(value.LastIndexOf('/') + 1)..].Trim() : value;
            names.Add(name);
        }
        return names;
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot, relativePath));

    private static IEnumerable<string> SourceFiles(string relativeDir, string searchPattern) =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot, relativeDir), searchPattern, SearchOption.AllDirectories)
            .Where(p =>
            {
                var norm = p.Replace('\\', '/');
                return !norm.Contains("/bin/") && !norm.Contains("/obj/");
            });

    private static readonly string RepoRoot = LocateRepoRoot();

    private static string LocateRepoRoot([CallerFilePath] string thisFile = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(thisFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
            dir = dir.Parent;
        dir.Should().NotBeNull("repo root (the dir with Directory.Build.props) must be found from the test file");
        return dir!.FullName;
    }
}
