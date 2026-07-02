---
title: Blindbudet — koncept, marknad & monetisering (spel #2)
type: analysis
status: beslut (kandidat A vald, strategi spikad)
date: 2026-06-30
tags: [spel2, strategi, marknad, monetisering, arkitektur, auktion]
---
# Blindbudet — strategiunderlag för spel #2

**Kort svar:** spel #2 blir en **ny kategori under kvissig.se** — inte en standalone-produkt,
inte en tung telefon-controller-plattform. En Razor-app, en fly.io-deploy, ett repo, två spel
sida vid sida med MEM (Mer eller Mindre). Vald kandidat: **A — sealed-bid auktion
("Blindbudet")**, MEMs mekaniska tvilling (dold numerisk input → samtidigt avslöjande → poäng).
Spåret är medvetet **"decider-darling"**: tekniskt renast, lägst UI-kostnad, en portfölj-/
tekniksatsning där marknadsbehovet för auktion är **oprövat och accepterat som sådant**.

Detta dokument är det durabla beslutsunderlaget. Den exekverbara SDD-artefakten är
`specs/blindbudet-event-model.yaml` (emlang event-model). Domän, GWT-tester och web-shell är en separat senare omgång — spec före kod.

## Bakgrund

MEM är ett event-sourcat co-located kviss byggt på decider-mönstret
(`command → decide → events → evolve → state`, Event Modeling / GWT). Ägaren vill spika ett
andra spel som:

- (a) passar event sourcing / decider / VSA "hand i handske",
- (b) inte kräver tung grafik (kort/bräd/ord/party/quiz OK),
- (c) har rimlig UI/UX-insats för ett litet team (webb: HTML/CSS/HTMX),
- (d) ligger i en **underserverad OCH rimligt monetiserbar** nisch.

Marknadslins: svenska + danska/norska + internationell engelska. Tre parallella
research-agenter kördes (genre/arkitektur-fit, marknadsgap, monetisering); detaljrapporterna
ligger i syskonfilerna `foamy-dazzling-nest-agent-*.md`.

## Tre nyckelfynd

### 1. Arkitektur-fit

Bäst på BÅDA axlarna (ES-fit + låg UI-kostnad):

- party-voting / Jackbox-likes
- roll-and-write (Qwixx)
- **sealed-bid auktion**
- multiplayer-Wordle
- trick-taking (Plump/Skitgubbe)
- turbaserat abstrakt (Reversi/Fyra-i-rad)

Undvik teckna/gissa (realtidscanvas) och idle (snapshot + delta-tid slåss mot finkornig ES).
Jackbox-modellen (host = server, telefon = tunn klient) är **strukturellt identisk** med
CQRS/event-sourcing — och med det MEM redan är.

### 2. Marknadsgap

Wordle/Connections/geo-gissning är STÄNGDA i alla språk (mättat, reklamdrivet). Bekräftade
öppna gap, rankade:

1. **Nordisk-lokaliserad Jackbox-liknande telefon-controller-partyplattform** — alla bra
   EN-alternativ är olokaliserade, Jackbox vägrar svenska, forum efterfrågar explicit
   "delad-TV + telefon"-partyspel på svenska. Stark monetisering.
2. **"Resa/ledtråds-quiz" à la På spåret (SV)** — enorm kulturell dragning, bara statiska
   blogg-quiz finns, signaturmekaniken (närmare = färre poäng) oexploaterad digitalt.
   OBS: "På spåret" är SVT-varumärke — bygg MEKANIKEN, inte namnet.
3. **Polerade nordiska kortspel** (Skitgubbe/Plump/Knack) — bara hobby-appar; billigt
   public-domain-innehåll; lägre monetisering.
4. **Nordiskspråkigt B2B quiz-/pubquiz-verktyg** för krogar/företag/skolor — noll nordiskt
   alternativ; högst ARPU, mindre TAM, hårdare försäljning.

### 3. Monetisering (rekommendation)

Lagrad trio, inget "gratis" som kil:

1. **Engångsköp "en köper, alla spelar"** (Jackbox-modell; ingen billing-infra,
   tiny-team-vänligt, högst fit).
2. **Betalda tema-kortlekar / DLC** — MEMs CSV-kortlekskatalog gör varje tema till en
   naturlig SKU.
3. **B2B-licens** för krogar/företag/skolor (högst per-kund-ARPU, parallell kanal).

Undvik: daglig-prenumerations-tredmölla (NYT-modellen kräver evig redaktion) och
reklam-freemium i skala (QuizUp/HQ Trivia dog av obefintlig intäktsmodell trots stor publik
— bestäm modellen INNAN skalning).

## Ägarens vägval (bekräftat)

- Spel #2 = **ny kategori under kvissig.se**.
- Vald kandidat: **A — sealed-bid auktion ("Blindbudet")**.
- Stannar på strategi just nu — ingen kod-/bygg-planering.
- Spår: **"decider-darling"** ("4 DD" = "för Decider-Darling"). Tekniskt renaste, lägst UI.
  Medvetet en portfölj-/tekniksatsning (elegans + lägst byggkostnad) framför marknadsvaliderad
  intäkt. Marknadsbehovet för auktion är OPRÖVAT — accepterat.

## Arkitektur-framing (kategori under kvissig.se)

- Spel #2 = **syster-vertikal-slice**: en ANDRA Decider (`AuctionState` + egna Commands/Events/
  Errors) + egen pack-katalog, vid sidan av MEM. **Rör INTE** MEMs befintliga decider.
