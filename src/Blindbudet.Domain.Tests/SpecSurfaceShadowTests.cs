using AwesomeAssertions;
using Emlang.CodeGen;
using Xunit;

namespace Blindbudet.Domain.Tests;

/// <summary>
/// Experiment 1 (code-as-build-artifact, step 0): the spec's c:/e:/x: props must
/// deterministically define the committed record surfaces (names, types, order).
/// A genuine divergence is DATA — allowlist it here with a finding ID that is logged in
/// docs/analysis/code-as-build-artifact.md §9. NEVER "fix" domain code or the spec.
/// </summary>
public class SpecSurfaceShadowTests
{
    /// <summary>Divergence.Key -> finding ID + reason. Every entry MUST have a §9 log row.</summary>
    private static readonly IReadOnlyDictionary<string, string> Findings = new Dictionary<string, string>();

    [Fact]
    public void Spec_surface_matches_committed_records()
    {
        var unclassified = SurfaceComparer.Compare(GameManifest.Blindbudet)
            .Where(d => !Findings.ContainsKey(d.Key))
            .Select(d => $"{d.Key} — {d.Detail} (spec line {d.SpecLine?.ToString() ?? "-"})")
            .ToList();

        unclassified.Should().BeEmpty(
            "every spec<->code divergence must be a mapping rule, a manifest fact, or an allowlisted finding");
    }
}
