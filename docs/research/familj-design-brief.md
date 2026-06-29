# Familj-deck — designbrief & research

Status: **research + spec only — inga kort byggda.** Underlag för ett kommande
`fragesattare`-uppdrag. Poängmodellen är LÅST (se memory/ADR 006); Familj ändrar **bara
innehåll** (igenkänning, bandmix, enheter, promptklarhet, kategorimix) — aldrig regler.

## Bakgrund: svårighetsstegen

| Slug | Visningsnamn | Storlek | Roll |
|---|---|---|---|
| `mer-eller-mindre` | "Mer eller Mindre – svår" | 1085 | Svåraste prod-decken |
| `alla-aldrar` | "Mer eller Mindre" | 1085 | **Featured/default** — lättaste prod-decken idag |
| `familj` *(ny)* | "Mer eller Mindre – Familj" | TBD | Ännu lättare, samma spel — barn 6+ med |

MEM är svårt by design: tvåstegs-gissningen (riktning + rå differens, server-normaliserad
0–100) är kilen. Familj sänker inte svårigheten genom enklare matematik utan genom **högre
igenkänning + mildare bandkurva**.

---

## R1 — 0-100 (PlayMIG), inspirationskällan

- **Mekanik = avståndspoäng på bunden skala.** Svaret är alltid 0–100; poäng = `|gissning −
  facit|`, exakt = −10, lägst total vinner. MEM ärver detta rakt av och lägger till
  tvåstegs-twisten.
- **Familj-editionen (Mini 0-100 Familj) är 6+** mot standardens 14+ — och skiljer sig
  **enbart på frågeinnehåll**, inte regler. "Alla mellan 6 och 100 år kan vara med, och alla
  har alltid ett svar." Exakt den modell vi vill kopiera.
- **Varför det är lättspelat:** den bundna skalan gör "jag vet inte" → "jag kan ändå chansa
  rimligt". Ingen sitter svarslös, ingen straffas hårt, förloraren lär sig ändå något.
  Ämnen hålls **konkreta och vardagliga** så alla har ett mentalt ankare.
- Editioner skiljs av innehåll, inte mekanik: Vit = **1085** (matchar vårt 1085-kontrakt),
  Mini = **175** / 7-frågorsrunda (matchar `Decider.MiniGameSize`).
- Källor: playmig.com/produkter/0-100-vit, /0-100-mini-familj, /mini-0-100-sverige; senses.se.

## R2 — Vad gör gissningsspel lättspelade för alla åldrar

Genrens fälla: vanlig trivia **belönar ren kunskap** → ojämnt, yngre/avslappnade "checkar ut".
Estimering fixar detta strukturellt: när ingen vet exakt spelar alla på jämn mark.

Innehållsspakar (rör ej poäng):
1. **Igenkänning är spak nr 1.** Både ett barn och en mormor måste känna igen *sakA* och
   *sakB* även utan att veta siffran (Hitster: "alla kan musik även om de inte vet året").
2. **"Ungefär rätt känns bra".** Välj par där *riktningen* går att känna men *storleken* är en
   äkta gissning — så landar −10-bonusen ofta (känns bra) men differensbandet förblir
   intressant. Undvik par som är trivialt självklara eller omöjligt obskyra.
3. **Rundlängd:** barn 10–20 min; 7-frågorsrundan matchar Wits & Wagers sju rundor och barns
   uppmärksamhetsfönster. 21-frågors prod passar längre vuxensessioner.
4. **Bordssnack:** öppna, debatterbara storlekar ("hur många miljoner skiljer det?") driver
   förhandling. Tvåstegs-reveal är i sig en snack-beat.
5. **Låg läsbörda:** en kort konkret mening, igenkännbara substantiv, inga bisatser/årtal/jargong.
6. **Kategorimix per generation:** sprid ämnen så varje åldersgrupp känner igen några
   (Trivial Pursuit Family / Smart10-läxan).
- Källor: gamesforyoungminds (Wits & Wagers), Hitster, 5 Second Rule, Smart10, gamerevolution.

## R3 — Intern baslinje: hur `alla-aldrar` ser ut idag

