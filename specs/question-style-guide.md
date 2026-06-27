# Frågestilguide — Mer eller Mindre

Detta är **rubriken**: feel-expertens kunskap om vad som gör ett bra jämförelsekort.
Författar-agenten (`fragesattare`), fact-check-agenten (`faktagranskare`),
språkgranskaren (`sprakgranskare`) och tydlighetsgranskaren (`tydlighetsgranskare`) läser
denna fil varje körning. Itereras tills leken känns rolig över hela spektrat.

Pipeline per kategori: `fragesattare` (batch ~25–30) → `faktagranskare` (verifiera värden +
riktning, fyll källa+år) → `sprakgranskare` (putsa fråga + differensfråga, rör ej värden) →
`tydlighetsgranskare` (förkasta tvetydig riktning, rör ej värden) → kurator/användar-snitt →
ackumulera behåll.

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

## Otydlig riktning (tvetydiga kort)

Att **förstå frågan** är det viktigaste i hela spelet. Knapparna är fasta [Mer]/[Mindre], så
frågans jämförelseord måste peka samma håll som "mer" — annars kan en familj inte enas om vad
svaret betyder, även om varje siffra är sann. Ett sådant kort är **trasigt**.

Ett kort är **otydligt i riktning** om NÅGOT av:

1. **Temporal riktning utan magnitud** — värdet är ett **årtal/tidpunkt** OCH frågan jämför
   *när* (före/efter/inträffade/grundades/invigdes/patenterades/kom ut/byggår). "Mer" bär
   ingen intuitiv tidsriktning: större årtal = senare = *mindre* länge sedan, en inverterad
   och osynlig mappning. → **förkasta** (skriv om till äkta magnitud eller ersätt med ett
   icke-temporalt kort).
2. **Komparativord ≠ mer/mindre-intuition** — ordet ska peka samma håll som "mer"
   (äldre/högre/tyngre/längre/fler/större = mer). Inverterade/tvetydiga ord ("länge sedan",
   "för X år sedan" blandat med årtal) → flagga för omskrivning.
3. **Konventionsbrott** — `sakA` ska hålla det STÖRRE värdet OCH vara subjektet. Håller sakA
   det mindre värdet läses facit-meningen bakvänt → byt sakA/sakB.

**Avskräckande exempel (förkasta):** "Inträffade Stockholms blodbad före eller efter Gustav
Vasas trontillträde?" (1520 vs 1523) — temporal riktning, ingen magnitud, OCH sakA (blodbadet)
håller det mindre årtalet. Omöjligt att gissa rätt mot [Mer]/[Mindre] utan en dold regel.

**EJ tvetydigt:** höjd/vikt/yta/avstånd/längd/antal/livslängd/ålder-vid-händelse med naturligt
komparativ. `äldre/yngre` mappar rätt så länge värdet är **ålder i år** (större = äldre = mer)
— inte ett **årtal/byggår** (större = yngre, inverterat). "År f.Kr." är OK eftersom större
f.Kr.-tal = äldre = mer.

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
loggor på frågeskärmen och DÖLJER namnen tills riktningen är gissad — så frågestammen MÅSTE
vara namnfri och `sakA`/`sakB` får aldrig läcka ut i frågan. (Steg 1 = bara loggan; efter
riktningsgissningen visas namnet bredvid loggan, råvärden/facit först på resultatskärmen.)

**Blandade mått, INTE enmetrik.** Leken växlar mått runda för runda så frågan inte blir
likadan varje gång. Sex metriker, var och en med en **namnfri, enhetlig stam** (samma
`fråga`/`differensfråga`/`enhet` för alla kort i samma metrik):

| Mått | `fråga` | `differensfråga` | `enhet` |
|---|---|---|---|
| Ålder | Vilket av märkena är äldst? | Hur många år skiljer dem åt? | år |
| Länder | Vilket märke finns i flest länder? | Hur många länder skiljer det? | länder |
| Anställda | Vilket märke har flest anställda? | Hur många anställda skiljer det? | anställda |
| Omsättning | Vilket märke har störst omsättning? (ÅR) | Hur många miljarder kronor skiljer det? | miljarder kr |
| Börsvärde | Vilket märke har störst börsvärde? (ÅR) | Hur många miljarder kronor skiljer det? | miljarder kr |
| Varumärkesvärde | Vilket märke är värt mest som varumärke? (ÅR) | Hur många miljarder dollar skiljer det? | miljarder USD |

(Kedjor: använd `butiker` som enhet under Länder-måttet om butiksantal är det naturliga.)

