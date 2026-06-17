#:project ../src/MerEllerMindre.Domain/MerEllerMindre.Domain.csproj

// Question-pack tooling. Run from repo root:
//   dotnet run tools/pack.cs                         report on the live pack
//   dotnet run tools/pack.cs -- report --staging     report over staging candidates
//   dotnet run tools/pack.cs -- merge --out <path>    dedup+write a candidate pack
//   dotnet run tools/pack.cs -- cap --max N --out <kept> --park <wip>
//                                                     cap item frequency, park overflow
// Reuses the Domain's parser + Decider.NormalizeDifference so band math has ONE source.

using System.Globalization;
using System.Text;
using MerEllerMindre.Domain;

const string LivePack = "src/MerEllerMindre.Domain/data/packs/mer-eller-mindre.csv";
const string StagingDir = "question-staging";

// Style-guide band thresholds (specs/question-style-guide.md): the only numbers that
// live here; the normalization itself comes from Decider.NormalizeDifference.
int[] thresholds = [20, 60, 85];
string[] bandLabels = ["0-20", "21-60", "61-85", "86-100"];
int[] targets = [15, 40, 30, 15];
const int ItemCap = 4; // report flags items appearing more than this in the live pack

var argv = new List<string>(args);
var command = argv.Count > 0 && !argv[0].StartsWith('-') ? argv[0] : "report";
if (argv.Count > 0 && !argv[0].StartsWith('-')) argv.RemoveAt(0);

bool useStaging = argv.Remove("--staging");
bool force = argv.Remove("--force");
string? outPath = TakeOption("--out");
string? parkPath = TakeOption("--park");
string? maxOpt = TakeOption("--max");
var positional = argv.Where(a => !a.StartsWith('-')).ToList();

switch (command)
{
    case "report":
        Report(LoadCards(positional.Count > 0 ? positional : DefaultSources(useStaging)));
        break;
    case "merge":
        Merge();
        break;
    case "cap":
        Cap();
        break;
    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Use 'report', 'merge' or 'cap'.");
        Environment.Exit(2);
        break;
}

string? TakeOption(string name)
{
    var i = argv.IndexOf(name);
    if (i < 0) return null;
    if (i + 1 >= argv.Count) { Console.Error.WriteLine($"{name} needs a value."); Environment.Exit(2); }
    var value = argv[i + 1];
    argv.RemoveRange(i, 2);
    return value;
}

string[] DefaultSources(bool staging) =>
    staging ? StagingFiles() : [LivePack];

string[] StagingFiles() =>
    Directory.GetFiles(StagingDir, "*.csv")
        .Where(f =>
        {
            var name = Path.GetFileName(f);
            return !name.StartsWith('_')
                && !name.StartsWith("tmp_")
                && !name.EndsWith(".tmp.csv")
                && !name.EndsWith(".källor.csv");
        })
        .OrderBy(f => f)
        .ToArray();

List<Question> LoadCards(IEnumerable<string> files)
{
    var cards = new List<Question>();
    foreach (var file in files)
    {
        var slug = Path.GetFileNameWithoutExtension(file);
        var pack = QuestionPackCsvParser.Parse(slug, File.ReadAllText(file)); // fail-fast names the file via slug
        cards.AddRange(pack.Questions);
    }
    return cards;
}

int Band(Question q)
{
    var norm = Decider.NormalizeDifference(Math.Abs(q.ValueA - q.ValueB), Math.Max(q.ValueA, q.ValueB));
    if (norm <= thresholds[0]) return 0;
    if (norm <= thresholds[1]) return 1;
    if (norm <= thresholds[2]) return 2;
    return 3;
}

