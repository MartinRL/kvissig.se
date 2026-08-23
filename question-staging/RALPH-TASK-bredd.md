# RALPH-TASK: bredda blandade prod-pack — musik + geografi

Mål: lägg till musik-kort i alla tre blandade prod-packen och geografi-kort i alla-aldrar +
familj, genom SWAP (packen är låsta på exakt 1085 kort av `EveryLivePackIs1085Clean`).
Nya kort ERSÄTTER gamla, de läggs aldrig till netto.

**EN sekventiell loop. Inga parallella loopar.** Pack-ordning: familj → alla-aldrar →
mer-eller-mindre. Kolla `git log -- <batchfil>` innan författande (unclaimed = kör).

## Scope

| Pack | musik | geografi | brief |
|---|---|---|---|
| familj | musik-01, musik-02 (~27 st) | geografi-01, geografi-02 (~27 st) | `question-staging/familj/BRIEF-bredd.md` |
| alla-aldrar | musik-01, musik-02 | geografi-01, geografi-02 | `question-staging/alla-aldrar/BRIEF-bredd.md` |
| mer-eller-mindre | musik-01, musik-02 | INGEN (redan ~30 % geo) | `question-staging/mer-eller-mindre/BRIEF-bredd.md` |

Live-packen: `src/MerEllerMindre.Domain/data/packs/<pack>.csv`.

## Per pack, per batch (~27 kort)

1. **Baseline:** `dotnet run tools/pack.cs -- report src/MerEllerMindre.Domain/data/packs/<pack>.csv`
   — notera bandhistogram, direction-split, HELA at-cap/over-cap-listan (avoid-list).
2. **Författa:** `fragesattare`-agenten med BRIEF-bredd.md som brief. ~27 kort, riktning
   ~50/50 FRÅN START. Förvanska ALDRIG siffror för att träffa band — byt par i stället.
   Skriv batch-CSV + sidecar `<batch>.källor.csv` (`fråga;källa;år`) samtidigt.
3. **QA-pipeline (OBLIGATORISK ordning):**
   a. `faktagranskare` — WebSearch-verifiera VARJE värde + riktning; fyll källa + år i sidecar.
   b. `sprakgranskare` — endast fråga/differensfråga.
   c. `tydlighetsgranskare` — temporal-utan-magnitud förkastas. OBS: "regel 3"-flagg på
      sakB-störst-kort IGNORERAS (giltiga riktningskort, false flag).
   VARJE strykning av rad i batch-CSV MÅSTE stryka SAMMA rad i sidecar (positionell join).
4. **Batch-check:** `dotnet run tools/pack.cs -- report --staging --dir question-staging/<pack>`
   — 0 dups, 0 over-cap bland nya kort, rimliga band.
5. **Swap i live-packet:** stryk EXAKT lika många rader som batchen tillför, append nya.
   Urvalsregler i ordning:
   (a) kort vars items ligger över cap 4 pack-wide (mer-eller-mindre har 67 over-cap-items!),
   (b) kort ur dominerande kategori (djur),
   (c) bevara direction ~50/50 och bandhistogram nära pack-target
       (mer-eller-mindre 15/40/30/15, alla-aldrar 10/35/35/20, familj 5/25/40/30).
   Nya rader: 7 kolumner, sv-SE (`;`, `,`-decimal), UTF-8 BOM bevarad. EXAKT 1085 rader
   data efter (header oräknad). Redigera med skript/csv-medvetet, EJ sed (quotade fält).
6. **Grind:** kör rapporten på live-packet igen (1085, 0 dups, 0 same-entity-smell,
   over-cap ej fler än baseline) + `dotnet test` grönt.
7. **Commit:** `wip(cards): bredd <pack> <kategori> batch NN` (batch-CSV + sidecar +
   live-pack i samma commit).

## Hårda regler

- Ordet "gratis" får ALDRIG förekomma. Ingen em dash (—) i svensk copy/frågetext.
- Musik = musikens entiteter (instrument/artister/låtar/verk), INTE byggnader,
  INTE artistfödelseår-som-årtal. Geografi = naturgeografi + länder, INTE landmärken.
- Stabila mått only (se resp. brief). Inga streams/månadslyssnare utan årspinne.
- Sidecar-join är positionell: rad N i batch-CSV ↔ rad N i sidecar. Alltid i synk.
- Rör INGA andra filer än batch-CSV:er, sidecars och de tre live-packen.

## DONE-villkor

Alla tre packen uppdaterade (familj +4 batchar, alla-aldrar +4, mer-eller-mindre +2),
exakt 1085 kort vardera, `dotnet test` grönt, allt committat.
→ skriv completion-promise **BREDD-DONE**.
Blockerad (faktaverifiering omöjlig, test rött som inte går att fixa inom scope, etc.)
→ skriv **BREDD-BLOCKED** + orsak.
