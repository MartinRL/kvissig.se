---
status: Accepted
type: game-design
created: 2026-06-21
---

# ADR 012: Difficulty-Banded Deck Balancing

## Context
A live question pack is large — `alla-aldrar.csv` (~1322 cards) and
`mer-eller-mindre.csv` (~1085 cards) — but a single game only plays 21 of them
(ADR 013). For the game to *feel* right, the pack itself has to span the whole
difficulty spectrum: a deck of only near-impossible "wow" cards is exhausting, a
deck of only nail-biters is fatiguing. So a pack is **deliberately balanced** across
four difficulty bands with target quotas, and curated against a stated philosophy.

This is a product / game-rule decision (`type: game-design`), not a structural one —
it shapes how a pack *plays*, not how the system is built.

## Decision
Balance every pack against a **difficulty normalization** computed from the raw card
values, sorted into **four bands with target proportions**, and curate cards against
a "closeness is a feature" rule.

### One normalization formula
Difficulty is the same 0–100 normalization scoring uses — there is exactly ONE
implementation:

```csharp
// src/MerEllerMindre.Domain/Decider.cs
public static byte NormalizeDifference(decimal value, decimal mx) =>
    mx <= 0 ? (byte)0
    : (byte)Math.Min(100m, Math.Round(value / mx * 100, MidpointRounding.AwayFromZero));
```

with `value = |valueA − valueB|` and `mx = max(valueA, valueB)`, clamped at 100. This
single function is consumed by scoring (`ScoreDifference`), by the draw's `BandOf`
(ADR 013), and by the `tools/pack.cs` report — never re-implemented.

### Four bands + target distribution
Thresholds `[20, 60, 85]`, default target quotas:

| Band | Norm | Andel | Känsla |
|---|---|---|---|
| Riktningsfälla / nagelbitare | 0–20 | 15 % | Nära värden — RIKTNINGEN är spelet |
| Slider-svett | 21–60 | 40 % | Proportionsgissning, finliret |
| Tydligt glapp | 61–85 | 30 % | Klart större, men hur mycket? |
| Wow / extrem | 86–100 | 15 % | Dramatisk stapel, wow-faktor |

Pack-specific override: **`alla-aldrar` = 10 / 35 / 35 / 20** (fewer nail-biters,
broader mid/high — it is the easiest, most general pack so it leans less on the
direction-trap band). Sources: `specs/question-style-guide.md:42-52`,
`tools/pack.cs:22-24` (the report's `--targets` option carries the override).

### Curation philosophy
> **Närhet är en FEATURE, inte ett fel.** Two cards with near-identical values
> (Shanghai Tower 632 m vs Tokyo Skytree 634 m) are GOOD cards — the direction is the
> game.

Cut a card ONLY when **both** sides fall below the pack's recognition level, or the
card is incomprehensible. Never cut for closeness, and never cut just because ONE side
is niche (name the species/category in apposition instead). `alla-aldrar`, being the
easiest pack, holds the highest recognition bar. Source: memory `question-curation.md`
+ `specs/question-style-guide.md`.

### Staging → merge pipeline
```
question-staging/alla-aldrar/<kategori>-N.csv   (frågesättare)
  → faktagranskare (+ .kilder sidecar)
  → språkgranskare
  → tools/pack.cs report --staging        (band-histogram + item-cap check)
  → tools/pack.cs merge --out <livepack>  (dedup on questionText; refuses the live
                                            pack without --force)
  → data/packs/<pack>.csv                 (live)
```

## Rationale
- **One formula, no drift**: band membership and scoring agree because they call the
  same `NormalizeDifference`.
- **Tunable per pack**: quotas are the design knob; `alla-aldrar` overrides without
  code change via `--targets`.
- **Closeness preserved**: the curation rule protects exactly the cards (near values)
  that make the direction guess interesting, instead of discarding them as "errors".
- **Author-friendly**: authors type real figures; the pipeline measures and reports —
  no mental normalization arithmetic.

## References
- ADR 005 (CSV catalog the bands are measured on).
- ADR 002 (Decider — home of `NormalizeDifference`).
- ADR 013 (the per-game draw that consumes these bands/quotas).

## Consequences
- A pack's "feel" is now an explicit, measurable target, not a vibe.
- **Known wart (deliberate):** the band *thresholds* `[20, 60, 85]` live in TWO places
  — `tools/pack.cs:22` (report) and `Decider.BandOf` (draw). Only the *formula*
  (`NormalizeDifference`) is single-source. Flagged, not fixed — extracting a shared
  threshold constant is YAGNI until a third consumer appears.
