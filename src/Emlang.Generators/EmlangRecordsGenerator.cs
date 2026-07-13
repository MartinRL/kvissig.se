using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emlang.CodeGen;
using Microsoft.CodeAnalysis;

namespace Emlang.Generators;

/// <summary>
/// Thin analyzer wrapper over Emlang.CodeGen (ADR 016): each AdditionalFiles spec that
/// matches a GameManifest gets its stratum-1 Commands/Events/Errors emitted straight
/// into the compilation (obj/, never disk). Correctness lives in SurfaceEmitterTests'
/// comparer round-trip — this class only routes text.
/// </summary>
[Generator]
public sealed class EmlangRecordsGenerator : IIncrementalGenerator
{
    private static readonly IReadOnlyList<GameManifest> Manifests =
    [
        GameManifest.MerEllerMindre,
        GameManifest.Blindbudet,
        GameManifest.TankTillTusen,
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var specs = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith("-event-model.yaml", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, ct) =>
                (FileName: Path.GetFileName(text.Path), Yaml: text.GetText(ct)?.ToString() ?? string.Empty));

        context.RegisterSourceOutput(specs, static (production, spec) =>
        {
            var manifest = Manifests.FirstOrDefault(m => Path.GetFileName(m.SpecPath) == spec.FileName);
            if (manifest is null)
                return;
            var elements = SpecModel.Parse(spec.Yaml);
            production.AddSource("Commands.g.cs", SurfaceEmitter.Emit(manifest, elements, 'c'));
            production.AddSource("Events.g.cs", SurfaceEmitter.Emit(manifest, elements, 'e'));
            production.AddSource("Errors.g.cs", SurfaceEmitter.Emit(manifest, elements, 'x'));
        });
    }
}
