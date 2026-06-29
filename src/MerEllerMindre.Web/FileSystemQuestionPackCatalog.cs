using System.Text;
using MerEllerMindre.Domain;

namespace MerEllerMindre.Web;

/// <summary>
/// Imperative-shell catalog: reads every data/packs/*.csv from disk at startup and
/// parses each into a QuestionPack via the pure QuestionPackCsvParser. The filename
/// (without extension) is the pack slug. Fails fast on a broken CSV so a deploy never
/// silently serves a malformed deck.
/// </summary>
public sealed class FileSystemQuestionPackCatalog
{
    private readonly Dictionary<string, QuestionPack> _bySlug;

    public FileSystemQuestionPackCatalog(string packsDirectory)
    {
        if (!Directory.Exists(packsDirectory))
            throw new DirectoryNotFoundException($"Question pack directory not found: {packsDirectory}");

        _bySlug = new Dictionary<string, QuestionPack>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(packsDirectory, "*.csv").OrderBy(p => p, StringComparer.Ordinal))
        {
            var slug = Path.GetFileNameWithoutExtension(path);
            var text = File.ReadAllText(path, Encoding.UTF8);
            _bySlug[slug] = QuestionPackCsvParser.Parse(slug, text);
        }

        // The featured "Mer eller Mindre" (alla-aldrar) pack pins to the top; the rest stay alphabetical.
        Packs = _bySlug.Values
            .OrderBy(p => p.PackId != "alla-aldrar")
            .ThenBy(p => p.PackId, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<QuestionPack> Packs { get; }

    public QuestionPack? Find(string slug) =>
        _bySlug.TryGetValue(slug, out var pack) ? pack : null;
}
