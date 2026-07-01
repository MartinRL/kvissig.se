# Ralph task — three 175-card mini decks (hund → elbil → fotboll)

Build three **mini (175-card, 7-question)** decks via the question pipeline, SEQUENTIALLY:
hund → elbil → fotboll. Terminate when ALL three are merged at **exactly 175 cards** and
`dotnet test` is green.

## Current state (resume here)
- `hundraser-mini.csv` exists at **183 cards** → TRIM to exactly 175 (drop weakest/most-borderline).
  Staging `question-staging/hund/` has batches 01–05.
- `elbil` + `fotboll` staging are EMPTY → full build needed, no pack yet.

## Goal
Three packs in `src/MerEllerMindre.Domain/data/packs/`, each at EXACTLY 175 cards:
`hundraser-mini.csv`, `elbil-mini.csv`, `fotboll-mini.csv`.

| Deck dir (`question-staging/`) | Output pack | Targets | Pool / units |
|---|---|---|---|
| `hund`    | `hundraser-mini.csv` | `15,40,30,15` | ≥88 SKK-raser; mankhöjd cm, vikt kg, livslängd år, SKK-reg/år (pinnat år), pris kr |
| `elbil`   | `elbil-mini.csv`     | `15,40,30,15` | ≥88 elbilsmodeller sv. marknad 2023–2026; räckvidd km (WLTP), pris kr (pinnat modellår), 0–100 s, hk, batteri kWh |
| `fotboll` | `fotboll-mini.csv`   | `15,40,30,15` | Allsvenskan-klubbar + spelare; marknadsvärde mkr (pinna säsong+Transfermarkt+år), grundadår-som-ålder, mål/säsong, publiksnitt/säsong, SM-guld |

Read `specs/question-style-guide.md` before each batch.

## Hard rules
- **7-kol sv-SE CSV + BOM**, headers `fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga`.
- **`--key question`** for report/check/merge.
- **ItemCap = 4** → need ≥~88 distinct entiteter/tema (else cap-krock). Reuse an entity at
  most ~2× per batch.
- **Direction ~50/50, NOT "sakA always largest":** for symmetric comparatives (större/mindre,
  högre/lägre) sakA need NOT be the largest. Assign so the larger value is sakA on ~half the
  cards, sakB on the other half. `tydlighetsgranskare`/`faktagranskare` must NOT flag "sakA
  mindre" as a convention breach and must NOT swap sakA↔sakB.
- **Volatile numbers are year-pinned:** elbil pris/räckvidd → modellår; fotboll marknadsvärde →
  säsong + källa (Transfermarkt) + år, written in the sidecar + question text.
- Author from REAL entities/figures only — never invent facts or entities.

## Each iteration (themes SEQUENTIAL hund → elbil → fotboll; finish one to 175 + merge + commit
before starting the next; the budget brake beats interleaving)

Run ONE theme at a time toward 175. NEVER fan out 12 parallel agents (that blew the usage
limit last run). The faktagranskare verifies ONLY its own ~27-card batch (≈ one WebSearch per
card). Staging = state: a limit hit mid-run is resumable, no rework.

1. **fragesattare** — author one ~27-card batch with the theme brief. Continue batch numbering
   (`batch-NN.csv`); write batch CSV + `.källor.csv` sidecar to that theme's staging dir.
   Append-only with a unique NN so a retry never double-writes.
2. **faktagranskare** (WebSearch = ground truth) — verify värdeA/B + implied direction against a
   source; fix the NUMBER in place if wrong; drop cards whose value can't be pinned; fill
   source+year in the sidecar. PRESERVE sakA/sakB order — never swap.
3. **sprakgranskare** — polish `fråga` + `differensfråga` (do NOT touch värden/enhet/riktning).
4. **tydlighetsgranskare** — reject only genuine ambiguity (temporal-utan-magnitud, inverterat
   komparativ). Do NOT flag "sakA mindre".
5. Run `dotnet run tools/pack.cs -- report --staging --dir question-staging/<tema> --key question`
   → read band/riktning/cap/dubbletter; steer the next batch (short band → closer-value pairs,
   skewed direction → flip pairs, over cap → swap entities).
6. When `dotnet run tools/pack.cs -- check --staging --dir question-staging/<tema> --min 175`
   gives **exit 0**:
   `dotnet run tools/pack.cs -- merge --dir question-staging/<tema> --key question --out src/MerEllerMindre.Domain/data/packs/<pack>.csv`
   Trim to exactly 175 if merge yields more (drop the weakest/most-borderline). Run
   `dotnet test` — must stay green (`EveryPackHasCleanCards` guards these; mini slugs are
   exempt from `EveryFullDeckIsExactly1085Cards`). Then commit so a later limit hit is resumable.

## Stop
Write a file `CARDS-DONE` (and output the text `CARDS-DONE`) ONLY when all three packs are
merged at exactly 175 cards AND `dotnet test` is green.

## Escape hatches
- Same band short two iterations in a row → change strategy, don't repeat; if still stuck write
  a file `RALPH-BLOCKED`.
- Pool runs short of distinct entities for a band/theme → STOP and report the gap (never invent).
- On usage-limit interruption: the loop stops; restarting resumes from staging.
