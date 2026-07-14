---
status: Accepted
type: architecture
created: 2026-07-14
---

# ADR 018: Decider Evolve/Decide switch skeletons are generated (CS8795 seam)

## Context
ADR 016 made the stratum-1 records build artifacts; ADR 017 did the same for the spec
`tests:` sections. The exhaustive `Evolve`/`Decide` union switches in each game's
`Decider.cs` were the remaining pure transcription: dispatch order and shape are fully
determined by the spec's `e:`/`c:` elements. Worse, they were the one place where a new
spec element could land *silently* — the records and tests would generate, but no
switch arm would exist until someone remembered to write one (caught only by the
non-exhaustiveness error once the union grew, with no pointer to what the arm should
call).

## Decision
`DeciderEmitter` (Emlang.CodeGen) emits a `Decider.g.cs` per game alongside the record
surface: a `public static partial class Decider` containing the two exhaustive switches
(one arm per spec `e:`/`c:`, spec order, no default arm) plus one
`private static partial` declaration per element. The hand-written case **bodies** —
the residue: scoring, selection, folding — moved to `Decider.Impl.cs`
(git-renamed from `Decider.cs`) as partial-method implementations. Applied to all three
games on 2026-07-14 (TankTillTusen `2b1fded`, Blindbudet `8fb535c`, MerEllerMindre
`3eec3bf`; a transient `EmlangEmit=core` opt-in let each game flip in its own commit
and was collapsed in `ecb12bc` — `surface` now always emits `Decider.g.cs`).

### The seam
A **new `e:`/`c:` in the spec is a CS8795 compile error** ("partial method must have an
implementation part") until a human or agent writes the body in `Decider.Impl.cs` —
verified one-off with a fake event in the tank spec (analysis doc §9.4). The reverse
direction also holds: an impl whose element leaves the spec orphans and fails to
compile. Both directions of stratum-2 sync are compiler-enforced.

### Signature convention
Uniform partial signatures prevent CS8826 (parameter-name mismatch, an error under
`TreatWarningsAsErrors`):

```csharp
private static partial <State> Evolve<Event>(<State> state, <Event> e);
private static partial Result<<EventUnion>[]> Decide<Cmd>(<State> state, <Cmd> command, <Context> context);
```

Impls that don't need `state` or `context` keep the parameter (unused partial-impl
params raise no warning). `GameManifest` gained `ContextType` to name the per-game
context record.

## Consequences
- Fold, game constants, helpers (MapRound/MapQuestion/NormalizeDifference/
  QuestionSelection), the Context records and the sister `Result<T>` unions stay
  human-owned in `Decider.Impl.cs`.
- Honest LOC accounting: only the dispatch switches left git (~150 generated lines
  across three games, net ≈ −6 LOC per game) — the arms were already one-line
  delegations or `state with {…}` bodies that survive as partial impls. The win is
  the seam, not the count.
- `DeciderEmitterTests` gate the emitter: per game, exactly one switch arm and one
  partial declaration per spec element, and no default arm.
- Arch-tests (`Decider_is_total_and_synchronous`) read `Decider.Impl.cs`; the
  generated half is total/synchronous by construction. CodeScene rule pattern widened
  to `**/Decider*.cs`.
- This exhausts the §6 roadmap of `docs/analysis/code-as-build-artifact.md`
  (steps 0–3 done; step 4/5 remain optional future work).
