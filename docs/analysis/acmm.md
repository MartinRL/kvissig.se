# ACMM placement — kvissig.se / Mer eller Mindre

Where this repo sits on the **AI Codebase Maturity Model** (Anderson, IBM Research,
arXiv:2604.09388). ACMM grades a codebase by **feedback-loop topology**, not by how much
autonomy the AI has: L1 Assisted → L2 Instructed → L3 Measured → L4 Adaptive →
L5 Self-sustaining. You cannot skip levels; each unlocks the next by adding a feedback
mechanism.

Assessed 2026-06-18; **re-assessed 2026-07-02** after the CodeHealth gate landed (see below).
Both against the live repo, not from memory.

## Verdict (2026-07-02)

**Full L3 (both halves: test-as-trust *and* measured), with the repo's first genuinely
closed loop — early L4 on the code-health axis. Not full L4, not L5.**

The 2026-06-18 verdict was "solid L2 reaching into L3, but not *measured*-L3." The CodeHealth
gate (CH ≥ 9.4, commit `190491d`) closes that gap: there is now a **quantitative,
machine-interpreted quality signal** measured on every prod C# file, and — crucially — a
**closed loop**, not a one-way human→AI instruction. The Stop hook / CI gate *auto-rejects*
(`exit 2`) and feeds the sub-threshold files back to the agent to self-correct; the durable
lesson lands in `memory/code-health.md`. That is a threshold triggering an automated response
= the first L4-flavoured mechanism actually wired (machine pulls the lever, not a human).

Still short of full L4: the loop is single-axis (code health, not a self-tuning config across
the system) and the threshold/rules are human-authored, not self-tuned. And no coverage metric yet.

### Verdict (2026-06-18, superseded)

Solid L2, reaching into L3 (test-as-trust), but not measured-L3, not L4/L5. The intelligence
lived in the system, not the model, but the loops were *one-way (human→AI)* and
*human-interpreted*, not *closed*. — The CH gate is precisely what changed this line.

## Evidence by level

### L2 — Instructed (solid)
Human judgment is encoded in artifacts the agent loads every session, not held in memory:
- `CLAUDE.md` — project rules, constraints (no SignalR/EF/Blazor-circuit), workflow.
- `.claude/constitution.md` — coding standards.
- Auto-memory (`~/.claude/.../memory/`) — confirmed conventions persisted across sessions.
- `specs/mer-eller-mindre-event-model.yaml` — emlang spec as single source of truth; code maps to it 1:1.
- 7 ADRs — every architectural choice is a written decision with a *why*.

### L3 — Measured (now BOTH halves)
ACMM's L3 breakthrough is **testing as the trust mechanism** ("instructions made the AI
consistent; tests made it trustworthy"). That half was always strong:
- Deterministic functional-core tests — `DeciderTests`, `EvolveTests`, `ProjectionTests`.
  153 green (MEM Domain 87 + Blindbudet Domain 43 + Web 23). No mocks, no I/O — the pure core
  makes them bombproof. (Was 80; the second game, Blindbudet, added its own suite.)
- `ArchitectureTests` / `BlindbudetArchitectureTests` — enforce the FC/IS boundary *in CI*.
- `GameEndpointsTests` — E2E through the imperative shell.
- EvalOps — a failed test feeds back into auto-memory as a permanent lesson.
- `specs/bugs.md` — logged-bug loop (`[x]` on fix), a lightweight defect feedback record.

The **measured half** (missing on 2026-06-18) is now present:
- **CodeHealth gate (CH ≥ 9.4)** — CodeScene `cs` scores every prod C# file; a quantitative
  code-quality signal, gated hard-absolute. Stop hook (`codehealth.sh --changed`) locally +
  CI (`--all`) authoritative. 23 files, mean ≈ 9.91. This is the first metric the system
  *enforces*, not just reports.

Still absent (would deepen the measured half, not required for L3):
- No coverage gating (no `coverlet` collector wired; no threshold).
- No acceptance-rate tracking or error monitoring — CH measures code quality, not AI-agent
  acceptance rate per se. The article's EvalOps framing treats output-quality gating as the signal.

So: tests make the AI *trustworthy* AND the CH gate *measures* output quality with a hard bar.
**Full L3.**

### L4 — Adaptive (first loop wired; not fully reached)
The 2026-06-18 note read "no thresholds that trigger automated responses, no closed loops."
The CH gate changes that: **CH < 9.4 → `exit 2`** blocks the Stop hook / CI and feeds the
failing files back to the agent, which self-corrects, and the fix pattern is written to
`memory/code-health.md` — a threshold triggering an automated response, machine-fired. That is
a genuine closed loop, ACMM's L3→L4 mechanism.

But it is **one axis, human-authored**: the 9.4 bar and the scoped `.codescene/code-health-rules.json`
are set by humans, not self-tuned; `tools/pack.cs` band-histograms are still human-fired reports.
Early L4 on the code-health axis; not the self-tuning, multi-axis adaptivity of full L4.

### L5 — Self-sustaining (not reached)
No community issue→implementation pipeline, no self-improvement cycle. (Nor is it a goal —
this is a solo weekend project, not a 24/7 CNCF dashboard.)

## Measurements taken (2026-07-02)

| Signal | Value |
|---|---|
| Tests | 153/153 green (MEM Domain 87 + Blindbudet Domain 43 + Web 23) |
| `ArchitectureTests` | FC/IS fitness boundary passes (both games) |
| **CodeHealth (CH ≥ 9.4)** | **23 prod C# files, mean ≈ 9.91; 21 at 10.0, gate green** |
| Coverage | still not instrumented (deliberate — no dependency added for one figure) |
| Question pack | 1 085 cards (MEM) + 175-lot Blindbudet mini pool |

*(2026-06-18 baseline: 80/80 tests, no CH signal; `scc` total 19 887 lines / pure 4 347;
COCOMO organic ~$513 883.)*

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
- ✅ full L3 (measured): **DONE** — CodeHealth gate (CH ≥ 9.4), commit `190491d`.
- → deeper L4: make `tools/pack.cs` band-histogram thresholds *auto-reject* candidates
  instead of reporting (a second closed loop, on content quality), and/or let the CH bar /
  rules-config self-tune from history rather than being human-authored. Not currently worth it.
- → coverage: wire `coverlet` + a CI threshold — the cheapest remaining metric, orthogonal to CH.
