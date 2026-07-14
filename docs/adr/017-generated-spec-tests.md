---
status: Accepted
type: architecture
created: 2026-07-13
---

# ADR 017: Spec `tests:` sections are compiled into xUnit facts at build time

## Context
ADR 016 made the stratum-1 record surface a build artifact of `specs/*-event-model.yaml`.
The specs' `tests:` sections (115 GWT/GT cases across the three games) were still
hand-translated into DeciderTests/EvolveTests/ProjectionTests — 2,197 LOC of pure
transcription (analysis doc §2 estimated 2,196). Same lockfile argument, same cure.

ADR 016 anticipated sidecar fixture files (`specs/<game>-fixtures.yaml`) because
`emlang lint` rejects a `fixtures:` key. That turned out unnecessary: the specs already
reference fixtures **by name** (`hostMartin`, `question0`, `q0Scored`); the generator
resolves each bare word to a `Fixtures.*` member in the test project and lets the
compiler be the oracle — a name with no matching member is a CS0117, not a silent skip.

## Decision
A second generator output, gated per-project, emits one `SpecTests.g.cs` per game from
the spec's `tests:` sections. The hand-written GWT/GT files are **deleted from git**.
Applied to all three games on 2026-07-13 (TankTillTusen `2e99483`, Blindbudet
`7612cec`, MerEllerMindre `85ede1d`).

### The `EmlangEmit` gate
Both generators read `build_property.EmlangEmit` via `AnalyzerConfigOptionsProvider`
(absent = `surface`): Domain projects get the record surface; test projects set
`<EmlangEmit>tests</EmlangEmit>` (plus `<CompilerVisibleProperty Include="EmlangEmit" />`)
and get only `SpecTests.g.cs`. Without the gate, adding the spec as AdditionalFiles to a
test project would emit a second record surface that shadows the Domain's types.

### Emission rules (Emlang.CodeGen/TestsEmitter)
- Classification: `when:` present → GWT decide test; otherwise a GT — then-`State` →
  `Decider.Fold`, then-`Screen`/`Todo` → `Projections.<ViewName>(Decider.Fold(…))`.
- One `[Fact(DisplayName = "<raw YAML key>")]` per case; method name = the key with
  non-alphanumeric runs collapsed to `_`.
- Values are typed by the spec's own step/view prop declarations: strings quoted,
  enums via their `(a|b|c)` note, `decimal` → `m` suffix, `byte` → cast, lists of
  `X[]` → `new X[] { … }`, and any other bare word → `Fixtures.PascalCase(word)`.
- `minted` sentinel: a then-prop whose value cannot be pinned (real `Guid.NewGuid`)
  is excluded from the equivalence pin and asserted `.Should().NotBe(default)`.
- Then-events are grouped by concrete type and matched with anonymous-object
  `BeEquivalentTo(…, o => o.WithStrictOrdering())`; union cases are checked with
  concrete `is` patterns only (C# 15 union values never match a generic `is T`).
- Flow-maps (inline YAML objects) are rejected with a failing test: every composite
  value under `tests:` must be a named fixture.

### The Fixtures seam
`Fixtures.cs` stays human/agent-owned in each test project: ids, players, cards,
rounds, view rows, and a stub Context with a **fixed clock** (`Now: () => Now`) so
timestamp then-pins are deterministic. Fixture names in YAML are the API; renaming a
fixture without updating the spec (or vice versa) is a compile error.

## Consequences
- **The spec is now load-bearing for the test suite.** 115 generated facts (TTT 38,
  BB 37, MEM 40); 2,197 LOC of hand-written GWTs left git. Editing a `tests:` case is
  the only way to change a spec test.
- Error-case fixtures must satisfy every guard **before** the one under test (decide
  guard order): e.g. an "already scored" round must carry its full submission history
  or the earlier `NotAllDifferencesIn` guard fires first. Found once per game; the
  generated test fails loudly, so the trap is self-announcing.
- Hand-written assertions on derived state methods (`PendingBidPlayerIds`,
  `TotalScore`, …) in the old EvolveTests were deleted as redundant: the data pins
  plus the command GWTs cover the derivations. Genuinely non-spec tests stay
  hand-written in small files (tank Solver/Validator/Parse, Blindbudet
  SamplingTests, MEM QuestionSelection/CsvParser), plus each game's architecture tests.
- Debug ritual: `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in a
  test csproj materializes `obj/…/generated/` for inspection.
- No emit-then-compile check in Emlang.CodeGen.Tests (Roslyn 4.14 cannot parse C# 15
  unions); the solution build plus the green game suites are the round-trip proof.
