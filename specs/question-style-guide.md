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

## Hund (hundraser-mini)

Gäller NÄR pack-slug = `hundraser-mini` (delta mot basfilosofin ovan). Leken jämför hundraser
kvantitativt. **HÅRD REGEL: bryt enmetrik-monotonin.** En tidig version blev 123/175 ren
kroppsstorlek (vikt + mankhöjd) → spelaren upplever "varenda fråga handlar om vikt". Leken ska
spänna över **allt möjligt om hundraser**, fortfarande som verifierbara mer/mindre-jämförelser
med per-ras-värden (faktagranskaren gatekeepar — ej källbart = förkasta).

**Åtta verifierbara dimensioner, ingen får dominera (cap ~25 kort/dimension):**

| Dimension | `enhet` | källa | målantal |
|---|---|---|---|
| Storlek (mankhöjd) | cm | FCI/SKK rasstandard | ~25 totalt med vikt |
| Storlek (vikt) | kg | FCI/SKK rasstandard | (ingår i ~25 ovan) |
| Livslängd | år | Agria/AniCura/rasdata | ~22 |
| SKK-popularitet | registreringar/år | SKK årsstatistik (år-pinnad) | ~22 |
| Valppris | kr | svenska kennlar/Agria (år-pinnad) | ~18 |
| Ras-ålder | år | år sedan rasstandard erkändes | ~22 |
| Kullstorlek | valpar | SKK Avelsdata/raslitteratur | ~22 |
| Höftledsdysplasi | % | ofa.org "Disease Statistics by Breed" | ~22 |
| Maxhastighet | km/h | Guinness/racing — bara vinthundar/sporthundar | ~8 (litet block med flit) |

**Coren intelligensrank = SKIP** (rank 1 = klokast är inverterad → tydlighetsgranskaren
förkastar). Debunkat/ej källbart och därför EJ med: bettstyrka, agility-vinster per ras.

**Riktnings-/tydlighetsfällor att baka in (annars trasiga kort):**
- **Ras-ålder:** värde = `nuvarande år − erkännandeår` i ÅR. ALDRIG årtalet (temporal utan
  magnitud = förkastas, se "Otydlig riktning"). Större = äldre = "mer".
- **Höftledsdysplasi %:** högre % = mer dysplasi. Fråga: "Har X högre eller lägre andel
  höftledsdysplasi än Y?" Rent komparativ.
- **Maxhastighet / kullstorlek / livslängd / pris / storlek:** naturligt komparativ, högre = "mer".
- **Volatila (pris, SKK-reg):** år-pinnas i frågetext + sidecar.
- **Riktning ~50/50**, sakA behöver ej hålla det största värdet.

**Pool:** ≥88 distinkta raser (ItemCap=4 pack-wide). SKK erkänner ~300 → gott om utrymme. En ras
≤4 ggr TOTALT över alla dimensioner; styr med cap-varningarna i `report`.

**Mät enhetsfördelningen varje batch:** `report --staging --dir question-staging/hund
--key question` → **Top units får inte visa någon enhet >~25** (~8 enheter representerade).
Bandmål 15/40/30/15, riktning ~50/50.

## Elbil (elbil-mini)

Gäller NÄR pack-slug = `elbil-mini` (delta mot basfilosofin ovan). Leken jämför elbilsmodeller
kvantitativt. **HÅRD REGEL: bryt enmetrik-monotonin.** En tidig version blev 81/175 ren
hästkraft (hk) → spelaren upplever "varenda fråga handlar om effekt". Leken ska spänna över
**allt mätbart om elbilar**, fortfarande som verifierbara mer/mindre-jämförelser med per-modell-
värden (faktagranskaren gatekeepar — ej källbart = förkasta). Källa: ev-database.org,
tillverkarspecar, carwow/biltester.

**Nio verifierbara dimensioner, ingen får dominera (cap ~28 kort/dimension):**

| Dimension | `enhet` | källa | målantal |
|---|---|---|---|
| Effekt | hk | tillverkarspec/ev-database | ~26 |
| Räckvidd (WLTP) | km | ev-database WLTP | ~28 |
| Acceleration 0–100 | sekunder | tillverkarspec | ~18 |
| Batterikapacitet (användbar) | kWh | ev-database "usable" | ~15 |
| Pris (år-pinnat) | kr | svensk prislista (år-pinnad) | ~15 |
| Toppfart | km/h | tillverkarspec/ev-database | ~22 |
| Tjänstevikt | kg | tillverkarspec/ev-database | ~22 |
| Laddeffekt (max DC) | kW | ev-database fastcharge | ~20 |
| Förbrukning (WLTP) | kWh/100km | ev-database efficiency | ~9 |

**Riktnings-/tydlighetsfällor att baka in:**
- **Acceleration 0–100 sek + förbrukning kWh/100km är "lägre = bättre" men frågan är ren
  magnitud** — "Tar X längre eller kortare tid…?" / "Drar X mer eller mindre…?". Komparativ
  pekar rätt mot [Mer]/[Mindre]; det är OK (snabbast/snålast ≠ "mer", men frågan frågar inte
  om bäst utan om magnitud). Lägre sekundtal = kortare tid = "mindre".
- **Alla andra (hk/km/kWh/kr/km/h/kg/kW):** naturligt komparativ, högre = "mer".
- **Pris:** volatilt → år-pinnas i frågetext (t.ex. "(2024)") + sidecar.
- **Enheter som krockar:** `kWh` (batteri) vs `kWh/100km` (förbrukning) vs `kW` (laddeffekt)
  är OLIKA enhetssträngar — håll dem distinkta i `enhet`-kolumnen. `km` (räckvidd) vs `km/h`
  (toppfart) likaså.
- **Riktning ~50/50**, sakA behöver ej hålla det största värdet.

**Pool:** ≥88 distinkta modeller (ItemCap=4 pack-wide). Hundratals elbilsmodeller finns →
gott om utrymme, MEN populära modeller (Tesla/Polestar/BMW) fylls snabbt över flera
dimensioner → sprid på färska modeller, styr med cap-varningarna i `report`. Använd EXAKTA
modellnamn med variant (t.ex. "Tesla Model 3 Long Range") konsekvent — `report` räknar exakta
strängar, så "Tesla Model 3" ≠ "Tesla Model 3 Long Range".

**Mät enhetsfördelningen varje batch:** `report --staging --dir question-staging/elbil
--key question` → **Top units får inte visa någon enhet >~28** (~9 enheter representerade).
Bandmål 15/40/30/15, riktning ~50/50.

### Mini- vs prod-skala

En pack-slug som innehåller `mini` = koncept-pack: **175 kort, 7-frågors omgång**, undantas
från 1085-kontraktet. Prövas billigt innan full prod. Promote-väg när konceptet validerats:
döp om (släpp `mini`-markören) + skala till **1085 kort** → blir automatiskt 21-frågors
prod-deck (se CLAUDE.md "Game ideas / scaling"). Pilotens slug = `loggor-mini-1`,
visningsnamn "Loggor".
