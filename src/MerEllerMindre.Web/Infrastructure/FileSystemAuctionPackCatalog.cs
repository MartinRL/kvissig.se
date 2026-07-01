using System.Text;
using Blindbudet.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Imperative-shell catalog for Blindbudet: reads every data/auction-packs/*.csv at startup
/// and parses each into an AuctionPack via the pure AuctionPackCsvParser. The filename (minus
/// extension) is the pack slug. Fails fast on a broken CSV. Sister to MEM's
/// FileSystemQuestionPackCatalog — a separate directory so the two catalogs never mix decks.
/// </summary>
public sealed class FileSystemAuctionPackCatalog
{
    private readonly Dictionary<string, AuctionPack> _bySlug;

    public FileSystemAuctionPackCatalog(string packsDirectory)
    {
        if (!Directory.Exists(packsDirectory))
            throw new DirectoryNotFoundException($"Auction pack directory not found: {packsDirectory}");

        _bySlug = new Dictionary<string, AuctionPack>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(packsDirectory, "*.csv").OrderBy(p => p, StringComparer.Ordinal))
        {
            var slug = Path.GetFileNameWithoutExtension(path);
            _bySlug[slug] = AuctionPackCsvParser.Parse(slug, File.ReadAllText(path, Encoding.UTF8));
        }

        Packs = _bySlug.Values.OrderBy(p => p.PackId, StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<AuctionPack> Packs { get; }

    public AuctionPack? Find(string slug) =>
        _bySlug.TryGetValue(slug, out var pack) ? pack : null;
}
