---
status: Accepted
type: architecture
created: 2026-07-12
---

# ADR 016: Stratum-1 records are build artifacts generated from the emlang spec

## Context
Step 0 of the code-as-build-artifact experiment (`docs/analysis/code-as-build-artifact.md`
§9) proved the spec-surface determinism assumption: 6 general mapping rules + a 3-fact
per-game `GameManifest` fully determine all three games' Commands/Events/Errors record
surfaces, with zero irreducible findings. The committed files were pure transcription —
a lockfile for information whose source of truth is `specs/*-event-model.yaml`.

## Decision
The stratum-1 record layer (Commands.cs / Events.cs / Errors.cs: positional records +
the closed C# 15 union per kind) is **generated at build time and deleted from git**,
piloted on Blindbudet.

### Mechanism
- **`Emlang.CodeGen`** (netstandard2.0) stays the pure core: `SpecModel.Parse` (YamlDotNet)
  → `SurfaceEmitter.Emit` (C# text). netstandard2.0 because the dll must load inside the
  compiler process; a 3-line `IsExternalInit` shim replaces a PolySharp dependency.
- **`Emlang.Generators`** is a thin `IIncrementalGenerator` wrapper: `AdditionalFiles`
  matching `*-event-model.yaml` → `GameManifest` match by file name → emit
  `Commands.g.cs`/`Events.g.cs`/`Errors.g.cs` via `AddSource`. Output lives in the
  compilation (`obj/`), never on disk — that is the article's whole point.
- Roslyn is pinned **old-stable (4.14.0)**: CS9057 fires only when the analyzer's Roslyn
  is newer than the compiler's, so old is the safe direction under the preview SDK.
- Non-Roslyn dependencies (Emlang.CodeGen.dll, YamlDotNet.dll) ship as analyzer assets
  via the `GetDependencyTargetPaths` → `TargetPathWithTargetPlatformMoniker` cookbook
  target in Emlang.Generators.csproj.
- Consuming Domain csproj wires both:
  `<AdditionalFiles Include="..\..\specs\<game>-event-model.yaml" />` +
  `<ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`.

### The 3-fact manifest
The spec structurally cannot say: the C# namespace, the three union names, the file
paths. These live as three literal `GameManifest` instances in code — no config file.

### Correctness proof
The step-0 shadow harness became the emitter's self-test: `SurfaceEmitterTests` feeds
the emitted text back through `SurfaceComparer` against the real spec for **all three
games** — zero divergences required. A flipped game's per-game shadow test retires
(spec↔generated comparison is tautological); unflipped games (MEM, TankTillTusen) keep
their shadow tests guarding the still-committed files.

## Consequences
- **The spec is now load-bearing for compilation.** A broken or renamed
  `blindbudet-event-model.yaml` is a build error, not a stale-test warning. The spec is
  officially the cheapest (and only) place to change the record surface.
- **166 LOC left git** for Blindbudet (+29 LOC retired shadow test); MEM (~225) and
  TankTillTusen (~164) follow the same flip when desired.
- Preview-SDK/IDE coupling: the generator emits C# 15 `union` text parsed with the
  consumer's `LangVersion=preview`; IDE squiggle glitches are possible and cosmetic.
- Generated output is invisible to the CodeHealth gate by construction (`\.g\.cs$` +
  `/obj/` exclusions), and reflection-based architecture tests validate generated types
  for free (they inspect the compiled assembly).
- Step-3 GWT generation must use **sidecar fixture files** (`specs/<game>-fixtures.yaml`)
  — `emlang lint` v1.0.0 hard-rejects a `fixtures:` key in the spec (step-0 Probe A).
