# Bug List

Loggade buggar i samma anda som `tasks.md`. Mall för nya poster:

```
- [ ] **Kort titel** — `var` (fil:rad)
  - Förväntat: …
  - Faktiskt: …
```

## Buggar

- [x] UI-bugg. efter att en fråga besvarats fås korrekt riktning och korrekta värden, vilket är jättebra! men i fallet med Danmark vs Norge så överlappar de, dvs "Norge · 5.5 miljoner invånare" överlappar med "Danmark · 5.9 miljoner invånare". formodad lösning: ett html-element, ej två, men inspektera och tänk själ därom!

- [x] språkbruk: "din gissning är ställd" --> "du har gissat"

- [x] språkbruk: "Lås riktning" --> "Gissa!"

- [x] det behövs ingen enhet (tex 'meter') under slidern — framgår av frågan

- [x] språkbruk: "Spelledaren går vidare strax…" --> "Spelledaren visar strax nästa fråga…"


- [x] **Död 6-teckens-join-kod visas i värd-lobbyn** — `LobbyHostScreen.razor`
  - Förväntat: visad kod går att skriva in för att gå med.
  - Faktiskt: inget konsumerar den korta koden — `Resolve` kräver hela 32-teckens-Guiden (`Guid.TryParse`), enda vägen in är QR/full-URL. Att skriva in koden gav "Spelet hittades inte". Fix: tog bort visningen (QR + full-URL räcker).

- [x] **Samma sak förekom flera gånger i samma spel (Globen ×3)** — `QuestionSelection.PickBalanced` (Decider.cs)
  - Förväntat: ingen sak ska dyka upp mer än en gång bland ett spels 21 frågor.
  - Faktiskt: band-urvalet shufflade utan sak-spärr; den 1085-korts live-packen är tung på populära saker (Eiffeltornet ×22, Globen ×11) → slumpen landade samma sak 2–3×. Fix: runtime-spärr i `PickBalanced` (varje itemA/itemB högst 1×/spel, best-effort) + sak-frekvensrapport & `cap`-kommando i `tools/pack.cs` för offline-kurering.

- [x] **Poängtavlan beskär "Total" på smala telefoner** — `playful.css` / `ResultsScreen.razor`
  - Förväntat: hela tabellen, inkl. Total-kolumnen, ryms inom kortramen på alla telefoner.
  - Faktiskt: 6-kolumnstabellen rymdes precis vid `.wrap` 560px → spillde över kortet och klipptes på smalare skärmar. Fix: ren CSS — `@media (max-width: 559.98px)` staplar `.scoreboard` till per-spelare-block (rad 1 = rank · namn · total, rad 2 = rond-celler som etiketterade chips via `data-label` + `::before`); ≥560px oförändrad tabell.

- [x] **Poängtavlans rader ser trasiga ut på telefon — totalen slängs till högerkanten** — `playful.css` / `ResultsScreen.razor`
  - Förväntat: varje spelarrad ser likadan ut oavsett namnlängd; total packad direkt efter namnet.
  - Faktiskt: i `@media (max-width: 559.98px)` är varje `<tr>` `display:flex; flex-wrap:wrap` och `.who` hade `flex:1`. När ett namn är långt nog att första chippen ("Mer eller Mindre ✓…") inte ryms på rad 1 wrappar chippen → rad 1 har bara rank+namn+total + massa tomrum som `.who{flex:1}` glupskt äter → totalen slängs till ytterkanten. Korta namn (Nils/Sven) ser fina ut, "Martin" (längst) trippar wrappen varje spel. Inte tittar-specifikt. Fix: ren CSS — tog bort `flex:1` från `.scoreboard .who` (namn+total packas vänster) + `.scoreboard .who + .round { flex-basis: 100%; }` tvingar första chippen till egen rad (chip 2+3 wrappar ihop på nästa). Deterministiskt för alla namnlängder; ≥560px oförändrad tabell.

- [x] **Långt egennamn spiller utanför kortet på riktningsknapp** — `playful.css` / `QuestionScreen.razor`
  - Förväntat: långa ord ("Washingtonmonumentet") ryms inom knappen/kortet, snyggt avstavat.
  - Faktiskt: ordet i `.dirbtn` (halv kolumn, grid 1fr 1fr) fick inte plats och stack ut förbi kortramen. Fix: ren CSS — `hyphens: auto` + `-webkit-hyphens: auto` på `.dirbtn` och `h1` (svensk avstavning via `<html lang="sv">`), `overflow-wrap: break-word` som skyddsnät om inget bindestreckställe finns.

- [x] **Inaktuell test: `CreateGame_RedirectsHostToTheLobbyShell` röd** — `GameEndpointsTests.cs:119`
  - Förväntat: testen grön; den verifierar att värd-lobbyn renderar join-länken.
  - Faktiskt: testen kräver `/games/{code}/join` i plain `/state`-svaret, men committen "clickable join URL + copy-to-clipboard" gjorde länken progressivt avslöjad — `LobbyHostScreen.razor:8` visar den bara `@if (Model.ShowJoinUrl)`, vilket slås på först när pollen anropar `/state?url`. Funktionen funkar; testen påstår ett gammalt kontrakt. Fix: assert mot `/state?url` (där join-URL:en faktiskt renderas), inte plain `/state`.

