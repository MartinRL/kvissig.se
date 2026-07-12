# Code as Build Artifact — analysis + roadmap for kvissig.se

> Analysis of applying the stratified thesis from
> [`code-as-build-artifact-research.md`](https://github.com/MartinRL/MartinRL.github.io/blob/main/articles/code-as-build-artifact-research.md)
> to this repository. Deliverable of this document: a verdict per layer, a vehicle
> decision, mechanism details, an emlang dialect extension design, and a step-wise
> roadmap. **No generator code exists yet** — implementation begins with step 0 and
> warrants an ADR (016 is the next free number) when it does.

## 1. Purpose and position

The research article argues a three-stratum model: where the Event Model **fully
determines** the code (records, Decider skeletons, GWT tests), code should become a
deterministic build artifact — generated into `obj/`, never committed; where the spec
only **constrains** (decide/evolve bodies, UX), agent-written code stays committed as
the lockfile; and some layers should be **interpreted**, not generated at all. It names
this repo as the empirical testbed:

> "build the minimal `Emlang.Generators` (records + Decider skeleton + GWT Facts from
> `mer-eller-mindre-event-model.yaml`), delete the corresponding hand-checked files, and
> measure what fraction of the ~3,000 lines of domain + tests evaporates from git."

Kvissig is a stronger testbed than the article assumed: there are now **three sister
games** (MerEllerMindre, Blindbudet, TankTillTusen), each hand-transcribed 1:1 from its
own emlang spec (`specs/*-event-model.yaml`). The transcription was done three times by
an agent following the same conventions — the generator amortizes at n=3, directly
answering the article's "first-project economics" risk (its objection #2).

One fact makes the thesis unusually cheap to test here: **a proto-parser already
exists**. All three `*ArchitectureTests` files carry a regex `SpecElementNames()`
(MerEllerMindre.Domain.Tests/ArchitectureTests.cs:195) that parses `c:`/`e:`/`x:` lines
out of the YAML and cross-checks spec↔union membership in both directions
(`Every_spec_element_has_a_code_type`, `Every_union_case_appears_in_the_spec`). The
spec→code contract is machine-**checked** today; it is just not machine-**generated**.
Step 0 below supersedes that regex with a real parser and loses nothing.

## 2. Stratum mapping of this repo

Measured 2026-07 (LOC = physical lines, `wc -l`, excluding `obj/`/`bin/`).

### Domain projects (3,246 LOC total)

| File | MEM | BB | TTT | Stratum | Verdict |
|---|---:|---:|---:|---|---|
| Commands.cs | 62 | 51 | 50 | 1 | **Generate** — union + records, 1:1 from `c:` props |
| Events.cs | 108 | 78 | 77 | 1 | **Generate** — union + records, 1:1 from `e:` props |
| Errors.cs | 55 | 37 | 37 | 1 | **Generate** — parameterless markers, 1:1 from `x:` |
| State.cs | 129 | 95 | 111 | 1/2 split | Records generate; **derived methods** (PendingPlayerIds, HasNextQuestion…) are logic — commit |
| Decider.cs | 555 | 314 | 293 | 1/2 split | Exhaustive `switch` skeletons generate (~410 LOC across the three); **case bodies** (scoring, selection, folding) are the residue — commit |
| Projections.cs | 202 | 138 | 137 | 1/2 split | View records generate from `v:`; projection functions are transforms — commit |
| Questions.cs / Lots.cs / Puzzles.cs | 234 | 193 | 219 | 2 | **Commit** — CSV parsers, card/lot/puzzle logic; the spec references these types but does not define their parsing |
| QuestionChecks.cs | 71 | — | — | 2 | **Commit** — pack-quality logic, not spec-determined |

Records + unions alone (Commands/Events/Errors + pure view/state records) ≈ **620 LOC**
of pure transcription. With switch skeletons (~410) the stratum-1 share of the domain is
roughly **1,030 / 3,246 ≈ 32%**; the rest is genuine Reeves residue.

### Test projects (3,780 LOC, 175 `[Fact]`s)

| File family | MEM | BB | TTT | Stratum | Verdict |
|---|---:|---:|---:|---|---|
| DeciderTests + EvolveTests + ProjectionTests (GWT) | 896 | 654 | 646 | 1 | **Generate** from `tests:` blocks — 2,196 LOC ≈ 58% of test code |
| Fixtures.cs | 125 | 177 | 193 | 2 | **Commit, human-owned** — the seam generated tests reference by identifier (§5) |
| ArchitectureTests | 234 | 190 | 189 | 2 | Commit; the two spec-coverage facts become **redundant by construction** and get deleted at step 1 |
| Parser/selection/solver tests | 360 | — | 116 | 2 | Commit — they test stratum-2 code |

