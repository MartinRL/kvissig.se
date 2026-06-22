namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Maps a logo company name (exactly as written in a question pack) to the URL of its PNG,
/// served under /logos. Built once at startup from data/logos/logos.csv, keeping ONLY rows
/// whose PNG is actually on disk — so a reference to a not-yet-downloaded logo resolves to
/// null (no broken &lt;img&gt;) instead of a 404.
/// </summary>
public sealed class LogoCatalog
{
    private readonly Dictionary<string, string> _urlByName = new(StringComparer.Ordinal);

    public LogoCatalog(string logosDirectory)
    {
        var csv = Path.Combine(logosDirectory, "logos.csv");
        if (!File.Exists(csv))
            return;

        // PNGs on disk, keyed "{origin}/{slug}.png" so an un-downloaded logo filters its row out.
        var onDisk = Directory.EnumerateFiles(logosDirectory, "*.png", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(logosDirectory, p).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        // ponytail: plain comma split — logos.csv is slug,name,domain,origin and seed names
        // contain no commas. Upgrade to an RFC4180 reader only if a name ever needs a comma.
        foreach (var line in File.ReadLines(csv).Skip(1))
        {
            var cols = line.Split(',');
            if (cols.Length < 4)
                continue;
            var (slug, name, origin) = (cols[0].Trim(), cols[1].Trim(), cols[3].Trim());
            if (onDisk.Contains($"{origin}/{slug}.png"))
                _urlByName[name] = $"/logos/{origin}/{slug}.png";
        }
    }

    public string? UrlFor(string name) =>
        _urlByName.TryGetValue(name, out var url) ? url : null;
}
