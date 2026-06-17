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

- [x] **Långt egennamn spiller utanför kortet på riktningsknapp** — `playful.css` / `QuestionScreen.razor`
  - Förväntat: långa ord ("Washingtonmonumentet") ryms inom knappen/kortet, snyggt avstavat.
  - Faktiskt: ordet i `.dirbtn` (halv kolumn, grid 1fr 1fr) fick inte plats och stack ut förbi kortramen. Fix: ren CSS — `hyphens: auto` + `-webkit-hyphens: auto` på `.dirbtn` och `h1` (svensk avstavning via `<html lang="sv">`), `overflow-wrap: break-word` som skyddsnät om inget bindestreckställe finns.