- **Dela endast det genuint identiska:** `Result<T>`-union, `GameContext`-form, CSV-parsern,
  web-shell-chrome (lobby/join/HTMX/scoreboard), fly.io-deploy, content-pipeline
  (`tools/pack.cs`, frågesättare/faktagranskare).
- **Abstrahera INTE en `GameEngine<T>` på n=2** — två snarlika deciders är rätt; en gemensam
  motor vore förtidig komplexitet (ponytail).
- **Monetisering stärks:** delad publik + en-köper-alla-spelar över hela siten + tema-lott-paket
  per spel = korsförsäljning (bättre än två standalone-produkter).

### Inom decider-darling: två kandidater

**Sealed-bid auktion** = MEMs mekaniska tvilling (dold numerisk input → samtidigt avslöjande →
poäng). `decide`: dolt bud → `LotRevealed` reveal; ren resolver + tie-regel. Återanvänder MEMs
shell (sifferfält, hidden-value-reveal, scoreboard) → **minsta diff**. Tema-"lotter" =
naturliga content-paket (som CSV-kortlekarna) → monetiserbart.

**Roll-and-write (Qwixx-typ)** = distinktare känsla. `DiceRolled(seed)`-event → simultana
`MarkCell`-kommandon (ren validering) → poäng-fold. Kräver NYTT ark-UI + tärningar + eget
originalspel (Qwixx licensierat — bygg mekanik ej kopia). Svagare monetisering.

**Lutar mot sealed-bid auktion** (lägst marginalinsats, återanvänder mest, klarast SKU-modell).
Roll-and-write om en mer egen spelkänsla prioriteras. Säkra-valet utanför spåret om
marknad/intäkt måste valideras = Jackbox-voting (men det är ej decider-darling).

## Koncept-skiss A — Sealed-bid auktion ("Blindbudet") — VALD

**Loop:** varje runda visas en *lott* (tema-laddad, dolt sant värde). Spelare lägger ett DOLT
bud. Vid reveal: högsta bud vinner lotten och **betalar sitt bud**; nettovinst = lottens
avslöjade värde − betalt bud. Flest netto över N rundor vinner. Spänningen = bjud högt nog att
vinna, lågt nog att gå med vinst + bluffa om värdet.

