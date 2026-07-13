using System;
using System.IO;
using System.Linq;
using Emlang.CodeGen;
using Microsoft.CodeAnalysis;

namespace Emlang.Generators;

/// <summary>
/// Shared plumbing for both generators: the spec provider and the EmlangEmit gate
/// (build_property.EmlangEmit; absent = "surface"). Domain projects emit records,
/// *.Domain.Tests projects set EmlangEmit=tests and get SpecTests instead (ADR 017).
/// </summary>
internal static class EmlangEmit
{
    internal static IncrementalValuesProvider<(string FileName, string Yaml, string Mode)> Specs(
        IncrementalGeneratorInitializationContext context)
    {
        var mode = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            options.GlobalOptions.TryGetValue("build_property.EmlangEmit", out var value) && value.Length > 0
                ? value
                : "surface");
        return context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith("-event-model.yaml", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, ct) =>
                (FileName: Path.GetFileName(text.Path), Yaml: text.GetText(ct)?.ToString() ?? string.Empty))
            .Combine(mode)
            .Select(static (pair, _) => (pair.Left.FileName, pair.Left.Yaml, Mode: pair.Right));
    }

    internal static GameManifest? Manifest(string fileName) =>
        GameManifest.All.FirstOrDefault(m => Path.GetFileName(m.SpecPath) == fileName);
}

/// <summary>
/// Thin analyzer wrapper over Emlang.CodeGen (ADR 016): each AdditionalFiles spec that
/// matches a GameManifest gets its stratum-1 Commands/Events/Errors emitted straight
/// into the compilation (obj/, never disk). Correctness lives in SurfaceEmitterTests'
/// comparer round-trip — this class only routes text.
/// </summary>
[Generator]
public sealed class EmlangRecordsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(EmlangEmit.Specs(context), static (production, spec) =>
        {
            if (spec.Mode != "surface" || EmlangEmit.Manifest(spec.FileName) is not { } manifest)
                return;
            var elements = SpecModel.Parse(spec.Yaml);
            production.AddSource("Commands.g.cs", SurfaceEmitter.Emit(manifest, elements, 'c'));
            production.AddSource("Events.g.cs", SurfaceEmitter.Emit(manifest, elements, 'e'));
            production.AddSource("Errors.g.cs", SurfaceEmitter.Emit(manifest, elements, 'x'));
        });
    }
}