- **1085 kort**, inget `.källor.csv`-sidecar. Bandmix träffar sin override nästan exakt:

  | Band | Norm | Andel | alla-aldrar-mål |
  |---|---|---|---|
  | Riktningsfälla | 0–20 | 9,7 % | 10 % |
  | Slider-svett | 21–60 | 34,9 % | 35 % |
  | Tydligt glapp | 61–85 | 35,3 % | 35 % |
  | Wow/extrem | 86–100 | 20,1 % | 20 % |

  Riktning 50/50, ~80 enheter, 0 dubbletter, 0 same-entity-smell.
- **Bandmodell (en formel):** `mx=max(A,B)`, `correctDifference=round(|A−B|/mx*100)` (klamp
  0–100). Trösklar `[20,60,85]` (`tools/pack.cs:31`), default-mål 15/40/30/15, **alla-aldrar
  override 10/35/35/20** (ADR 012:49; style-guide:160-162). En källa = `Decider.NormalizeDifference`.
- **Igenkänningsribba (style-guide:156-172):** 13–83 år, maximera igenkänning, ~50/50
  globalt/svenskt. Men alla-aldrar släpper igenom **vuxen-nisch**: banddiskografier
  (Kent/Europe/The Cardigans), museibesök, antal anställda, huvudstäders folkmängd,
  länder-efter-yta. Igenkänt av vuxna, **inte nödvändigtvis av små barn.**
- **Enheter** domineras av fysiska storheter (meter 183, kilo 107, gram 81, år 80) men har en
  abstrakt svans (kromosomer, hertz, Nobelpris, volymprocent).
- **pack.cs validering:** `report` (histogram/riktning/enheter/items/dubbletter), `check`
  (maskingate, exit 0/1: count, band-tolerans, riktning, item-cap 4, dubbletter, smell),
  `merge --out`. Validera Familj med `report --targets <familj-kvot>` + `--min`.

---

## Rekommendationer för Familj (innehåll, inte regler)

1. **Sänk igenkänningsribban till små barn.** Stryk vuxen-nischen alla-aldrar tillåter
   (diskografier, museibesök, antal anställda, huvudstadsfolkmängd, länder-efter-yta).
   Ersätt med det en 6–10-åring också kan: vanliga djur, vardagsföremål, mat, kroppen,
   väder, fordon, kända landmärken, planeter, dinosaurier.
2. **Tippa bandkvoten ännu längre bort från riktningsfällan.** alla-aldrar är 10/35/35/20.
   Familj: kapa 0–20-bandet, väx "tydligt glapp" + "wow" så skillnaden blir visuellt tydlig.
   **Kandidatkvot 5/25/40/30** — mekanismen är mildare bandkurva + högre igenkänning,
   INTE enklare aritmetik (style-guide:167).
3. **Smalna enheterna till de mest intuitiva:** kilo, meter/centimeter, år, km/h,
   antal/stycken, °C. Släng per-100g-näring, ekonomiska/abstrakta tal, vetenskapliga enheter.
4. **Maximal promptklarhet:** Väger/Är/Har med vikt, längd, höjd, ålder-i-år, fart, antal.
   **Inga temporala riktningskort** (årtal + före/efter är det dokumenterat trasiga fallet).
   Namnge alltid arten/kategorin i apposition.
5. **Behåll låsta invarianter:** ~50/50 riktning, item-cap 4, inga lika-värden, inga
   dubbletter, 0 same-entity-smell; pris-/"kostnadsfritt"-ord får aldrig förekomma (se memory-regeln).

## Öppen designfråga (rekommendation, ej beslut)

**Rundlängd / slug.** Två vägar:
- `familj-mini` → 7-frågorsrunda, 175 kort, undantagen 1085-testet. Snabbast att pilota,
  kortast runda = mest lättspelad för barn (matchar 0-100 Familj Mini + R2:s 10–20 min).
  MEN `mini` är konceptdeck-markören — signalerar "oprövad" snarare än "lätt familjeprodukt".
- `familj` (prod) → måste nå **exakt 1085 kort** (guardas av `EveryFullDeckIsExactly1085Cards`)
  och spelar 21-frågorsrunda.

**Rekommendation:** pilota som `familj-mini` (175) för att validera igenkänningsribban + kvoten
billigt; promota till prod `familj` (1085, 21-runda) när konceptet håller. Kort runda är mer
lättspelad — men 21-runda är prod-kontraktet. Användaren avgör slug + rundlängd.

---

Deck-spec + färdig `fragesattare`-brief: `question-staging/familj/BRIEF.md`.