**Modell (en-stegs, MEM-tvilling utan tvåstegsraket):** dolt first-price-bud → reveal + score.
Vinnaren betalar sitt eget bud; poängen normaliseras till % av lottens eget värde:
`profit = vinnare ? clamp(round((santVärde − betaltBud)/santVärde*100), −100, 100) : 0`.
Normaliseringen krävdes när mini-poolens spridning (single-digit → miljoner) lät den största
lotten dränka alla andra — magnitud, inte budskicklighet, avgjorde. Överbjud >
sant värde ⇒ NEGATIV profit ("vinnarens förbannelse" = hela spänningen, speglar MEMs negativa
poäng), golvad vid −100. **HÖGST total vinner** (OBS motsatt MEM som har lägst-vinner — måste vara glasklart i
copy/spec). INGEN budget i v1 (ponytail — vinnarens förbannelse ersätter budget-bokföring).

**Tie-regel (lat, ES-snygg):** lika toppbud bryts av bud-ordning i event-loggen (tidigaste
`BidPlaced` vinner deterministiskt — gratis ES-egenskap, ingen extra state).

**UI:** lott-kort + sifferfält (bud) + reveal-lista + scoreboard. Återanvänder MEMs shell nästan
rakt av (dold numerisk input + reveal + projektioner finns redan).

**Innehåll/SKU:** lott-paket = CSV `{beskrivning; santVärde; tema; enhet}` — SAMMA
katalog-mönster som MEM (`tools/pack.cs` + frågesättare/faktagranskare-pipeline återanvänds).
Tema-paket = SKU, en-köper-alla-spelar.

## Koncept-skiss B — Roll-and-write (Qwixx-typ original)

**Loop:** aktiv spelare kastar delade tärningar; ALLA markerar sina egna ark under
placeringsregler (vänster→höger, monotont); låsta rader/straff avslutar; ren poäng-fold. Flest
poäng vinner. (Qwixx-mekanik är ej skyddad — men bygg EGET tema/poäng, ej klon.)

**ES-fit:** `DiceRolled(activePlayer, white[2], colored[4])` — RNG löst server-side, värdena i
event-payloaden ⇒ deterministisk replay → simultana `CellMarked`-kommandon (ren validering) →
poäng-fold. Exceptionell fit.

**Svagheter:** NYTT ark-UI (CSS-rutnät + Unicode-tärningar) + svagare SKU-story (replay-värdet
sitter i mekaniken, inte innehållet ⇒ svårare att sälja paket).

## Skillnaden i ett svep

| | Sealed-bid auktion | Roll-and-write |
|---|---|---|
| Byggkostnad | **Lägst** (återanvänder MEM-shell) | Måttlig (nytt ark-UI + tärningar) |
| ES-fit | Exceptionell (= MEMs reveal) | Exceptionell (1 RNG-event + rena marks) |
| Monetisering | **Klar** (tema-lott-paket = SKU) | Svag (mekanik-driven, tunna paket) |
| Spelkänsla | Nära MEM (bluff + siffror) | **Distinktare** (eget bordsspel) |
| Innehållspipeline | Återanvänder MEMs | Behöver ny (ark-design) |

## SDD-sekvens

Iterativ workflow (`specs/CLAUDE.md`): **spec → lint → diagram → (domän = separat effort)**.

1. **Analysdokument** (detta dokument) — det durabla strategiunderlaget.
2. **emlang event-model** — `specs/blindbudet-event-model.yaml` (NY fil, syskon till
   `mer-eller-mindre-event-model.yaml`). Samma emlang-dialekt, emoji-konventioner (✍️/👀/📋, ⚙️ System-gear,
   🧑‍🏫 host / 🧑‍🎓 Player), decider-true GWT. Co-located ⇒ inga timeouts, gears fyrar på
   state-villkor.

Verifiering av specen:

```bash
export PATH="$PATH:/c/Program Files/Go/bin:$HOME/go/bin"
emlang lint specs/blindbudet-event-model.yaml
emlang diagram specs/blindbudet-event-model.yaml -o specs/blindbudet-event-model.html
```

## Avgränsning

Domän-records, sister-Decider, GWT-tester och web-shell = SEPARAT senare omgång efter att
`blindbudet-event-model.yaml` lintat grönt och godkänts (SDD: spec före kod).
