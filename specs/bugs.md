# Bug List

Loggade buggar i samma anda som `tasks.md`. Mall för nya poster:

```
- [ ] **Kort titel** — `var` (fil:rad)
  - Förväntat: …
  - Faktiskt: …
```

## Buggar

- [x] **Död 6-teckens-join-kod visas i värd-lobbyn** — `LobbyHostScreen.razor`
  - Förväntat: visad kod går att skriva in för att gå med.
  - Faktiskt: inget konsumerar den korta koden — `Resolve` kräver hela 32-teckens-Guiden (`Guid.TryParse`), enda vägen in är QR/full-URL. Att skriva in koden gav "Spelet hittades inte". Fix: tog bort visningen (QR + full-URL räcker).