The GWT estimate matches the article's "55-60% of test LOC is scaffolding/assertion
transcription". Combined stratum-1 share of domain + tests:
(1,030 + 2,196) / 7,026 ≈ **46%** by raw LOC — the article predicts 60-70% for the
layers it targets; the measured number is the falsifiable output of step 5.

### Stratum 3 (interpret, don't generate)

Small here: emlang's own `diagram`/`parse` commands already interpret the spec for
docs; the pack catalogs interpret CSV at startup. No endpoint routing or read-model
wiring is worth interpreting at this scale. Noted for completeness; no roadmap step.

### The GWT blocker is concrete, not conceptual

Generation of records and switches needs nothing new. Test generation does: fixtures
(`question0`, `hostMartin`, minted `joinCode`) live in YAML **comments**
(mer-eller-mindre-event-model.yaml:150-156); test cases reference them symbolically;
`propName: propName` means presence-only assertion; Givens are partial `v: State / Game`
props. None of that is machine-readable today. Step 0 therefore spikes a structured
`fixtures:` dialect extension (§5) before any generator is built.

## 3. Vehicle comparison

### The architecture that dissolves most of the question

All real work lives in **one pure core library**, `Emlang.CodeGen`:
YamlDotNet parse → line-tracked model → validation → C# text emit (raw strings). The
Roslyn incremental generator, the snapshot tests, and any CLI tool are **thin
wrappers** over that core. Once this is fixed, "SG vs CLI vs T4" is only a question of
where the emitted text lands, and the answer differs per target:

| Target | Vehicle | Why |
|---|---|---|
| Records/unions, Decide/Evolve skeletons, GWT `[Fact]`s, Vm records | **Roslyn incremental generator** | Output belongs in the compilation, not on disk; `obj/` output is the article's whole point; IDE sees generated types live |
| `.razor` screen scaffolds | **CLI, scaffold-once** | The Razor SDK compiles `.razor` from disk *before* consuming-project generators run — Razor is categorically not SG territory. Scaffolded once, human-owned after |
| Build-time regeneration guarantee | CI job calling the same core | No MSBuild-integrated CLI: `dotnet run tools/x.cs` inside a build is too slow/flaky; if ever needed, compiled console + `Exec` + `Inputs`/`Outputs` incremental target |

### T4: dominated, but weighed honestly

T4 is not dead — `dotnet-t4` (mono/t4, Mono.TextTemplating) runs fine on Linux CI, and
T4.BuildTools adds incremental build. But every hard part of this generator (YAML
parsing, exhaustiveness model, GWT↔fixture cross-references, diagnostics) lives in the
core library **regardless of vehicle** — so T4's contribution reduces to string
interpolation, which C# 11+ raw string literals do without a second language. T4 also
cannot: surface analyzer-grade diagnostics located in the YAML, regenerate at
design-time in the IDE, or keep output out of git (on-disk output is either committed —
defeating the goal — or wrapped in the CLI+MSBuild pattern with a worse authoring
language). IDE support in 2026 remains poor. **Verdict: rejected**, not for being
legacy but for adding a layer that contributes nothing once the core library exists.

### Interpretation

Considered per target: interpreting the spec at runtime (e.g. a generic Decider driven
by parsed YAML) would erase the compile-time exhaustiveness guarantees that are this
repo's strongest fitness function (exhaustive `switch`, no default arm,
TreatWarningsAsErrors). Rejected for stratum 1 here; the spec's value *is* that the
compiler enforces it.

## 4. Mechanism details

Verified against Roslyn/xunit behavior during research (sources in §8).

### Analyzer wiring without NuGet (in-repo generator)

```xml
<!-- consuming Domain .csproj -->
<ProjectReference Include="..\Emlang.Generators\Emlang.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
<AdditionalFiles Include="..\..\specs\blindbudet-event-model.yaml" />
```

- Generator project targets **netstandard2.0** (per-project override; do not fight
  Directory.Build.props globally).
- Reference a deliberately **old stable** `Microsoft.CodeAnalysis.CSharp` — warning
  CS9057 fires only when the analyzer's Roslyn is *newer* than the compiler's, so old
  is the safe direction under a preview SDK.
- Emitting C# 15 `union` **text** is fine: generated trees parse with the *consumer's*
  `LangVersion=preview`, not the generator's.
- Emitted files start with `// <auto-generated/>` and must be nullable-clean (the
  Domain has TreatWarningsAsErrors).
