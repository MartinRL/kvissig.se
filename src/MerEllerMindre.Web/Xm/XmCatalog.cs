using Xmlang;

namespace MerEllerMindre.Web.Xm;

/// <summary>
/// Loads the load-bearing xm spec pair (Experience Model + its Event Model) from
/// AppContext.BaseDirectory\specs at startup and lints it, throwing on ANY error — the
/// FileSystemQuestionPackCatalog fail-fast philosophy: a broken spec must kill the deploy,
/// never degrade a rendered screen. Spec + interpreter always deploy atomically (the spec
/// never reaches the browser), which deletes SDUI's version-skew failure class.
/// </summary>
public sealed class XmCatalog
{
    public XmSpec Blindbudet { get; }
    public EmSpec BlindbudetModel { get; }

    public XmCatalog(string specsDirectory) =>
        (Blindbudet, BlindbudetModel) = Load(Path.Combine(specsDirectory, "blindbudet.xm.yaml"));

    private static (XmSpec Xm, EmSpec Em) Load(string xmPath)
    {
        var xm = XmParser.Parse(File.ReadAllText(xmPath));
        var dir = Path.GetDirectoryName(xmPath)!;
        var em = EmModel.Merge([.. xm.Models.Select(m => EmModel.Parse(File.ReadAllText(Path.Combine(dir, m))))]);

        var errors = XmLinter.Lint(xm, em).Where(f => f.Severity == XmSeverity.Error).ToList();
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"{Path.GetFileName(xmPath)} fails xm lint: "
                + string.Join("; ", errors.Select(e => $"{e.Rule}: {e.Message}")));
        return (xm, em);
    }
}
