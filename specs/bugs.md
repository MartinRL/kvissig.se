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

