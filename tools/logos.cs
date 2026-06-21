// tools/logos.cs — logo-corpus tooling. Run from repo root:
//   dotnet run tools/logos.cs -- gen                 slug+dedup tools/logos-seed.csv -> data/logos/logos.csv
//   dotnet run tools/logos.cs -- fetch [--limit N]   download missing logos from Logo.dev; rewrite failures.csv
//   dotnet run tools/logos.cs -- status              count files on disk vs. the 2000/700-se targets
// Idempotent: fetch skips slugs already on disk, so it is safe to re-run (this drives the ralph loop).
// No #:project — slug logic is self-contained here (nothing in the Domain shares it yet, YAGNI).

using System.Globalization;
using System.Net.Http;
using System.Text;

const string LogosDir = "src/MerEllerMindre.Domain/data/logos";
const string Seed = "tools/logos-seed.csv";
const int SeTarget = 700;
const int TotalTarget = 2000;
var Master = Path.Combine(LogosDir, "logos.csv");
var Failures = Path.Combine(LogosDir, "failures.csv");
var token = Environment.GetEnvironmentVariable("LOGODEV_TOKEN");

var argv = new List<string>(args);
var command = argv.Count > 0 && !argv[0].StartsWith('-') ? argv[0] : "status";
var limit = 0;
var li = argv.IndexOf("--limit");
if (li >= 0 && li + 1 < argv.Count) int.TryParse(argv[li + 1], out limit);

switch (command)
{
    case "gen": Gen(); break;
    case "fetch": await Fetch(); break;
    case "status": Status(); break;
    case "demo": Demo(); Console.WriteLine("slug demo ok"); break;
    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Use gen | fetch | status.");
        Environment.Exit(2);
        break;
}

void Gen()
{
    if (!File.Exists(Seed)) { Console.Error.WriteLine($"Missing seed {Seed}."); Environment.Exit(2); return; }
    var seen = new HashSet<string>();
    var rows = new List<(string slug, string name, string domain, string origin)>();
    foreach (var raw in File.ReadAllLines(Seed))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;
        var p = line.Split(';');
        if (p.Length < 3) { Console.Error.WriteLine($"skip (need name;domain;origin): {line}"); continue; }
        var name = p[0].Replace(',', ' ').Replace('"', ' ').Trim();
        while (name.Contains("  ")) name = name.Replace("  ", " ");
        var domain = p[1].Trim().ToLowerInvariant();
        var origin = p[2].Trim().ToLowerInvariant();
        if (origin != "se" && origin != "int") { Console.Error.WriteLine($"skip (origin must be se|int): {line}"); continue; }
        var slug = Slug(name);
        if (slug.Length == 0 || domain.Length == 0) { Console.Error.WriteLine($"skip (empty slug/domain): {line}"); continue; }
        if (!seen.Add(slug)) continue; // dedup on slug
        rows.Add((slug, name, domain, origin));
    }
    var sb = new StringBuilder("slug,name,domain,origin\n");
    foreach (var r in rows) sb.Append($"{r.slug},{r.name},{r.domain},{r.origin}\n");
    Directory.CreateDirectory(LogosDir);
    File.WriteAllText(Master, sb.ToString(), new UTF8Encoding(false));
    var se = rows.Count(r => r.origin == "se");
    Console.WriteLine($"Wrote {rows.Count} rows to {Master} (se {se}, int {rows.Count - se}).");
}

async Task Fetch()
{
    if (!File.Exists(Master)) { Console.Error.WriteLine($"Run gen first ({Master} missing)."); Environment.Exit(2); return; }
    if (string.IsNullOrWhiteSpace(token))
    { Console.Error.WriteLine("LOGODEV_TOKEN not set. Set it, or rescue misses via the ralph loop."); Environment.Exit(2); return; }

    var rows = ReadMaster();
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("kvissig-logos/1.0");
    int attempted = 0, ok = 0, skipped = 0;
    var failedSlugs = new HashSet<string>();
    foreach (var r in rows)
    {
        if (limit > 0 && attempted >= limit) break;
        var dir = Path.Combine(LogosDir, r.origin);
        Directory.CreateDirectory(dir);
        if (Existing(dir, r.slug) is not null) { skipped++; continue; }
        attempted++;
        var bytes = await TryDownload(http, r.domain);
        if (bytes is not null && IsValidPng(bytes))
        {
            await File.WriteAllBytesAsync(Path.Combine(dir, r.slug + ".png"), bytes);
            ok++;
        }
        else
        {
            failedSlugs.Add(r.slug);
            Console.Error.WriteLine($"FAIL {r.origin}/{r.slug} ({r.domain})");
        }
    }
    WriteFailures(rows, failedSlugs);
    var failed = failedSlugs.Count;
    Console.WriteLine($"Fetch: {ok} ok, {failed} failed, {skipped} already on disk.");
    Status();
}

