# Ralph task — Familj PROD deck (1085-card, 21-question)

Build the **prod** `familj` deck → `src/MerEllerMindre.Domain/data/packs/familj.csv` at
**EXACTLY 1085 cards** (guarded by `EveryFullDeckIsExactly1085Cards`, 21-question round).
Spec + recognition bar: `question-staging/familj/BRIEF.md`. Full research:
`docs/research/familj-design-brief.md`.

## Goal
`familj.csv` at exactly 1085 clean cards; `dotnet test` green; band quota **5/25/40/30**;
direction ~50/50; item-cap 4; 0 dubbletter; 0 same-entity-smell; no price/"kostnadsfritt" words.

## Recognition bar (HÅRT — Familj > alla-aldrar)
Both a 6-year-old AND a grandparent must recognize sakA + sakB. Categories: vanliga djur,
vardagsföremål, mat & godis, kroppen, väder, fordon, kända landmärken, planeter & rymd,
dinosaurier, känd sport. **Förbjudet:** band-diskografier, antal anställda, museibesök,
huvudstadsfolkmängd, länder-efter-yta, dagsfärsk popkultur, abstrakta/vetenskapliga enheter
(hertz, kromosomer, volymprocent), temporala riktningskort (årtal+före/efter).
Enheter: kilo, gram, meter, centimeter, år (ålder), km/h, antal/stycken, °C, kalorier.

## Band quota 5/25/40/30 — tippad mot tydliga glapp/wow
Färre nära-lika riktningsfällor (0–20), fler tydliga glapp (61–85) + wow (86–100).
`--targets 5,25,40,30`. Mekanism: mildare bandkurva + högre igenkänning, INTE enklare matte.

## Themes (≈108 cards each → ~4 batches of ~27; SEQUENTIAL, finish + report before next)
djur · vardagsforemal · mat-godis · kroppen · vader · fordon · landmarken · rymden ·
dinosaurier · sport  (dir = `question-staging/familj/<tema>/` or `<tema>-NN.csv` flat)

## Hard rules (same as alla-aldrar build)
- 7-col sv-SE CSV + BOM; header `fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga`.
- `--key question` for report/check/merge.
- Direction ~50/50 (sakA need NOT be largest for symmetric comparatives).
- Real entities/figures only — never invent. Volatile numbers year-pinned in sidecar.
- ItemCap 4 → reuse an entity ≤~2× per batch.

## Each iteration (ONE batch at a time — NEVER fan out parallel agents; budget brake)
1. **fragesattare** — one ~27-card batch, Familj brief + theme. Write `<tema>-NN.csv` +
   `.källor.csv` sidecar to `question-staging/familj/`. Append-only, unique NN.
2. **faktagranskare** (WebSearch) — verify värdeA/B + direction; fix number in place; drop
   unpinnable; fill source+year. PRESERVE sakA/sakB order.
3. **sprakgranskare** — polish fråga + differensfråga (not värden/enhet/riktning).
4. **tydlighetsgranskare** — reject genuine ambiguity only.
5. `dotnet run tools/pack.cs -- report --staging --dir question-staging/familj --key question --targets 5,25,40,30`
   → steer next batch (short band → closer/wider pairs; skew → flip; over cap → swap entities).
6. When `check --staging --dir question-staging/familj --min 1085 --targets 5,25,40,30` exits 0:
   `merge --dir question-staging/familj --key question --out src/MerEllerMindre.Domain/data/packs/familj.csv`,
   trim to exactly 1085, `dotnet test` green, add `["familj"]="Mer eller Mindre – Familj"` to
   `DisplayNameOverrides` (Questions.cs), commit.

## Stop
Write `FAMILJ-DONE` when familj.csv = exactly 1085 clean cards AND `dotnet test` green.

## Escape hatches
- Pool short of distinct family-recognizable entities for a band → STOP, report the gap, never invent.
- Same band short two iterations → change strategy; if stuck write `FAMILJ-BLOCKED`.
- Usage-limit interruption: loop stops; staging = state, restart resumes.