- **Valuta/år:** båda sidor av ETT kort har samma enhet OCH samma år/källa — annars är de
  inte jämförbara. **Volatila mått (omsättning, börsvärde, varumärkesvärde) år-pinnas i
  `fråga`** (ersätt `(ÅR)` med t.ex. `(2024)`). Stabila mått (ålder, länder, anställda)
  behöver ingen pinne men ska ändå spegla en aktuell källa.
- **Metrik-val:** ålder = `nuvarande år − grundningsår` (ALDRIG grundningsår rakt av — det
  är degenererat: `|1943−2006|/2006 ≈ 0,03` → band 0 för varje kort). Ålder ger
  ratio-spridning (IKEA 83 vs Spotify 20 → norm 76 → band 2).
- **Sektor-koherens: båda märkena på ett kort ska tillhöra samma breda sektor** — bilmärke
  mot bilmärke, bank mot bank. Inte snävaste bransch utan BRED sektor; Scania/Tesla/SAS får
  mötas inom "Fordon & transport". Bred taxonomi (utökas vid behov; alla namn MÅSTE finnas
  exakt i poolen med png på disk):
  - **Fordon & transport:** Volvo, Volvo Cars, Scania, Saab, Polestar, BMW, Audi,
    Mercedes-Benz, Volkswagen, Toyota, Hyundai, Porsche, Ferrari, Tesla, SAS, Lufthansa, Voi
  - **Mat & dryck:** Marabou, Cloetta, Felix, Pågen, Gevalia, Löfbergs, Oatly, Lantmännen,
    Kopparbergs, Absolut Vodka, Carlsberg, Heineken, Coca-Cola, Pepsi, Nestlé
  - **Detaljhandel & dagligvaror:** IKEA, H&M, ICA, Axfood, Willys, Clas Ohlson, Dustin,
    Systembolaget, McDonald's, Burger King
  - **Tech & digitalt:** Apple, Microsoft, Google, Amazon, Samsung, Sony, Nintendo, Spotify,
    Netflix, Facebook, Mojang, King, Nvidia, Ericsson, Telia, Evolution, Klarna, Disney, Adobe
  - **Industri & verkstad:** Atlas Copco, Sandvik, SKF, Assa Abloy, Electrolux, Husqvarna,
    NIBE, Hexagon, Bahco, Boliden, Skanska, NCC, Essity, Tetra Pak, Securitas
  - **Finans & bank:** Swedbank, Nordea, SEB, Handelsbanken, Investor AB, EQT
  - **Mode & sport:** Nike, Adidas, Levi's, Björn Borg, Fjällräven, Acne Studios,
    Daniel Wellington, Hästens
  - **Hälsa/läkemedel:** AstraZeneca (para först när ≥2 finns i poolen, annars hoppa)

  Ambigua märken (Tesla = fordon+tech, Klarna = finans+tech, Samsung = tech) tilldelas
  konsekvent per kort den sektor som gör kortet giltigt. **Saknar sektorn data för ett mått
  → hoppa måttet (kortet byter mått eller utgår), vidga ALDRIG till annan sektor.**
- **`sakA`/`sakB` = EXAKTA namn ur `data/logos/logos.csv` vars png finns på disk.** Annars
  returnerar `LogoCatalog.UrlFor` null → trasig render. Filtrera kandidaterna mot disk.
- **Svensk-tung pool + krav:** majoriteten svenska märken. Utländska märken BARA om de har
  **svensk närvaro** (säljs/används i relevant omfång här — t.ex. digitala tjänster som
  Google/Meta, eller kedjor/varor på svenska hyllor). Inga obskyra B2B-märken ingen känner.
- **Bandmål 10/35/35/20**, riktning ~50/50, `ItemCap = 4`, inga lika-värde-ties (samma
  `värdeA`/`värdeB` ⇒ ingen riktning, droppa kortet).
- **`--key metricpair` vid report OCH merge** (mått-stam + oordnat `{sakA,sakB}`): samma
  par får finnas en gång *per mått* men inte två gånger inom samma mått. Mät:
  `report --staging --dir question-staging/loggor-mini --targets 10,35,35,20 --key metricpair`.

### Mini- vs prod-skala

En pack-slug som innehåller `mini` = koncept-pack: **175 kort, 7-frågors omgång**, undantas
från 1085-kontraktet. Prövas billigt innan full prod. Promote-väg när konceptet validerats:
döp om (släpp `mini`-markören) + skala till **1085 kort** → blir automatiskt 21-frågors
prod-deck (se CLAUDE.md "Game ideas / scaling"). Pilotens slug = `loggor-mini-1`,
visningsnamn "Loggor".