- YamlDotNet flows into the analyzer via the `GetTargetPathWithTargetPlatformMoniker`
  target trick; the core library is **source-linked** (`<Compile Include="..\Emlang.CodeGen\**\*.cs" />`)
  into the generator project so only one dll + YamlDotNet ship as analyzer assets.

### The partial-method seam (CS8795)

Generated `Decider.g.cs` shape:

```csharp
// <auto-generated/>
public static partial class Decider
{
    public static GameState Evolve(GameState state, GameEvent evt) => evt switch
    {
        LobbyOpened e         => EvolveLobbyOpened(state, e),
        PlayerJoined e        => EvolvePlayerJoined(state, e),
        // ... one arm per e: in the spec, exhaustively, no default
    };

    private static partial GameState EvolveLobbyOpened(GameState state, LobbyOpened e);
    private static partial GameState EvolvePlayerJoined(GameState state, PlayerJoined e);
}
```

Human-owned `Decider.Impl.cs` implements the partials. A new `e:` in the spec makes the
build fail with **CS8795** (partial method declared but not implemented) until a human
or agent writes the body — "new spec event = compile error" is exactly achievable, and
TreatWarningsAsErrors makes the seam stricter still.

### Generated xUnit facts point at the YAML

xunit.v3 discovers generator-emitted `[Fact]`s normally. Its `FactAttribute` captures
`[CallerFilePath]`/`[CallerLineNumber]`, which honor `#line` — the same technique
Reqnroll uses to map generated tests back to `.feature` files:

```csharp
#line 137 "specs/mer-eller-mindre-event-model.yaml"
    [Fact]
#line default
    public void PlayerJoins() { /* generated GWT body */ }
```

Discipline: `#line` on the attribute only, `#line default` before the body (so stack
traces in the body stay honest). Set `EmitCompilerGeneratedFiles=true` on test projects
for inspection.

### Shadow-mode diffing (step 0, pre-flip)

`EmitCompilerGeneratedFiles` diffing is structurally impossible *before* the flip: an
attached generator emitting types that also exist as committed files ⇒ CS0101 duplicate
type errors. Shadow mode is instead **unit tests in the existing `*.Domain.Tests`
projects** calling `Emlang.CodeGen` directly and comparing **structurally**: Roslyn-parse
both the generated text and the committed file, compare the declaration surface (type
names, union case lists, record parameter names + types). That is a strict superset of
today's regex arch checks — valuable even if the flip never happens. Exact-text diff is
only meaningful at flip time, when the committed file is about to be deleted.

## 5. Dialect extension design: `fixtures:`

Designed here, applied in a later session (step 0 spike). Goal: make the fixture
comments at mer-eller-mindre-event-model.yaml:150-156 machine-readable without turning
emlang into a programming language.

### Shape

```yaml
fixtures:
  question0:
    Question:
      questionText: question0
      itemA: question0A
      itemB: question0B
      valueA: 100
      valueB: 60
      unit: question0U
      differencePrompt: question0D
  packMerEllerMindre:
    QuestionPack: { packId: mer-eller-mindre, questions: [question0, question1] }
  hostMartin: { hostName: Martin }
```

- A fixture is a named, typed prop bag. Bare identifiers in value position that match
  another fixture name are references (`questions: [question0, ...]`).
- Ids/timestamps/join codes are **not** in fixtures — they come from the deterministic
  test `GameContext` (sequenced Guids, fixed clock), which stays in the human-owned
  `Fixtures.cs`.

### Placeholder semantics (in `tests:` blocks, unchanged syntax)

| Pattern | Meaning |
|---|---|
| `playerName: Nils` (concrete value) | equality assertion / concrete input |
| `playerName: playerName` (value == prop name) | **presence-only** — assert the prop exists, don't pin the value |
| `then: - e:` props referencing a fixture name | structural equality against the fixture-built record |
| `given: - v: State / Game` with partial props | partial-state Given: build state by folding the fixture events implied by the named phase, then apply the listed props as `with`-mutations |

The partial-state rule is the riskiest piece (it encodes construction knowledge); the
mitigation is that anything the rule cannot express stays in the human-owned
`Fixtures.cs`/GWT scaffold, and generated tests reference those members **by plain
identifier** — a missing fixture is a compile error, same seam philosophy as CS8795.

### emlang lint compatibility — open question

