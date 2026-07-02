# Ralph task — grow Blindbudet mini pool to 175 lots

Grow `src/Blindbudet.Domain/data/auction-packs/blindbudet-mini.csv` from its current lots to
**exactly 175 lot rows**. This is the concept-scale ("mini") pool; the Decider now samples
`Decider.MiniAuctionSize` (7) lots from it per game, so a big, varied pool = replay variety.
Read `CLAUDE.md` first; obey the "gratis"-word ban.

## Format (auction CSV — NOT MEM's 7-col schema)
- **4 columns, sv-SE dialect**: `beskrivning;santVärde;tema;enhet` (`;` separator, `,` decimal).
- Keep the existing header row; append lot rows below it. UTF-8 (the parser strips a BOM if present).
- `beskrivning` = a full, natural Swedish noun phrase naming ONE concrete thing
  (e.g. `Höjden på Mount Everest över havet`, `Antal ben i en vuxen människokropp`).
- `santVärde` = the ONE real numeric worth of that thing (sv-SE decimal, e.g. `42,195`).
- `tema` = a short Swedish theme label (Geografi, Rymden, Sport, Kroppen, Musik, Landmärken,
  Djur, Historia, Teknik, Mat, Byggnader, Natur, Fordon, …).
- `enhet` = the unit (meter, km, stycken, kg, år, kr, …).

## Lot quality (auction-specific — there is NO direction/difference/bands here)
- Every lot is a **recognizable** thing with a **real, WebSearch-verifiable** value a layperson
  can reason about and bid on. No obscure nerd-trivia; no invented facts or entities.
- **Spread magnitudes** (single digits → millions) and **spread themes** so bidding has variety —
  don't stack 40 "height in meters" lots.
- **Year-pin volatile figures** in the beskrivning (prices, populations, records), e.g.
  `Sveriges folkmängd 2024`. Pick stable facts where possible.
- **No duplicate beskrivning and no duplicate underlying fact.**

## PRIMARY source = the already-fact-checked MEM packs (reuse, don't re-verify)
MEM's comparison cards already carry TWO real, faktagranskare-verified magnitudes each. **Mine
them into single-value auction lots — this is the cheapest, most reliable path (no re-search):**
- Merged packs: `src/MerEllerMindre.Domain/data/packs/*.csv` (familj = 1085 cards, plus the
  mini decks). WIP/staged candidates: `question-staging/**/*.csv` (each has a `.källor.csv`
  source sidecar — reuse those figures too).
- Each MEM row `fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga` → up to two lots:
  `sakA @ värdeA` and `sakB @ värdeB`, in `enhet`.
- Derive `beskrivning` from the fråga's dimension verb: `Väger…` → `Vikten av en/ett <sak>`;
  `längre/kortare` → `Längden på en/ett <sak>`; `högre/lägre` → `Höjden på …`; `snabbare` →
  `Toppfarten för …`; etc. Keep the sak's article/number natural. `enhet` maps straight
  (kilo→kg stays kilo, centimeter, meter, …). Pick a `tema` from the card's subject.
- These values are pre-verified: **do NOT WebSearch them again.** Only fix an obvious typo.
- **New blindbudet-suited lots are also welcome** (single well-known facts a layperson can bid
  on) — for those, WebSearch-verify before adding.
- Dedup on `beskrivning`; keep ONE representative lot per distinct item/dimension; spread
  themes + magnitudes (don't stack 40 animal-weights-in-kg).

## Each iteration (~25 lots, sequential — the budget brake beats fanning out agents)
1. Add ~25 new lots — mostly mined from the MEM packs above, plus any new WebSearch-verified
   blindbudet-suited facts.
2. For MEM-mined values, trust the source (no re-search). For NEW hand-authored lots, **verify
   every `santVärde` with WebSearch against a source BEFORE adding it.** Never guess.
3. Append the verified rows to `blindbudet-mini.csv` (header + existing rows intact).
4. Dedup beskrivning:
   `tail -n +2 src/Blindbudet.Domain/data/auction-packs/blindbudet-mini.csv | cut -d';' -f1 | sort | uniq -d`
   must print nothing.
5. Count: `tail -n +2 src/Blindbudet.Domain/data/auction-packs/blindbudet-mini.csv | grep -c .`
6. `dotnet test` must stay green (the web tests boot the app and load this CSV via
   `FileSystemAuctionPackCatalog` — a malformed pack fails the suite).
7. Commit `wip(lots): blindbudet-mini +N (total M/175)` so a limit hit is resumable.
   Trim to exactly 175 (drop the weakest/most-borderline) if you overshoot.

## Stop — write file `BLINDBUDET-MINI-DONE` (and output `BLINDBUDET-MINI-DONE`) ONLY when
- The CSV has **exactly 175 lot rows** (176 lines incl. header),
- dedup check prints nothing,
- `dotnet test` is green.

## Escape hatches
- Stuck on the same problem two iterations running → change approach, don't repeat; if still
  stuck write a file `RALPH-BLOCKED` and stop.
- Can't find enough distinct verifiable facts → STOP and report the gap (never invent).
- Usage-limit interruption: the loop stops; restarting resumes from the committed CSV.
