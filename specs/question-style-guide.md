# Frågestilguide — Mer eller Mindre

Detta är **rubriken**: feel-expertens kunskap om vad som gör ett bra jämförelsekort.
Författar-agenten (`fragesattare`), fact-check-agenten (`faktagranskare`) och
språkgranskaren (`sprakgranskare`) läser denna fil varje körning. Itereras tills leken
känns rolig över hela spektrat.

Pipeline per kategori: `fragesattare` (batch ~25–30) → `faktagranskare` (verifiera värden +
riktning, fyll källa+år) → `sprakgranskare` (putsa fråga + differensfråga, rör ej värden) →
kurator/användar-snitt → ackumulera behåll.

## Spelet i en mening

Vårt spel är ett **jämförelsespel**, inte ett "tal 0–100"-spel. Varje kort är ett **par**
(sakA vs sakB). Spelaren gissar:
1. **Riktning** — är sakA mer eller mindre än sakB? (knappar [Mer]/[Mindre])
2. **Skillnadens storlek** — hur stor är den råa skillnaden, i kortets egen enhet?

Systemet normaliserar skillnaden i hemlighet (spelaren ser aldrig facit-värdena). Staplarna
i `QuestionScreen.razor` visar proportionen: hög stapel = 100 %, kort stapel =
`(100 − normaliserad)%`. Man gissar med **känsla för proportion**.

## Hemlig matematik (designa TILL den)

`ScoreQuestion` i `Decider.cs`:

```
mx = max(värdeA, värdeB)
correctDifference = round(|värdeA − värdeB| / mx × 100)   # klampad 0–100
```

Beräkna detta i huvudet/på papper NÄR DU SKRIVER varje kort — det avgör vilket
**svårighetsband** kortet hamnar i:

- **Extrem ratio** (elefant 6000 vs blåval 150000 → 96): dramatisk stapel, "slå slidern i
  topp". Kul EN gång som wow, tråkigt om varje kort är så.
- **Liten ratio** (Danmark 5,9 vs Norge 5,5 → 7; Portugal 10,3 vs Sverige 10,5 → 2):
  "slider-svett", riktningen är fällan. Här bor finliret.

En bra lek är en **medveten fördelning** över hela spektrat.

## Svårighetsband (mål-fördelning per batch)

Beräkna `round(|A−B|/max(A,B)*100)` för varje kort och sikta på:

| Band | Norm | Andel | Känsla |
|---|---|---|---|
| Riktningsfälla / nagelbitare | 0–20 | ~15 % | Nära värden — RIKTNINGEN är spelet |
| Slider-svett | 20–60 | ~40 % | Proportionsgissning, finliret |
| Tydligt glapp | 60–85 | ~30 % | Klart större, men hur mycket? |
| Wow / extrem | 85–100 | ~15 % | Dramatisk stapel, wow-faktor |

Rapportera band-histogrammet per batch. Justera tills fördelningen matchar målet.

## Vad gör ett bra par

- **Båda sakerna igenkännbara** för en familj / 14+ (inte nischade trivia-objekt).
- **Jämförelsen icke-trivial / överraskande / debatterbar** — gärna sådan man skulle
  diskutera vid matbordet.
- **Riktningen ibland kontraintuitiv** — argumentsfröet. Den uppenbara gissningen ska
  ibland vara fel.
- **Samma enhet** på båda sidor.
- **En tydlig siffra per sida** (inget "ca 3–5").
- **Verifierbar** mot en pålitlig källa.

## Riktningsbalans

~50/50 om sakA eller sakB är störst (varierande facit, så spelaren inte kan gissa
mönstret). **sakA = det naturliga subjektet** i frågan; knapparna är fasta [Mer]/[Mindre].
Konvention från `CLAUDE.md`: formulera frågan så sakA är subjektet, men låt verkligt
facit avgöra vem som är störst — variera medvetet.

## Stabilt vs volatilt

- **Prioritera stabila siffror**: höjder, livslängder, avstånd, ytor, diametrar — ändras
  inte år till år.
- **Volatila siffror sparsamt** (befolkning, försäljning, streams, ekonomi) och **ALLTID
  årtals-pinnade** i frågetexten eller källan (t.ex. "(2024)"). Volatilt-blandat-kategorin
  är liten med flit.

## Dedup / variera enheter

Sprid enheterna över HELA leken — inte 200 kort med "yta i km²". Variera: m, km, kg, år,
km/h, kcal, dygn, milj inv, platser, hk, ton, st. Flagga dubbletter (samma par eller
nästan-samma jämförelse).

## CSV-format (sv-SE)

```
fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga
```

- **Separator** `;`, **decimal** `,` (sv-SE).
- **UTF-8 med BOM** (`EF BB BF` först i filen) — annars läser svensk Excel filen som
  Windows-1252 och åäö blir skräp. Parsern strippar BOM, så pack-CSV:n får också ha BOM.
- **RFC4180-citat** om ett fält innehåller `;` — omge med `"..."`.
- **`fråga`** = naturlig svensk mening, sakA som subjekt, t.ex.
  "Har Danmark större eller mindre befolkning än Norge?".
- **`differensfråga`** = naturlig mening om den råa skillnaden, t.ex.
  "Hur många miljoner invånare skiljer det?".
- **`värdeA`/`värdeB`** = råa exakta författarvärden (decimal), ALDRIG ett 0–100-tal,
  ALDRIG förräknat facit.
- **Inga källkolumner i pack-CSV:n** — källa+år lever i sidecar `<kategori>.källor.csv`
  (nyckel = frågetext). Domänschemat är orört; parsern är header-mappad och fail-fastar på
  saknad/extra-mappad kolumn.

## Bra / dåliga exempel

**Bra** (norm 7, riktningsfälla, stabil/volatil-pinnat):
```
Har Danmark större eller mindre befolkning än Norge? (2024);Danmark;Norge;5,9;5,5;miljoner invånare;Hur många miljoner invånare skiljer det?
```

**Bra** (norm 96, wow, stabil):
```
Väger en afrikansk elefant mer eller mindre än en blåval?;Afrikansk elefant;Blåval;6000;150000;kilo;Hur många kilo skiljer det?
```

**Dåligt** — okänd sak: "Är Gangkhar Puensum högre eller lägre än K2?" (få känner Gangkhar Puensum).
**Dåligt** — olika enhet: jämför vikt mot längd.
**Dåligt** — trivialt: "Är solen större eller mindre än ett äpple?" (norm ~100, men noll spänning).
**Dåligt** — ovärderbar/volatil utan år: "Hur många följare har X på Instagram?".
**Dåligt** — luddig siffra: "ca 3 000–4 000 arter".