void Report(List<Question> cards)
{
    if (cards.Count == 0) { Console.WriteLine("No cards."); return; }

    var counts = new int[4];
    foreach (var c in cards) counts[Band(c)]++;

    Console.WriteLine($"Cards: {cards.Count}");
    Console.WriteLine();
    Console.WriteLine("Band       count   actual   target");
    for (var b = 0; b < 4; b++)
    {
        var pct = 100.0 * counts[b] / cards.Count;
        Console.WriteLine($"{bandLabels[b],-9}  {counts[b],5}   {pct,5:0.0}%   {targets[b],4}%");
    }

    var mer = cards.Count(c => c.ValueA >= c.ValueB);
    Console.WriteLine();
    Console.WriteLine($"Direction: Mer {mer} ({100.0 * mer / cards.Count:0.0}%)  Mindre {cards.Count - mer} ({100.0 * (cards.Count - mer) / cards.Count:0.0}%)");

    Console.WriteLine();
    Console.WriteLine("Top units:");
    foreach (var g in cards.GroupBy(c => c.Unit).OrderByDescending(g => g.Count()).Take(10))
        Console.WriteLine($"  {g.Count(),5}  {g.Key}");

    var items = cards
        .SelectMany(c => new[] { c.ItemA, c.ItemB })
        .GroupBy(x => x)
        .OrderByDescending(g => g.Count())
        .ThenBy(g => g.Key)
        .ToList();
    Console.WriteLine();
    Console.WriteLine("Top items (sakA + sakB):");
    foreach (var g in items.Take(25))
        Console.WriteLine($"  {g.Count(),5}  {g.Key}");
    var over = items.Where(g => g.Count() > ItemCap).ToList();
    Console.WriteLine($"Items over cap {ItemCap}: {over.Count}" +
        (over.Count == 0 ? "" : $"   ({string.Join(", ", over.Select(g => g.Key))})"));

    var dups = cards
        .GroupBy(c => c.QuestionText.Trim(), StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() > 1)
        .ToList();
    Console.WriteLine();
    if (dups.Count == 0)
        Console.WriteLine("No duplicate questionText.");
    else
    {
        Console.WriteLine($"DUPLICATE questionText ({dups.Count}):");
        foreach (var g in dups)
            Console.WriteLine($"  x{g.Count()}  {g.Key}");
    }
}

void Merge()
{
    if (outPath is null) { Console.Error.WriteLine("merge requires --out <path>."); Environment.Exit(2); return; }

    // ponytail: --force guard, names the triple-size clobber ceiling.
    // Blindly merging all ~888 staging candidates into the live pack would triple its
    // content and break the curated count; refuse unless explicitly forced.
    if (Path.GetFullPath(outPath) == Path.GetFullPath(LivePack) && !force)
    {
        Console.Error.WriteLine($"Refusing to overwrite the live pack '{LivePack}'. Pass --force if you really mean it.");
        Environment.Exit(2);
        return;
    }

    var cards = LoadCards(StagingFiles());
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var kept = new List<Question>();
    var dropped = 0;
    foreach (var c in cards)
        if (seen.Add(c.QuestionText.Trim())) kept.Add(c);
        else dropped++;

    WritePack(kept, outPath);
    Console.WriteLine($"Wrote {kept.Count} cards to {outPath} (dropped {dropped} duplicate questionText).");
    Console.WriteLine();
    Report(kept);
}

void Cap()
{
    if (maxOpt is null || !int.TryParse(maxOpt, out var max) || max < 1)
    { Console.Error.WriteLine("cap requires --max <N> (N >= 1)."); Environment.Exit(2); return; }
    if (outPath is null) { Console.Error.WriteLine("cap requires --out <keptPath>."); Environment.Exit(2); return; }
    if (parkPath is null) { Console.Error.WriteLine("cap requires --park <wipPath>."); Environment.Exit(2); return; }

    if ((Path.GetFullPath(outPath) == Path.GetFullPath(LivePack)
        || Path.GetFullPath(parkPath) == Path.GetFullPath(LivePack)) && !force)
    {
        Console.Error.WriteLine($"Refusing to overwrite the live pack '{LivePack}'. Pass --force if you really mean it.");
        Environment.Exit(2);
        return;
    }

    var cards = LoadCards([LivePack]);
    var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    var kept = new List<Question>();
    var parked = new List<Question>();
    foreach (var c in cards) // stable order
    {
        seen.TryGetValue(c.ItemA, out var a);
        seen.TryGetValue(c.ItemB, out var b);
        if (a < max && b < max)
        {
            seen[c.ItemA] = a + 1;
            seen[c.ItemB] = b + 1;
            kept.Add(c);
        }
        else parked.Add(c);
    }

    WritePack(kept, outPath);
    WritePack(parked, parkPath);
    Console.WriteLine($"Capped at {max}/item: kept {kept.Count} to {outPath}, parked {parked.Count} to {parkPath}.");
    Console.WriteLine();
    Report(kept);
}

void WritePack(List<Question> cards, string path)
{
    var sb = new StringBuilder();
    sb.Append('\uFEFF'); // BOM so Excel reads sv-SE UTF-8
    sb.Append("fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga\n");
    var sv = CultureInfo.GetCultureInfo("sv-SE");
    foreach (var c in cards)
        sb.Append(string.Join(';',
            Q(c.QuestionText), Q(c.ItemA), Q(c.ItemB),
            Q(c.ValueA.ToString(sv)), Q(c.ValueB.ToString(sv)),
            Q(c.Unit), Q(c.DifferencePrompt))).Append('\n');
    File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
}

static string Q(string field) =>
    field.Contains(';') || field.Contains('"') || field.Contains('\n') || field.Contains('\r')
        ? "\"" + field.Replace("\"", "\"\"") + "\""
        : field;
