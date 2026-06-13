# Mer eller Mindre — statisk designprototyp

A throw-away, **static HTML/CSS prototype** of every screen in *Mer eller Mindre*, built
three times — once each in **Pico CSS**, **Web Awesome**, and **Shoelace** — so the
look, feel, and responsive behavior can be locked and one library chosen.

There is **no backend, no htmx, no polling, no `dotnet`**. Screens are wired together
with plain `<a href>` links that fake the System's auto-transitions. All data is mocked
and identical across the three libraries (same players, same card, same scores), so they
are truly comparable.

## How to view

Just open `index.html` in a browser — no build step, no server required. Each library
loads purely from a CDN `<link>`/`<script>`.

```
prototype/index.html      ← comparison hub: pick a library, walk the GM or Player flow
prototype/<lib>/*.html     ← the 8 screens for one library
prototype/<lib>/theme.css  ← that library's MEM brand + responsive rules
```

> Internet access is required the first time (the CDN files are fetched live). If you are
> offline the components will not render.

## The 8 screens (1:1 with the spec's `Screen /` views)

| File | Spec view | Who | Shows |
|------|-----------|-----|-------|
| `catalog.html` | `Screen / Quiz catalog` | GM | Frågepaket (namn + antal frågor) → Spela |
| `join.html` | `Player / Join form` | Player | Efter QR-skanning: skriv namn → Gå med |
| `lobby-gm.html` | `Screen / Game lobby` | GM | QR + kort kod + live spelarlista + Starta omgång |
| `lobby-player.html` | `Screen / Game lobby` | Player | Du är med, väntar, spelarlista |
| `question.html` | `Screen / Question` | Alla | Fråga X/N, kort, riktning + 0-100-slider m. två staplar, Ställ |
| `waiting.html` | `Screen / Waiting for others` | Alla | Vem har gissat vs vem väntar man på |
| `results.html` | `Screen / Round results` | Alla | Facit + råvärden + två staplar + poäng per spelare |
| `standings.html` | `Screen / Final standings` | Alla | Slutställning (lägst vinner), vinnare markerad |

## Designval (spec-trogna — dokumenterade, inte frågade)

1. **Differens = 0-100-slider** med live **två-staplar-visualisering** (lika→0, hälften→50,
   en tiondel→90 — enligt poängrubriken i `specs/game-flows.yaml`). Brädans kg-knappsats
   föregick spec:ens 0-100-normalisering (`guessedDifference: int (0-100)`), så slidern är
   det spec-trogna valet.
2. **Riktning + differens på en Question-skärm, en submit** = ett `SubmitGuess`-kommando
   (matchar ASSUMPTION i `game-flows.yaml` rad 63-65). Steg 1: tryck vilket alternativ som
   är "mer", steg 2: dra differensen, en "Ställ"-knapp.
3. **Gå med = skanna GM:s QR + tryck-länk, kort kod som reserv.** UI:t visar en kort
   mänsklig kod (skalet mappar den till `joinCode`-Guid senare). QR:n är en platshållar-SVG.
4. **Lobbyn har två ansikten**: GM (QR + kod + live spelarlista + "Starta omgång", aktiv
   vid ≥2 spelare) och Spelare (med, väntar, spelarlista).
5. **Round results visar facit rikt**: råa kortvärden (550 km vs 330 km),
   normaliserad `correctDifference` (0-100), `correctDirection`, två-staplar, samt varje
   spelares `roundScore` + löpande `totalScore`. **Lägst total vinner.**

## Responsiv strategi

Mobile-first bas; brytpunkter ger luft vid surfplatta stående/liggande; **desktop
återanvänder surfplattelayouten** (max-bredd, centrerad). Pico är responsivt by default;
för Web Awesome/Shoelace lägger `theme.css` till max-bredd + ett enkelt rutnät. Slidern
och två-staplarna är tum-dimensionerade för telefon först.

Kontrollera i devtools device toolbar: telefon / surfplatta stående / surfplatta liggande
/ desktop. Bekräfta desktop == surfplatta, ingen horisontell scroll, slider/knappar
tum-stora.

## Bibliotekanteckningar

| Bibliotek | Kostnad | Modell | Status |
|-----------|---------|--------|--------|
| **Web Awesome** | gratis kärna + betald Pro | web components, CDN, ingen build | GA, aktivt |
| **Pico CSS** | gratis MIT | classless CSS, **noll JS**, CDN | aktivt |
| **Shoelace** | gratis MIT | web components, CDN, ingen build | **SOLNEDGÅNG** |

### ⚠️ Shoelace-solnedgången

Shoelace 2.x underhålls inte längre aktivt — projektet har blivit **Web Awesome** (samma
skapare, Font Awesome-teamet). Komponenterna är i princip identiska, bara prefixet skiljer
(`<sl-*>` → `<wa-*>`). Det är därför båda finns med här: man ser hur det "gamla" och det
"nya" ser ut, men **välj inte Shoelace för ny utveckling** — välj Web Awesome.

### CDN-versioner

Varje biblioteksmapp inkluderar sin CDN i `<head>` i varje `.html`. Versionerna kan
behöva bumpas över tid:

- **Pico CSS** — `cdn.jsdelivr.net/npm/@picocss/pico@2/...` (stabil, classless).
- **Shoelace** — `cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.20.x/...` (autoloader).
- **Web Awesome** — `early.webawesome.com/webawesome@3.x/...` (early-access CDN; bumpa
  versionssträngen om komponenterna inte renderar).

En liten vanilla-JS-snutt i `question.html` driver sliderns live-uppdatering av
två-staplarna och valet mer/mindre. Det är prototypens enda JavaScript och är oberoende
av biblioteket (även i Pico-versionen, vars *bibliotek* är JS-fritt — skriptet är vårt
eget, inte Picos).

## Utanför scope (senare faser)

htmx + polling, minimal-API-endpoints, projektioner och domän-alignment — hela spec:ens
"nästa fas". Den här prototypen låser bara visuell design + responsivitet.