Whether `emlang lint` (Go CLI v1.0.0) tolerates an unknown top-level `fixtures:` key
(or a per-slice one) is **untested** and must be checked empirically before committing
to the shape. Fallbacks, in order: (a) `.emlang.yaml` already ignores one lint rule —
add another; (b) sidecar file `specs/<game>-fixtures.yaml` parsed only by
`Emlang.CodeGen`. The sidecar is the safe default if lint objects; the cost is one more
file per spec.

## 6. Step-wise roadmap (shadow-first, delete-late)

### Step 0 — `Emlang.CodeGen` core + structural shadow tests

- **Preconditions:** none; zero build-integration risk.
- **Deliverable:** `src/Emlang.CodeGen` (YamlDotNet, line-tracked model, C# emitters)
  + shadow tests in each existing `*.Domain.Tests` comparing generated vs committed
  declaration surfaces (Roslyn structural compare, §4). **Spike the `fixtures:`
  dialect here** — it is the highest design risk and back-propagates into the parser
  model; test emlang lint tolerance empirically.
- **Done-gate:** shadow tests green for all three games; the two regex spec-coverage
  facts per game are superseded (deleted) by the structural versions. Nothing else
  deleted.
- **Touchpoints:** `.claude/hooks/codehealth.sh:49` scope regex whitelists only the
  existing 4 projects — add `Emlang\.` or the new project is silently unscored (the
  script itself warns about this).

### Step 1 — Flip records + unions (pilot: Blindbudet, smallest at 906 LOC)

- **Preconditions:** step 0 shadow green.
- **Deliverable:** `Emlang.Generators` analyzer wrapper; Analyzer `ProjectReference`
  wiring; **delete** `Commands.cs`, `Events.cs`, `Errors.cs` + pure view records
  (~166 LOC in BB). Then repeat for MEM (~225) and TTT (~164). **[DONE for all three
  games — BB §9.1, MEM+TTT §9.2.]**
- **Done-gate:** `dotnet build` + full test suite green with generated types; deleted
  files gone from git; shadow tests for this layer retired (the generator *is* the
  source now).
- **Touchpoints:** amend `Domain_project_has_no_dependencies`
  (MerEllerMindre.Domain.Tests/ArchitectureTests.cs:65-66 and siblings) to allow
  Analyzer-only references — an honest fitness-function change, not a
  Directory.Build.targets workaround. `Every_spec_element_has_a_code_type` /
  `Every_union_case_appears_in_the_spec` become redundant by construction — delete.

### Step 2 — Decide/Evolve skeletons

- **Preconditions:** step 1 flipped for at least one game.
- **Deliverable:** restructure each `Decider.cs` into `Decider.Impl.cs` partial bodies;
  generator emits the exhaustive switch + declared-only partials (CS8795 seam, §4).
- **Done-gate:** build green; a deliberately added fake `e:` in a scratch spec fails
  compilation with CS8795 (one-off verification); ~410 LOC of switch scaffolding gone.
- **Touchpoints:** `Decider_is_total_and_synchronous` reads
  `src/<Game>.Domain/Decider.cs` by path — repoint at `Decider.Impl.cs` (the generated
  half is total/sync by construction).

### Step 3 — GWT `[Fact]` generation

- **Preconditions:** `fixtures:` dialect landed in the specs (step 0 spike outcome);
  steps 1-2 flipped.
- **Deliverable:** generator emits DeciderTests/EvolveTests/ProjectionTests from
  `tests:` blocks with `#line`-to-YAML; **`Fixtures.cs` stays human-written** —
  generated tests reference its members by identifier.
- **Done-gate:** all 175 domain `[Fact]`s (or their generated equivalents) green;
  hand-written GWT files deleted (~2,196 LOC); test failures navigate to the YAML line.
  Non-spec tests (CSV parser, selection, solver, architecture) stay hand-written.
- **Touchpoints:** `EmitCompilerGeneratedFiles=true` on test projects for review.

### Step 4 — Web, split

- **Deliverable:** Vm records (plain C#) → same SG path as step 1. `.razor` screen
  scaffolds → CLI tool under `tools/` (repo convention: `dotnet run tools/x.cs`),
  scaffold-once, human-owned after — **not** build artifacts (Razor SDK ordering, §3).
- **Done-gate:** Web builds + `MerEllerMindre.Web.Tests` (23 facts) green.
- **Touchpoints:** codehealth.sh scope again if a new project appears.

### Step 5 — Process guarantees + measurement

- **Deliverable:** CI invariant *artifact == f(spec, generator)* — a job that
  regenerates and fails on divergence; `EmitCompilerGeneratedFiles` review-diff ritual
  on generator version bumps; the **measurement**: LOC evaporated from git vs the
  article's 60-70% prediction (baseline table in §2).
- **Done-gate:** the number published back into the research article; ADR 016 written
  covering the whole mechanism.

## 7. Risks and falsifiable predictions

1. **Preview-SDK/IDE coupling.** This repo builds on a .NET 11 preview with C# 15
   unions. Mitigation: pin an old stable Roslyn in the generator (CS9057 only fires
   analyzer-newer-than-compiler); CLI builds are the safe direction; accept possible
   IDE squiggle glitches as cosmetic.
2. **Diagnostics UX.** A YAML typo must surface as a precise diagnostic located *in the
   yaml* (`Location.Create` from YamlDotNet Marks), with emit-nothing-on-error
   semantics — otherwise the spec stops being the cheapest place to make a change,
   which is the article's single operational invariant. Budget: roughly an emitter's
   worth of work; do not skip it.
3. **Test-semantics gap.** The `fixtures:`/placeholder dialect must express
   presence-only asserts, folded partial-state Givens, and concrete-type extraction
   from union-typed results — without becoming a programming language. Mitigation: the
   human-owned `Fixtures.cs` seam absorbs everything the dialect refuses to express.
4. **Böckeler risk + Reeves residue.** If the spec must grow so detailed to drive
   generation that reviewing it is as hard as reviewing code, the bottleneck merely
   moves; and if decide/evolve bodies dominate effort, the deterministic stratum
   shrinks to scaffolding. The kvissig measurement (step 5) is the empirical answer to
   both.

**Falsifiable prediction (the experiment the article calls for):** flipping steps 1-3
across all three games removes ≈1,030 domain LOC + ≈2,196 test LOC ≈ **46% of the
7,026 domain+test lines** from git, against the article's 60-70% prediction for the
spec-determined layers. Whichever way the number lands, it is the first measured data
point for the thesis.

## 8. Sources

- The research article: `MartinRL.github.io/articles/code-as-build-artifact-research.md`
- Roslyn: source generators as project references without NuGet —
  https://github.com/dotnet/roslyn/discussions/47517
- Roslyn: CS9057 analyzer/compiler version mismatch —
  https://github.com/dotnet/roslyn/issues/66918
- Andrew Lock, *Creating a source generator* series —
  https://andrewlock.net/series/creating-a-source-generator/
- Thinktecture, *Roslyn Source Generators* series, parts 6-7 (referencing third-party
  assemblies from analyzers; the `GetTargetPathWithTargetPlatformMoniker` trick)
- xunit.v3 `FactAttribute` caller-info (`[CallerFilePath]`/`[CallerLineNumber]`, honors
  `#line`) — https://github.com/xunit/xunit
- Reqnroll `#line` mapping of generated tests to `.feature` files —
  https://github.com/reqnroll/Reqnroll/issues/413
- mono/t4 (`dotnet-t4`, Mono.TextTemplating) — https://github.com/mono/t4
- In-repo prior art: `SpecElementNames()` spec↔code cross-check,
  `src/MerEllerMindre.Domain.Tests/ArchitectureTests.cs:140-211` (siblings in
  Blindbudet/TankTillTusen arch tests); fixture comments,
  `specs/mer-eller-mindre-event-model.yaml:150-156`.

## 9. Experiment log — step 0 (2026-07-12, branch `experiment/emlang-codegen-shadow`)

**Question tested:** do the specs' `c:`/`e:`/`x:` props deterministically define the
committed record surfaces (type names, prop names, prop types, order) across all three
games?

**Answer: YES — the assumption holds, stronger than expected.** The shadow tests
(`SpecSurfaceShadowTests` × 3, backed by `src/Emlang.CodeGen`'s
SpecModel/CodeSurface/SurfaceComparer) went green for all three games **on the first
run**, with an **empty findings allowlist**. The planned classification loop converged
at iteration 0. A false green is structurally excluded: the comparison is
bidirectional (an empty parse on either side floods missing-record /
union-extra divergences), and 23 inline-YAML/C# negative cases prove the comparer
fails on fabricated divergences (fake prop, extra param, wrong type, missing element,
orphan record, swapped order, union drift both ways, wrong namespace).

| Metric | Result |
|---|---|
| # general mapping rules | **6** — (1) camelCase → PascalCase prop names; (2) `X[]` → `IReadOnlyList<X>`; (3) strip parenthesized note (`Direction (mer\|mindre)` → `Direction`); (4) events strip the `Game / ` stream prefix; (5) surface comes from slice *steps* only (`tests:` props are fixture values); (6) the props-richest occurrence of an element defines it (bare re-occurrences are slice inputs). Inline `# comments` cost nothing (YAML scalars end at the comment). |
| # manifest facts per game | **3 categories** (`GameManifest`, three literal instances): union names (`GameCommand`/`AuctionCommand`/`TankCommand` + Event/Error), namespace, file paths (spec + Commands/Events/Errors.cs). Exactly the per-game facts the spec structurally cannot say — confirmed by construction. |
| # (c)-findings (irreducible spec↔code gaps) | **0** — no allowlist entries in any game. |
| Probe A verdict | `emlang lint` v1.0.0 **hard-rejects** `fixtures:` at both placements — `unknown top-level key "fixtures"` and `unknown slice key "fixtures"` are *parse* errors, so the `.emlang.yaml` ignore fallback is dead too. **Step 3 must use sidecar `specs/<game>-fixtures.yaml`** parsed only by Emlang.CodeGen. |

**Interpretation:** rules small (6), manifest tiny (3 facts/game), zero findings ⇒
the deterministic-mapping thesis **holds** for the stratum-1 record layer. The 6 regex
spec-coverage facts (2 × 3 games) are deleted, superseded by the strictly stronger
structural check (adds prop names/types/order, union membership both ways, namespace).
Proceed to **step 1** (analyzer wiring, flip Blindbudet records — separate plan +
ADR 016). Residual risks unchanged: analyzer/preview-SDK wiring (§7.1) and the
`fixtures:` test-semantics dialect (§7.3) were *not* exercised here, except that
Probe A now fixes the fixtures placement to a sidecar.

## 9.1 Experiment log — step 1 (2026-07-12, same branch)

**Flip executed: Blindbudet's Commands.cs/Events.cs/Errors.cs are generated, not
committed.** Mechanism per ADR 016: `Emlang.CodeGen` retargeted netstandard2.0, new
`SurfaceEmitter` (spec elements → C# text, records in spec order + closed union),
new `Emlang.Generators` incremental generator (AdditionalFiles → manifest match →
`AddSource`), output in `obj/` only.

| Metric | Result |
|---|---|
| LOC removed from git | **166** domain (Commands 51 + Events 78 + Errors 37) + 29 retired shadow test = 195, vs the ~166 §6 prediction — exact hit |
| Wiring pain (§7.1, the deferred risk) | **None.** The cookbook stack (netstandard2.0 + Roslyn pinned 4.14.0 + `GetDependencyTargetPaths` shipping Emlang.CodeGen.dll/YamlDotNet.dll) built and generated **on the first attempt** — no CS9057, no analyzer-load failures. The real cost was the netstandard2.0 retarget itself: ~8 mechanical API fixes (ranges → `Substring`, `Contains(char)`, `TrimEntries`, non-generic `MatchCollection`, KVP deconstruction, explicit usings, `IsExternalInit` shim) |
| Test delta | 243 → **247**: −1 Blindbudet shadow test (tautological post-flip), +5 emitter self-tests. All green; Web compiles unchanged against generated types |
| Correctness proof | emit → `SurfaceComparer` → 0 divergences for **all three** manifests against the real specs — the step-0 harness reused as the generator's own gate. Reflection arch tests (`All_public_domain_types_are_records` etc.) validate the generated types for free |

**Interpretation:** the analyzer-wiring risk (§7.1) is retired empirically. MEM (~225
LOC) and TankTillTusen (~164) are now a csproj-wiring + `git rm` + shadow-test-retire
each — the generator and manifests already handle them. Next material risk is step 2's
partial-method Decider seam.

### 9.1.1 Practical findings — netstandard2.0 retarget of the core library

The §4 assumption "generator project targets netstandard2.0, core is source-linked"
was replaced by a simpler shape: **the core itself (`Emlang.CodeGen`) retargeted to
netstandard2.0** and ships as a dll beside the analyzer. One TFM, no multi-targeting,
no source-linking — the net11.0 test projects consume the netstandard2.0 dll fine.

Every API gap surfaced as a build error and was mechanical to fix. The complete list
(useful as a checklist for any future library that must load in-compiler):

| netstandard2.0 gap | Fix |
|---|---|
| Records need `IsExternalInit` | 3-line internal shim in the library (no PolySharp dep) |
| Range/index operators on `string` (`s[1..]`, `s[..^2]`) | `Substring(...)` equivalents |
| `string.Contains(char)` | `IndexOf(char)` — or restructure (`LastIndexOf + 1` handles the no-prefix case for free, since `-1 + 1 = 0`) |
| `StringSplitOptions.TrimEntries` + `Split(char, StringSplitOptions)` | `Split(',')` + LINQ `Select(Trim)` / `Where(Length > 0)` |
| `MatchCollection` is non-generic (`IEnumerable`, not `IEnumerable<Match>`) | `.Cast<Match>()` before LINQ |
| `KeyValuePair<K,V>` has no `Deconstruct` | iterate the pair, use `.Key`/`.Value` |
| `ImplicitUsings=enable` emits **no** default usings on netstandard2.0 (the SDK gates them on net6.0+) | explicit `using` lines per file; drop the property |

What did **not** need fixing: `LangVersion=preview` features (collection expressions
`[.. x]` incl. `IReadOnlyList<T>` targets, switch expressions, pattern matching,
file-scoped namespaces, target-typed `new`) all compile down to netstandard2.0 — they
are compiler features, not BCL features. `ValueTuple`, `Array.Empty<T>`,
`[CallerFilePath]`, `EndsWith(string, StringComparison)` all exist in ns2.0.
YamlDotNet 16.3.0 and Microsoft.CodeAnalysis.CSharp 4.14.0 both carry netstandard2.0
assets.

### 9.1.2 Practical findings — analyzer wiring (the exact recipe that worked)

First attempt, zero failures. The three load-bearing pieces:

1. **Roslyn pin, old-stable:** `Microsoft.CodeAnalysis.CSharp 4.14.0` with
   `PrivateAssets="all"`. CS9057 only fires when the analyzer's Roslyn is *newer* than
   the compiler's — under a preview SDK (11.0.100-preview.5) old is always safe.
2. **Dependency shipping** in Emlang.Generators.csproj — every
   `TargetPathWithTargetPlatformMoniker` item returned from `GetTargetPath` becomes an
   Analyzer item in the consuming project, which is how Emlang.CodeGen.dll and
   YamlDotNet.dll get into the compiler's analyzer load context:

   ```xml
   <PropertyGroup>
     <GetTargetPathDependsOn>$(GetTargetPathDependsOn);GetDependencyTargetPaths</GetTargetPathDependsOn>
   </PropertyGroup>
   <Target Name="GetDependencyTargetPaths">
     <ItemGroup>
       <TargetPathWithTargetPlatformMoniker Include="$(PKGYamlDotNet)\lib\netstandard2.0\YamlDotNet.dll" IncludeRuntimeDependency="false" />
       <TargetPathWithTargetPlatformMoniker Include="$(TargetDir)Emlang.CodeGen.dll" IncludeRuntimeDependency="false" />
     </ItemGroup>
   </Target>
   ```

   (`$(PKGYamlDotNet)` requires `GeneratePathProperty="true"` on the YamlDotNet
   PackageReference; `$(TargetDir)Emlang.CodeGen.dll` works because the plain
   ProjectReference copy-locals the dll into the generator's own output.)
3. **Consumer wiring** — two lines in the Domain csproj:

   ```xml
   <AdditionalFiles Include="..\..\specs\blindbudet-event-model.yaml" />
   <ProjectReference Include="..\Emlang.Generators\Emlang.Generators.csproj"
                     OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   ```

Confirmations of §4's untested claims: emitting C# 15 `union` **text** from a
netstandard2.0 analyzer works exactly as predicted — the generated tree parses with
the *consumer's* `LangVersion=preview`. The generator emits `#nullable enable` +
explicit `using System; using System.Collections.Generic;` so the output is
self-contained under `TreatWarningsAsErrors` regardless of the consumer's
ImplicitUsings.

### 9.1.3 Practical findings — test architecture after a flip

- **The step-0 shadow harness converts into the generator's correctness proof for
  free.** `SurfaceEmitterTests` runs emit → `SurfaceComparer.Compare(manifest, spec,
  emitted…)` → must be empty, for all three games. Same comparer, same 23 negative
  cases guarding against false greens — no new proof machinery was written for step 1.
- **A flipped game's per-game shadow test must be deleted, not kept:** it reads the
  committed files from disk (crash post-`git rm`), and spec↔generated comparison is
  tautological anyway. Unflipped games' shadow tests stay — their committed files can
  still drift.
- **Reflection-based architecture tests need no changes** — they inspect the compiled
  assembly, so `All_public_domain_types_are_records`, the union-exhaustiveness facts
  etc. validate generated types automatically. Path-scanning arch tests
  (`No_reflection_or_dynamic_in_domain` globs `src/<Game>.Domain/*.cs`) silently skip
  generated code — acceptable: the emitter is deterministic and comparer-gated.
- **Tooling gates hold by construction:** codehealth.sh already excluded `\.g\.cs$` and
  `/obj/`, so generated output is invisible to the CH gate; the scope regex needed
  `Emlang\.(CodeGen|Generators)` so the generator projects themselves stay scored (all
  new files CH 10.0).
- The pre-verified "no test guards the Domain csproj" held for Blindbudet. **MEM is
  different:** `Domain_project_has_no_dependencies` reads MEM's csproj text — flipping
  MEM will require amending it to allow Analyzer-only ProjectReferences (an honest
  fitness-function change, anticipated by §6 step 1).

### 9.1.4 Flip recipe (for MEM and TankTillTusen, when decided)

Per game, the whole flip is: (1) add the two wiring lines (§9.1.2 item 3, pointing at
that game's spec) to the Domain csproj; (2) `git rm Commands.cs Events.cs Errors.cs`;
(3) delete that game's `SpecSurfaceShadowTests.cs` and drop the now-unused
Emlang.CodeGen ProjectReference from its Tests csproj; (4) MEM only: amend
`Domain_project_has_no_dependencies`. The generator, manifests, and emitter self-tests
already cover all three specs — no generator-side work remains.

## 9.2 Experiment log — step 1 completed for MEM + TankTillTusen (2026-07-12)

The §9.1.4 recipe executed verbatim for the remaining two games; **step 1 is now DONE
for all three games** — no stratum-1 record file remains in git anywhere.

| Metric | Result |
|---|---|
| LOC removed from git | MEM **225** domain (Commands 62 + Events 108 + Errors 55) — exact §6 prediction; TTT **166** (Commands 51 + Events 78 + Errors 37) vs ~164 predicted. +29 LOC retired shadow test each |
| Wiring pain | **None**, again. Two csproj blocks (AdditionalFiles + Analyzer ref), first-attempt green build for both |
| Test delta | 247 → **245**: −2 retired shadow tests (MEM + TTT, tautological post-flip). Whole suite green; Web + all Decider/Evolve/Projection tests compile unchanged against the generated surface |
| Fitness-function amendment (the anticipated §6 touchpoint) | `Domain_project_has_no_dependencies` now parses MEM's csproj XML: every `<ProjectReference>` must carry `OutputItemType="Analyzer"` + `ReferenceOutputAssembly="false"`; `<PackageReference>` still banned outright. Still red on any runtime dependency. TTT needed no arch-test change (`TankArchitectureTests` never reads the csproj) |
| Deviations from the §9.1.4 recipe | None |

**Interpretation:** flips after the pilot are pure mechanics, ~10 lines of csproj per
game. Running total for step 1: **557 domain LOC + 3 shadow tests (87 LOC) out of
git**. The next material risk is unchanged: step 2's partial-method Decider seam
(CS8795).

## 9.3 LOC accounting — branch vs main (2026-07-12)

An honest close-of-step-1 balance sheet: how much does the branch (10 commits ahead of
`main`) differ from `main` in committed C# source, in percent?

Committed C# LOC per branch (`git ls-tree -r --name-only <ref> | grep '\.cs$'`, then
`git show <ref>:<file> | wc -l` summed per area):

| Area | main | branch | delta |
|---|---|---|---|
| Game prod (Domain + Web) | 6 173 | 5 616 | **−557 (−9.0 %)** |
| Game tests | 4 372 | 4 228 | −144 (−3.3 %) |
| Emlang.CodeGen + Emlang.Generators (new infra) | 0 | 442 | +442 |
| Emlang.CodeGen.Tests (new) | 0 | 266 | +266 |
| **Total .cs in git** | **10 545** | **10 552** | **+7 (+0.07 %)** |

Churn (`git diff main...HEAD --shortstat` on `*.cs`): 22 files, **+729/−722** — roughly
13.8 % of main's C# corpus touched. Non-C# churn: +336/−2 (this doc §9–9.2, ADR 016,
csproj wiring, memory files). Infra breakdown: Emlang.CodeGen 398 LOC (SpecModel 107,
SurfaceComparer 126, GameManifest 61, CodeSurface 58, SurfaceEmitter 42, IsExternalInit 4)
+ Emlang.Generators 44.

**Interpretation (honest):** at n=3 games the experiment is **LOC-neutral** (+7 net).
What was deleted is O(games) transcription (~186 LOC/game plus shadow tests); what was
added is O(1) infrastructure (708 LOC incl. tests). The breakeven point is crossed with
game 4: its record layer costs ~2 csproj lines instead of ~186 LOC. The real win is
structural, not numeric — the spec is now the single change surface for the record
layer, enforced by the compiler.
