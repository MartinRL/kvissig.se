# Deck-spec: "Mer eller Mindre – Familj"

Full bakgrund + research: `docs/research/familj-design-brief.md`.
Detta är den körbara specen + `fragesattare`-briefen. **Inga kort byggda än.**

## Beslut som ska fastställas (av användaren) före kortförfattande
- **Slug + rundlängd:** rekommendation = pilota som `familj-mini` (175 kort, 7-frågorsrunda,
  undantaget 1085-testet); promota senare till prod `familj` (1085 kort, 21-runda, guardas av
  `EveryFullDeckIsExactly1085Cards`). Kort runda = mer lättspelad; prod = kontraktet.
- **Visningsnamn:** "Mer eller Mindre – Familj" (en-dash, matchar övriga). Lägg i
  `DisplayNameOverrides` (Questions.cs) när decken existerar.

## Bandkvot-override (vs alla-aldrar 10/35/35/20)
Kandidat **5/25/40/30** — tippad bort från riktningsfällan mot tydliga glapp + wow:

| Band | Norm | Familj-mål | alla-aldrar |
|---|---|---|---|
| Riktningsfälla | 0–20 | 5 % | 10 % |
| Slider-svett | 21–60 | 25 % | 35 % |
| Tydligt glapp | 61–85 | 40 % | 35 % |
| Wow/extrem | 86–100 | 30 % | 20 % |

Validera: `dotnet run tools/pack.cs -- report --staging --dir question-staging/familj --targets 5,25,40,30`.
Mekanismen är **mildare bandkurva + högre igenkänning**, inte enklare matematik.

## Igenkänningsribba (HÖGRE än alla-aldrar)
Målgrupp: **6+** med (barn, föräldrar, mor/farföräldrar). Både ett barn OCH en mormor måste
känna igen `sakA` och `sakB` — även utan att veta siffran.

- **JA — kategorimix:** vanliga djur, vardagsföremål, mat & godis, kroppen, väder, fordon,
  kända landmärken/byggnader, planeter & rymd, dinosaurier, sport alla känner till.
- **NEJ — stryk vuxen-nischen alla-aldrar släpper igenom:** banddiskografier
  (Kent/Europe/The Cardigans), museibesök, antal anställda, huvudstäders folkmängd,
  länder rangordnade efter yta, dagsfärsk popkultur bara ena änden kan.
- ~50/50 globalt känt / svenskt-nordiskt.

## Enheter — smalna till de mest intuitiva
Tillåt: **kilo, gram, meter, centimeter, år (ålder), km/h, antal/stycken, °C, kalorier**
(bekanta). Undvik: per-100g näring (fett/socker), ekonomiska/abstrakta tal, vetenskapliga
enheter (hertz, kromosomer, volymprocent, m/s², Nobelpris).

## Promptklarhet
- En kort konkret mening. Igenkännbara substantiv. Inga bisatser/årtal/jargong.
- Säkra komparativer: **Väger / Är / Har** med vikt, längd, höjd, ålder-i-år, fart, antal.
- **Inga temporala riktningskort** (årtal + före/efter = dokumenterat trasigt — större år =
  senare = inverterad riktning).
- Konvention: **Mer = sakA håller det större värdet** och fraseras som subjekt.
- `differensfråga` ska bjuda in muntlig gissning ("Hur många kilo skiljer det?").

## Låsta invarianter (gäller alla decks)
~50/50 riktning · item-cap 4 · inga lika-värden · inga dubbletter (`questionText`) ·
0 same-entity-smell · **pris-/"kostnadsfritt"-ord får aldrig förekomma** (se memory-regeln).

---

## fragesattare-brief (körklar)

> **Kategori:** Familj — högsta igenkänning, 6+ år.
> **Antal:** generera batchar om ~25–30 till `question-staging/familj/`.
> **Typiska enheter:** kilo, gram, meter, centimeter, år, km/h, antal, °C, kalorier.
> **Innehåll:** vanliga djur, vardagsföremål, mat & godis, kroppen, väder, fordon, kända
> landmärken, planeter, dinosaurier, känd sport. Både barn och mor/farförälder ska känna
> igen båda sakerna.
> **Bandmål 5/25/40/30** — sikta på TYDLIGA glapp och WOW-kort; undvik nära-lika
> riktningsfällor. Self-review varje kort mot banden via `Decider.NormalizeDifference`-logik
> (mx=max, round(|A−B|/mx*100)).
> **Förbjudet:** vuxen-nisch (diskografier, antal anställda, museibesök, huvudstadsfolkmängd,
> länder-efter-yta), temporala riktningskort (årtal+före/efter), abstrakta/vetenskapliga
> enheter, pris-/"kostnadsfritt"-ord (se memory-regeln).
> **Konvention:** Mer = sakA större; namnge art/kategori i apposition; en kort mening.
> Skriv batch-CSV (sv-SE: `;`-separator, `,`-decimal, header
> `fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga`) + källrad-sidecar.

Pipeline efter fragesattare: **faktagranskare → sprakgranskare → tydlighetsgranskare → kurator**,
sedan `tools/pack.cs check --targets 5,25,40,30 --min 175` (eller `--min 1085` för prod).