async Task<byte[]?> TryDownload(HttpClient http, string domain)
{
    try
    {
        var url = $"https://img.logo.dev/{domain}?token={token}&format=png&size=256&retina=true";
        using var resp = await http.GetAsync(url);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsByteArrayAsync() : null;
    }
    catch { return null; }
}

// failures.csv = rows that were ATTEMPTED at least once and still lack a file: prior
// failures ∪ this run's misses, minus anything now on disk. Un-attempted rows (skipped by
// --limit) are NOT failures, so they stay out.
void WriteFailures(List<(string slug, string name, string domain, string origin)> rows, HashSet<string> newFails)
{
    var attempted = new HashSet<string>(newFails);
    if (File.Exists(Failures))
        foreach (var l in File.ReadAllLines(Failures).Skip(1))
        {
            var s = l.Split(',')[0].Trim();
            if (s.Length > 0) attempted.Add(s);
        }
    var keep = rows.Where(r => attempted.Contains(r.slug)
        && Existing(Path.Combine(LogosDir, r.origin), r.slug) is null).ToList();
    var sb = new StringBuilder("slug,name,domain,origin\n");
    foreach (var r in keep) sb.Append($"{r.slug},{r.name},{r.domain},{r.origin}\n");
    File.WriteAllText(Failures, sb.ToString(), new UTF8Encoding(false));
}

void Status()
{
    var se = CountLogos("se");
    var @int = CountLogos("int");
    var total = se + @int;
    Console.WriteLine($"On disk: se {se}/{SeTarget}, int {@int}, total {total}/{TotalTarget}.");
    Console.WriteLine(total >= TotalTarget && se >= SeTarget
        ? "DONE — targets met."
        : $"Remaining: {Math.Max(0, SeTarget - se)} se, {Math.Max(0, TotalTarget - total)} total.");
}

int CountLogos(string origin)
{
    var dir = Path.Combine(LogosDir, origin);
    return Directory.Exists(dir)
        ? Directory.GetFiles(dir).Count(f => !f.EndsWith(".gitkeep", StringComparison.Ordinal))
        : 0;
}

List<(string slug, string name, string domain, string origin)> ReadMaster() =>
    File.ReadAllLines(Master)
        .Skip(1)
        .Where(l => l.Trim().Length > 0)
        .Select(l => l.Split(','))
        .Where(p => p.Length >= 4)
        .Select(p => (p[0].Trim(), p[1].Trim(), p[2].Trim(), p[3].Trim()))
        .ToList();

static string? Existing(string dir, string slug) =>
    Directory.Exists(dir) ? Directory.GetFiles(dir, slug + ".*").FirstOrDefault() : null;

// ponytail: magic-bytes + min-size only (512). Monogram-placeholder detection is left to
// the agent's visual check in the ralph loop, not guessed here.
static bool IsValidPng(byte[] b) =>
    b.Length > 512 && b.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

// Clean ASCII kebab slug: NFD-decompose + strip combining marks (å/ä/à→a, ö→o, é→e, ü→u),
// explicit map for non-decomposing letters (ø/æ/ð/þ/ß), everything non-[a-z0-9] -> '-', collapse, trim.
static string Slug(string name)
{
    var pre = name.ToLowerInvariant()
        .Replace("ø", "o").Replace("æ", "ae").Replace("œ", "oe")
        .Replace("ð", "d").Replace("þ", "th").Replace("ß", "ss");
    var sb = new StringBuilder();
    foreach (var ch in pre.Normalize(NormalizationForm.FormD))
    {
        if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
        sb.Append(ch < 128 && char.IsLetterOrDigit(ch) ? ch : '-');
    }
    var slug = sb.ToString();
    while (slug.Contains("--")) slug = slug.Replace("--", "-");
    return slug.Trim('-');
}

// ponytail: one runnable check on the slug path (the one piece of non-trivial logic).
// Run with `dotnet run tools/logos.cs -- demo`.
static void Demo()
{
    void Eq(string got, string want)
    { if (got != want) throw new Exception($"slug expected '{want}', got '{got}'"); }
    Eq(Slug("Hennes & Mauritz"), "hennes-mauritz");
    Eq(Slug("Löfbergs"), "lofbergs");
    Eq(Slug("Systembolaget"), "systembolaget");
    Eq(Slug("Coca-Cola"), "coca-cola");
    Eq(Slug("Føtex"), "fotex");
    Eq(Slug("L'Oréal"), "l-oreal");
}
