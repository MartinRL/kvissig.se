# ACMM placement — kvissig.se / Mer eller Mindre

Where this repo sits on the **AI Codebase Maturity Model** (Anderson, IBM Research,
arXiv:2604.09388). ACMM grades a codebase by **feedback-loop topology**, not by how much
autonomy the AI has: L1 Assisted → L2 Instructed → L3 Measured → L4 Adaptive →
L5 Self-sustaining. You cannot skip levels; each unlocks the next by adding a feedback
mechanism.

Assessed 2026-06-18 against the live repo, not from memory.

## Verdict

**Solid L2, reaching into L3 (test-as-trust), but not measured-L3, not L4/L5.**

The intelligence here lives in the system, not the model — which is the whole ACMM thesis
— but the loops are *one-way (human→AI)* and *human-interpreted*, not *closed*.

## Evidence by level

### L2 — Instructed (solid)
Human judgment is encoded in artifacts the agent loads every session, not held in memory:
- `CLAUDE.md` — project rules, constraints (no SignalR/EF/Blazor-circuit), workflow.
- `.claude/constitution.md` — coding standards.
- Auto-memory (`~/.claude/.../memory/`) — confirmed conventions persisted across sessions.
- `specs/game-flows.yaml` — emlang spec as single source of truth; code maps to it 1:1.
- 7 ADRs — every architectural choice is a written decision with a *why*.

### L3 — Measured (partial: the trust half, not the metrics half)
ACMM's L3 breakthrough is **testing as the trust mechanism** ("instructions made the AI
consistent; tests made it trustworthy"). That half is present and strong:
- Deterministic functional-core tests — `DeciderTests`, `EvolveTests`, `ProjectionTests`
  (69 Domain tests, all green). No mocks, no I/O — the pure core makes them bombproof.
- `ArchitectureTests` — enforce the FC/IS boundary *in CI*; I/O in the core fails the build.
- `GameEndpointsTests` — E2E through the imperative shell (11, green).
- EvalOps — a failed test feeds back into auto-memory as a permanent lesson.
- `specs/bugs.md` — logged-bug loop (`[x]` on fix), a lightweight defect feedback record.

What's **missing** for full L3 (the *measured* half):
- No coverage gating (no `coverlet` collector wired; no threshold).
- No acceptance-rate tracking, no error monitoring, no quantitative AI-performance signal.

So: the tests make the AI *trustworthy* (L3 trust), but the system does not yet *measure*
its own AI performance (L3 metrics). Early/partial L3.

### L4 — Adaptive (not reached)
No self-tuning configs, no thresholds that trigger automated responses, no closed loops.
The `tools/pack.cs` band-histogram report + the agent content-pipeline are **manually
triggered** quality mechanisms — adaptive *in spirit*, but a human pulls the lever.

### L5 — Self-sustaining (not reached)
No community issue→implementation pipeline, no self-improvement cycle. (Nor is it a goal —
this is a solo weekend project, not a 24/7 CNCF dashboard.)

## Measurements taken (2026-06-18)

| Signal | Value |
|---|---|
| Tests | 80/80 green (Domain 69 + Web 11) |
| `ArchitectureTests` | FC/IS fitness boundary passes |
| Coverage | not instrumented (deliberate — no dependency added for one figure) |
| `scc` total | 19 887 lines |
| `scc` pure code | 4 347 (C# 3 182 + Razor 443 + CSS 722) |
| Question pack | 1 085 cards (5 306 CSV lines) |
| COCOMO (organic) | ~$513 883 (drifts up as `articles/` lands in the repo) |

A red test (`CreateGame_RedirectsHostToTheLobbyShell`, stale contract after the
clickable-join-URL commit) was found during this assessment, logged in `specs/bugs.md`,
and fixed — bringing the suite to 80/80. That round-trip *is* the L3 trust loop working.

## Why this matters for the article

The "Practise What You Preach" article claims quality is *upheld, not asserted*. ACMM is
the same claim turned on the quality claim itself: don't assert a level, measure it. The
honest placement above backs §4 (quality mechanism) and §6 (the "factory" / role-shift)
without overclaiming — the content pipeline is a *miniature* factory (L4-flavoured loop,
manually fired), not a self-sustaining system.

## Upgrade path (if ever wanted)
- → full L3: wire `coverlet` + a coverage gate in CI; that's the cheapest next loop.
- → L4: only if the content pipeline's band-histogram thresholds start *auto-rejecting*
  candidates instead of reporting. Not currently worth it.
