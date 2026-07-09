---
title: "Practise What You Preach, p2: From ACMM 2+ to 3"
aliases:
  - pwyp-p2
  - acmm-2plus-to-3
type: linkedin-post
status: draft
created: 2026-07-09
language: en
series: practise-what-you-preach
part: 2
tags:
  - linkedin
  - acmm
  - codehealth
  - codescene
  - evalops
  - kvissig
related:
  - "[[acmm]]"
  - "[[../../articles/practise-what-you-preach/en/practise-what-you-preach]]"
---

# Practise What You Preach, p2: From ACMM 2+ to 3

## The LinkedIn post (ready to paste)

---

In my last piece I claimed quality isn't asserted — it's enforced. Fair challenge: prove it. So I turned the claim on itself.

I graded my own repo against the AI Codebase Maturity Model (ACMM v2, Anderson, IBM Research) — six levels, graded by feedback-loop topology, not by how much autonomy the AI has. The honest verdict on June 18: **Level 2+**. Solid "Instructed" (spec as source of truth, ADRs, a constitution the agent loads every session), reaching into Level 3 because deterministic functional-core tests made the AI *trustworthy*. But nothing *measured* output quality. The loops were one-way, human-interpreted. Not closed.

Two weeks later: **full Level 3**. Here's what changed.

I wired a CodeScene CodeHealth gate: every production C# file must score **≥ 9.4 / 10**. A Stop hook runs it locally on changed files; CI runs it on everything. Below threshold → hard reject → the agent self-corrects → the fix pattern is written to persistent memory as a permanent lesson. That's EvalOps: a quantitative signal the system *enforces*, not just reports — and the repo's first genuinely closed loop.

The baseline was humbling: 23 files, 7 below the bar, worst at 7.92. Then the loop did its job:

→ Endpoint registration at 7.92: my first "fix" made it *worse* (7.45!) — extracting lambdas to methods exploded the argument counts. The real fix: bundle dependencies into a record. 7.92 → passing.
→ A 15-branch selection algorithm: 8.58 → 9.68, by giving a stateful accumulator its own small class.
→ A CSV tokenizer: 8.09 → 8.79 by flattening nesting — and the residual complexity declared *intentional* in a scoped rules config.

And the honest part: two files still sit at **9.38**. They're exhaustive pattern-matching switches — one arm per screen — mandated by the project's own constitution. Splitting them would scatter the complexity, not remove it. So they're documented exemptions, visible in every report. The global 9.4 bar was never lowered to make the dashboard green.

That's the difference between claiming a level and measuring one. Mean today: 9.91.

The game the gate guards: kvissig.se — play it, more or less.

#ACMM #CodeHealth #CodeScene #EvalOps #AIEngineering #SoftwareQuality

---

## Backing notes (evidence, not for the post)

### Timeline (from git history)

