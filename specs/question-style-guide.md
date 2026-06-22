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
- **Namnge alltid arten** (kategori-apposition) för nischade egennamn så man vet VAD som
  jämförs: "Uranusmånen Oberon", "dvärgplaneten Ceres", "floden Niger", "bergstoppen Lhotse".
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

## Alla åldrar (lättare lek)

Gäller NÄR pack = `alla-aldrar` (delta mot basfilosofin ovan — allt annat i guiden står kvar).

- **Bandmål 10/35/35/20** (inte 15/40/30/15): färre 0–20-riktningsfällor, fler "tydligt
  glapp" + wow. Mät mot detta: `report --staging --dir question-staging/alla-aldrar
  --targets 10,35,35,20`.
- **Målgrupp 13–83, tvärgenerationellt**: maximera igenkänning. Båda 13- och 83-åringen ska
  känna saken → tidlösa kategorier (djur, kropp, mat, landmärken, planeter, svenska
  storheter). Undvik dagsfärsk popkultur som bara ena änden känner.
- **Jämn mix ~50/50** globalt-känt ↔ svenskt/nordiskt.
- Lättare via BÅDE igenkännbara saker OCH mildare bandkurva — inte enklare matte.

**Bra** (igenkänt par, tydligt glapp): "Är en giraff högre eller lägre än en elefant?"
**Bra** (svenskt, tidlöst): "Är Vänern större eller mindre än Vättern till ytan?"
**Dåligt** (off-audience): "Har låt X fler eller färre Spotify-streams än låt Y?" (dagsfärsk,
bara ena generationen känner båda).

## Loggor (logga-läge)

Gäller NÄR pack-slug börjar på `loggor-` (delta mot basfilosofin ovan). Loggleken visar
loggor på frågeskärmen och DÖLJER namnen tills resultatet — så frågestammen MÅSTE vara
namnfri och `sakA`/`sakB` får aldrig läcka ut i frågan.

- **`fråga` + `differensfråga` är NAMNFRIA och enhetliga** över hela leken (samma stam för
  alla kort i samma metrik):
  - Ålder: `fråga` "Vilket av märkena är äldst?", `differensfråga` "Hur många år skiljer
    dem åt?", enhet `år`.
  - Länder: `fråga` "Vilket märke finns i flest länder?", `differensfråga` "Hur många länder
    skiljer det?", enhet `länder`. (Butiker för kedjor: enhet `butiker`.)
- **`sakA`/`sakB` = EXAKTA namn ur `data/logos/logos.csv` vars png finns på disk.** Annars
  returnerar `LogoCatalog.UrlFor` null → trasig render. Filtrera kandidaterna mot disk.
- **Metrik = ålder (`2026 − grundningsår`) eller antal länder/butiker.** ALDRIG
  grundningsår rakt av — det är degenererat: `|1943−2006|/2006 ≈ 0,03` → norm ~3 → varje
  kort hamnar i band 0. Ålder ger ratio-spridning (IKEA 83 vs Spotify 20 → norm 76 → band 2).
  Stabila metriker (ej volatil omsättning/streams).
- **Två lekar = två svårighetsgrader, BARA via pack-val:**
  - `loggor-alla-aldrar-1` — bandmål **10/35/35/20**, pool = **konsumentmärken** (hushållsnamn
    13- OCH 83-åringen känner: sweets/drinks/snacks, restaurants, toys, games/music/film,
    electronics, sports, fashion, retail).
  - `loggor-blandat-1` — bandmål **15/40/30/15**, pool = **B2B/industri** (obskyra:
    industrials/holdings, real estate, pharma, finance/insurance, semiconductors/enterprise,
    chemicals/energy, logistics, SaaS/components).
  - Poolerna (mestadels) disjunkta — igenkänning är den andra svårighetsaxeln.
- **`--key pair` vid report OCH merge** (alla kort delar EN frågestam → dedup/dupflagg på
  questionText skulle kollapsa leken till 1 kort). Mät:
  `report --staging --dir question-staging/loggor-alla-aldrar --targets 10,35,35,20 --key pair`.
- `ItemCap = 4` gäller (≥543 distinkta märken/lek; korpusen 2034 png räcker väl).