- [x] **För lite ämnesvariation — 4 av 7 frågor handlade om börsvärde** — `QuestionSelection.PickBalanced` (Decider.cs)
  - Förväntat: en spelomgång ska sprida sig över olika frågetyper (`fråga`-kategorier), inte domineras av en.
  - Faktiskt: urvalet balanserade bara på svårighetsband + sak-distinkthet; inget spärrade samma `questionText`-kategori. Loggor-mini (7 frågor, 6 kategorier) kunde landa 4 börsvärde-frågor. Fix: runtime-ämnescap i `PickBalanced` — varje distinkt `questionText` högst `ceil(count / antal distinkta kategorier)` ggr/spel (best-effort, samma mönster som sak-spärren).

- [x] **Otydlig riktning — temporala "före/efter"-årtalskort** — pack-CSV:erna (`otydlig-rikting.webp`)
  - Förväntat: frågans jämförelseord pekar samma håll som [Mer]/[Mindre] så en familj kan enas om svaret.
  - Faktiskt: kort som "Inträffade Stockholms blodbad före eller efter Gustav Vasas trontillträde?" (1520 vs 1523) jämför *när* mot fasta [Mer]/[Mindre] — större årtal = senare = *mindre* länge sedan, en inverterad osynlig mappning, OCH sakA höll det mindre årtalet (konventionsbrott). Omöjligt att gissa rätt utan dold regel. Fix: skrev om kort med äkta magnitud (regeringslängd/ålder/höjd/antal) och ersatte rena temporala årtalskort med icke-temporala magnitud-kort i alla tre packen (antal bevarat 1085/1085/175). Ny `tydlighetsgranskare`-agent + stilguide-sektion "Otydlig riktning" fångar mönstret framåt.

- [x] **TTT fel poäng #3: alla spelare fick Nådde 3 · Poäng 100** — `TankEndpoints.Parse` (TankEndpoints.cs:147, `fel-poäng-#3.jpg`)
  - Förväntat: varje spelares faktiskt nådda tal och `min(|nådd − mål|, 100)`-poäng.
  - Faktiskt: klienten postar camelCase (`{steps, answerIndex}`) men servern deserialiserade UTAN options → case-sensitive PascalCase band INGET → `SolutionDto(Steps: null, AnswerIndex: 0)` → tyst `Solution([],0)` = lås `Numbers[0]` (3) för alla → 100 poäng för alla. Domäntesterna passerade aldrig JSON-gränsen, därför gröna. Fix: `JsonSerializerOptions.Web` + `Steps is null`-vakt (malformed → avslag, aldrig fejk-lösning). Regressionstest `TankSolutionParseTests` låser payload-form ↔ options-kontraktet.

- [x] **Steg 2 frågar i enheten men spelaren svarar i procent** — `QuestionScreen.razor` (stage 2, `741940416_..._n.jpg`)
  - Förväntat: frågan och svaret på gisskärmen är i samma skala.
  - Faktiskt: steplabeln var kortets `differensfråga` ("Hur många centimeter skiljer det?") men slidern visade aldrig ett tal, bara "≈ NN%" — spelaren gissade i praktiken en andel men avkrävdes enheten. Fix: 0-100-ramen — steplabeln genereras: "Om {MER} är 100, var hamnar {MINDRE}?" (logo-om-finns), korta stapeln visar "≈ NN", höga "100". Ren presentation: hidden input postar fortfarande rått slidervärde, servern normaliserar som förut. `DifferencePrompt`/`Unit` städade ur `QuestionVm` (domänen + CSV-kolumnen `differensfråga` orörda).

  ## Loggor
  - [x] Mer eller Mindre – Loggor (alla åldrar 1) --> Mer eller Mindre – Loggor – alla åldrar
  - [x] Mer eller Mindre – Loggor (blandat 1) --> Mer eller Mindre – Loggor
  - [x] loggorna ska inte skalas om. ser helt förfärligt ut! kolla skärmdumpen scaled-logos-bug.png! — `.dirbtn`/`.twobars-legend` är flexcontainrar (`align-items: stretch`) som tänjde `<img>` till full bredd medan `max-height` klippte höjden → distordrade loggor. Fix: `.logoimg` får `align-self: center` (ingen sträckning) + `object-fit: contain` i playful.css.
  - [x] spelet "Mer eller Mindre" ska vara placerat högst upp, ej längst ned — katalogen sorterade rent ordinalt på slug (`mer-eller-mindre` hamnade sist). Fix: `FileSystemQuestionPackCatalog` pinnar `mer-eller-mindre` först, resten alfabetiskt.