| Date | Commit | What |
|---|---|---|
| 2026-06-18 | `3ac5a21` | Initial ACMM assessment (`docs/acmm.md`): **"Solid L2, reaching into L3 (test-as-trust), but not measured-L3, not L4/L5."** Tests trustworthy, but no quantitative quality signal; loops one-way, human-interpreted. |
| 2026-06-22 | `cd7a13b` | acmm.md relocated to `docs/analysis/`. |
| 2026-07-02 | `190491d` | **CodeHealth gate lands**: `.claude/hooks/codehealth.sh` (Stop hook `--changed`, CI `--all`), `.codescene/code-health-rules.json`, refactors to ≥ 9.4. All 153 tests green. |
| 2026-07-02 | `ab6d52e` | ACMM re-assessed: **"Full L3 (both halves), early L4 on the code-health axis."** |
| 2026-07-04 | `a700c4b` | CI hardening (installer's interactive PATH prompt on headless runners). |
| 2026-07-06 | `89a7a89` | Third game (TankTillTusen) onboarded to the gate scope; its screen selector exempted like the other two. |

### The gate mechanics (why it's a *closed* loop)

- CH ≥ 9.4 hard-absolute, threshold a single const in `codehealth.sh`.
- Local: Stop hook scores only touched files (pre-existing debt never blocks unrelated work). CI: full scan = authoritative.
- Failure = `exit 2` → blocks → agent self-corrects → durable lesson appended to `memory/code-health.md`. Threshold triggering an automated response, machine-fired — ACMM's L3→L4 mechanism, which is why the re-assessment says "early L4 on the code-health axis" (single axis, human-authored bar; not full L4).

### Baseline (2026-07-02, cs 1.0.33): 23 scorable prod files, 7 below 9.4

GameEndpoints.cs 7.92 · Lots.cs 8.09 · Questions.cs 8.09 · MEM Decider.cs 8.58 · AuctionEndpoints.cs 8.64 · AuctionScreens.cs 9.38 · GameScreens.cs 9.38. Backend mean 9.38, frontend mean 9.67.

### Path A — refactored genuine debt (score bumps with the lesson learned)

| File / function | Before → after | Fix + läxa |
|---|---|---|
| `Decider.PickBalanced` (MEM) | 8.58 → 9.68 | cc=15 orchestration inlining bucketing + dedup + topic-cap + deficit-fill. Extracted helpers + a small `BandPicker` class owning the cohesive mutable state. **Läxa:** a stateful multi-guard accumulator → its own small class beats one big loop. |
| `GameEndpoints` / `AuctionEndpoints` | 7.92 / 8.64 → passing | Giant Map lambdas → one private static handler per route. **GOTCHA:** naive extraction *dropped* the score 7.92 → 7.45 (DI services became 9 explicit args → "Excess Number of Function Arguments" + duplication). Real fix: `[AsParameters]` deps record + a `RenderContext` record. **Läxa:** extraction that spreads args is a net loss — bundle args into a record first. |
| `ReadRows` CSV tokenizer (Lots/Questions) | 8.09 → 8.79 (+ rules-config for the rest) | Flattened nesting=4, extracted `ReadQuoted`/`IsLineBreak`/`SkipPairedLf`. **GOTCHA:** merging two guards into one `if (a \|\| b \|\| (c && d))` re-added a Complex Conditional — kept the two-if form. |
| `Puzzles.cs` Solver (Tank, 2026-07-06) | 8.39 → 9.53 | Recursive brute-force with cc=12, nesting=4. Extracted `Combine`/`Without`, bundled mutable accumulators into a record, derived a param instead of threading it. **Läxa:** recursion threading many params → bundle state in a record. |

### Path B — the honest exclusions (never lowered the global 9.4)

Two mechanisms, both documented, both visible:

1. **Scoped rules-config** (`.codescene/code-health-rules.json`, each override pinned by path glob so every other file keeps the strict rule): the exhaustive `Decide` command switch (cc=11) and the `Describe`/`RenderState` dispatch switches are *constitution-mandated* — exhaustive pattern matching with no default arm is the design, so the cyclomatic warning is raised for exactly those files. Same for the CSV string-boundary files (Primitive Obsession disabled there: strings at a parse boundary are the point).
2. **Outright exemptions at 9.38**: `GameScreens.cs`, `AuctionScreens.cs`, `TankScreens.cs` — the per-screen exhaustive render dispatch. Splitting the switch scatters it across files without removing a single branch. Grandfathered via `EXEMPT_RE` in the gate script; still shown in every `--report` run.

Rule of thumb (from `memory/code-health.md`): refactor when complexity is real and divisible; use rules-config when the flag fights a deliberate design. **Never split a constitution-mandated exhaustive switch and never lower the global 9.4.**

### End state

23 prod files, mean ≈ 9.91, 21 at 10.0, gate green. Gate proven both ways: config applied → exit 0; config removed → exit 2 naming the files.

### ACMM delta in one line

> 2026-06-18: "the loops are one-way (human→AI) and human-interpreted, not closed."
> 2026-07-02: "a threshold triggering an automated response, machine-fired — the repo's first genuinely closed loop."

Source docs: [[acmm]] (both verdicts, superseded one kept inline), commit messages `3ac5a21`, `190491d`, `ab6d52e`.
