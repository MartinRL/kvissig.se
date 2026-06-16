using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Fitness functions: the mechanically-checkable subset of CLAUDE.md / the constitution /
/// the ADRs, enforced as ordinary xUnit tests so violations turn the suite red (and the
/// Stop hook in .claude/hooks/arch-fitness.sh feeds them back). Reflection over the Domain
/// assembly + a few source/.csproj/YAML text scans — no new project, no new NuGet.
///
/// ponytail: deliberately NO "exhaustive switch / no `_ =>`" test. The compiler already
/// enforces union exhaustiveness, and QuestionSelection.BandOf legitimately uses `_ =>` on
/// a byte switch — a grep would false-positive. (See plan.)
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(Decider).Assembly;

    // C# 15 union types: their runtime shape is not a record, so they're excluded from the
    // records check. Verified against the current Domain assembly on implementation.
    private static readonly string[] UnionTypeNames = ["GameCommand", "GameEvent", "GameError", "Result`1"];

    // --- reflection over the Domain assembly -------------------------------------------

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

    // --- source / .csproj text scans ----------------------------------------------------

    [Fact]
    public void Domain_project_has_no_dependencies()
    {
        var csproj = Read("src/MerEllerMindre.Domain/MerEllerMindre.Domain.csproj");

        csproj.Should().NotContain("<PackageReference", "the functional core has no NuGet deps");
        csproj.Should().NotContain("<ProjectReference", "the functional core depends on nothing");
    }

    [Fact]
    public void No_forbidden_packages_anywhere()
    {
        string[] forbidden = ["SignalR", "EntityFrameworkCore", "Dapper", "SqlClient", "Npgsql"];

        var offenders = (
            from file in SourceFiles("src", "*.csproj")
            let text = File.ReadAllText(file)
            from bad in forbidden
            where text.Contains(bad, StringComparison.OrdinalIgnoreCase)
            select $"{Path.GetFileName(file)} references {bad}"
        ).ToList();

        offenders.Should().BeEmpty("forbidden infra packages must not appear in any .csproj");
    }

    [Fact]
    public void Decider_is_total_and_synchronous()
    {
        var decider = Read("src/MerEllerMindre.Domain/Decider.cs");

        // "throw " (with trailing space) so the "never thrown exceptions" doc comment is ignored.
        decider.Should().NotContain("throw ", "the functional core is total — failures are Result values");
        decider.Should().NotContain("async ", "the functional core is synchronous");
        decider.Should().NotContain("await ", "the functional core is synchronous");
    }

    [Fact]
    public void No_reflection_or_dynamic_in_domain()
    {
        string[] banned = ["dynamic ", "typeof(", ".GetType(", "System.Reflection"];

        var offenders = (
            from file in SourceFiles("src/MerEllerMindre.Domain", "*.cs")
            let text = File.ReadAllText(file)
            from bad in banned
            where text.Contains(bad, StringComparison.Ordinal)
            select $"{Path.GetFileName(file)} uses {bad.Trim()}"
        ).ToList();

        offenders.Should().BeEmpty("the domain must not use reflection or dynamic");
    }

    [Fact]
    public void Razor_components_are_static_ssr()
    {
        var razorOffenders = (
            from file in SourceFiles("src/MerEllerMindre.Web", "*.razor")
            let text = File.ReadAllText(file)
            where text.Contains("@rendermode") || Regex.IsMatch(text, @"@on[a-z]+") || text.Contains("<EditForm")
            select Path.GetFileName(file)
        ).ToList();

        razorOffenders.Should().BeEmpty("Razor components must be static SSR — no interactivity (ADR 007)");

        // The Blazor interactive client bundle must not be referenced from any markup.
        // ponytail: markup only (.razor/.cshtml/.html) — a .cs doc comment in Program.cs
        // literally says "no blazor.web.js" and would false-positive a blanket scan.
        var markup = SourceFiles("src/MerEllerMindre.Web", "*.razor")
            .Concat(SourceFiles("src/MerEllerMindre.Web", "*.cshtml"))
            .Concat(SourceFiles("src/MerEllerMindre.Web", "*.html"));
        var scriptOffenders = markup
            .Where(f => File.ReadAllText(f).Contains("blazor.web.js"))
            .Select(Path.GetFileName)
            .ToList();
        scriptOffenders.Should().BeEmpty("the Blazor interactive bundle must not be referenced (ADR 007)");

        Read("src/MerEllerMindre.Web/Program.cs")
            .Should().NotContain("AddInteractiveServerComponents", "no interactive render mode (ADR 007)");
    }

    // --- emlang spec-coverage cross-check (both directions) -----------------------------

    [Fact]
    public void Every_spec_element_has_a_code_type()
    {
        var missing = SpecElementNames()
            .Where(name => Domain.GetType("MerEllerMindre.Domain." + name) is null)
            .ToList();

        missing.Should().BeEmpty("every c:/e:/x: in the spec must resolve to a Domain type");
    }

    [Fact]
    public void Every_union_case_appears_in_the_spec()
    {
        var spec = SpecElementNames();

        var cases = UnionMembers(Read("src/MerEllerMindre.Domain/Commands.cs"), "GameCommand")
            .Concat(UnionMembers(Read("src/MerEllerMindre.Domain/Events.cs"), "GameEvent"))
            .Concat(UnionMembers(Read("src/MerEllerMindre.Domain/Errors.cs"), "GameError"));

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
        var yaml = Read("specs/game-flows.yaml");
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
