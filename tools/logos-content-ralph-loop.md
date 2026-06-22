# Ralph loop — logo content decks

Feed this to `/ralph-loop`. Builds the two full **1085-card** logo decks via the
question pipeline. Terminates deterministically when BOTH decks are merged at 1085 cards.

---

Goal: two full decks in `src/MerEllerMindre.Domain/data/packs/`:

| Deck dir (`question-staging/`) | Output pack | Targets | Recognizability pool |
|---|---|---|---|
| `loggor-alla-aldrar` | `loggor-alla-aldrar-1.csv` | `10,35,35,20` | **consumer** brands (hushållsnamn) |
| `loggor-blandat`     | `loggor-blandat-1.csv`     | `15,40,30,15` | **B2B / industri** (obskyra) |

Read `specs/question-style-guide.md` (esp. "## Loggor (logga-läge)") before each batch.

Hard rules (baked in from the pilot):
- **Names:** `sakA`/`sakB` MUST be exact `name` values from `question-staging/loggor-pool-ondisk.csv`
  (the only logos with a png on disk). Copy char-for-char. The two decks' brand pools must be
  (mostly) **disjoint** — consumer brands in alla-aldrar, B2B/industri in blandat. The seed
  `tools/logos-seed.csv` `#`-section headers classify brands; use them to partition.
- **Metric:** AGE = `2026 − grundningsår` (write the AGE as värdeA/B, NEVER the founding year —
  it's degenerate → all band 0), or country/store count. Stable metrics only (no revenue/streams).
- **Namnfri stems**, identical within a metric: ålder → `fråga` "Vilket av märkena är äldst?",
  `differensfråga` "Hur många år skiljer dem åt?", enhet `år`. länder → "Vilket märke finns i
  flest länder?" / "Hur många länder skiljer det?" / `länder` (`butiker` for chains).
- **Direction ~50/50 (live pack is 52/48 — this is the real convention, NOT "sakA always
  largest"):** assign sakA/sakB so the OLDER (larger value) is sakA on ~half the cards and sakB
  on the other half. The namnfri stem has no "subject", so there is nothing forcing sakA largest.
  Do NOT make sakA always the oldest — that skews Direction to 100% Mer (gameable) and FAILS the
  report's direction check.
- **faktagranskare must PRESERVE the author's sakA/sakB order** — it only verifies/corrects each
  brand's age value and checks the implied larger-value direction is factually right for the
  brands as placed. It must NEVER swap sakA↔sakB to make sakA the oldest (that destroys the
  50/50). If a value is wrong, fix the number in place; do not reorder.
- ItemCap = 4 → need ≥543 distinct brands/deck; the 2032-name pool is ample. Reuse a brand at
  most ~2× per batch so variety stays high and no brand hits the cap from one batch.

Each iteration (pick the deck furthest from 1085 distinct pairs):

1. **fragesattare** — author one ~27-card batch with the deck's brief (pool, targets, the hard
   rules above). Continue batch numbering (`batch-NN.csv`); write batch CSV + `.källor.csv`
   sidecar to that deck's staging dir.
2. **faktagranskare** — verify each card's grundningsår/ålder (or country count) against a
   source; fix the NUMBER in place if wrong; drop cards whose year can't be pinned. Fill
   source+year in the sidecar. PRESERVE sakA/sakB order — do NOT swap to make sakA oldest
   (keeps the deck ~50/50; see hard rules).
3. **sprakgranskare** — light pass (stems are uniform + namnfria).
4. `dotnet run tools/pack.cs -- report --staging --dir question-staging/<deck> --targets <t> --key pair`
   from repo root. Read the histogram, direction split, items-over-cap, duplicate pairs.
   If a band is short, bias the next batch's brands toward it (close pairs → low bands, far →
   high). Drop/replace cards flagged over cap or as duplicate pairs.
5. When a deck's report shows **≥1085 cards, histogram within ~±5 % of target, direction ~50/50,
   0 items over cap 4, 0 duplicate pairs**, merge it:
   `dotnet run tools/pack.cs -- merge --dir question-staging/<deck> --key pair
   --out src/MerEllerMindre.Domain/data/packs/<pack>.csv`
   Then trim to exactly 1085 if merge yields more (drop the weakest/most-borderline pairs).
   Run `dotnet test` — must stay green (`EveryPackHasCleanCards` already guards these).

STOP when BOTH packs are merged at exactly 1085 cards. As the final step, remove the
`loggor-` exception in `src/MerEllerMindre.Domain.Tests/QuestionPackCsvParserTests.cs`
(`IsFullDeck` → return true for all packs) so `EveryFullDeckIsExactly1085Cards` guards the
logo decks too, then run `dotnet test` once more (must be green) and report final counts.

If the pool runs short of distinct brands for a band/deck before 1085, STOP and report the
gap — the loop authors from real brands, it does not invent logos.
