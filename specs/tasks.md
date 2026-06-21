# Implementation Tasks

## Phase 1: Domain Types (from spec)
- [x] Create `Direction` enum (Mer, Mindre)
- [x] Create `Commands.cs` with all command records (`union GameCommand`)
- [x] Create `Events.cs` with all event records (`union GameEvent`)
- [x] Create `Errors.cs` with all error records (`union GameError`)
- [x] Create `State.cs` with `GameState` record (QuestionRound deck + derived members)
- [x] Create `PlayerScore` record for scoring breakdown

## Phase 2: Decider Implementation
- [x] Implement `Evolve` function with pattern matching
- [x] Implement `Decide` function with pattern matching (`GameContext.FindPack`)
- [x] Implement scoring logic: diff + bonus calculation (normalize + clamp in ScoreQuestion)
- [x] Add `Result<T>` type for error handling

## Phase 3: GWT Tests (from spec)

### Lobby Tests
- [x] `game can be created`
- [x] `player can join lobby`
- [x] `cannot join nonexistent game`
- [x] `cannot join started game`
- [x] `cannot join with name already taken`

### Start Tests
- [x] `game can be started`
- [x] `cannot start nonexistent game`
- [x] `cannot start without enough players`

### Guess Tests
- [x] `guess submitted successfully`
- [x] `cannot guess in nonexistent game`
- [x] `cannot guess before game starts`
- [x] `cannot guess as non-member`
- [x] `cannot guess again on same question`
- [x] `difference out of range rejected`

### Scoring Tests
- [x] `all guesses scored and answer revealed`
- [x] `exact difference with correct direction`
- [x] `scores accumulate across rounds`
- [x] `cannot score before all guesses in`
- [x] `cannot score an already-scored question`

### Progression Tests
- [x] `progress shows a next question while questions remain`
- [x] `progress shows no next question once the last is scored`
- [x] `next question presented when one remains`

### End Game Tests
- [x] `lowest score wins` (lowest total; ties share the win via `winnerIds`)
- [x] `tied lowest totals share the win`

## Phase 4: Question Loading (file-based CSV catalog)
- [x] Create `Question` record (questionText, itemA, itemB, valueA, valueB, unit — raw values, no precomputed answer)
- [x] Create `QuestionPack` record (slug packId, deslugged name, derived questionCount)
- [x] Create pure `QuestionPackCsvParser` in Domain (sv-SE `;`/`,`, RFC4180 quotes, BOM, header-mapped)
- [x] Create `FileSystemQuestionPackCatalog` in Web (IO, fail-fast) + DI registration
- [x] Add first real pack `data/packs/mer-eller-mindre.csv` (10 Swedish cards)

## Phase 5: Game Repository
- [x] Create `GameRepository` (in-memory event store + joinCode→gameId index)
- [x] Implement `Load` (fold events through `Decider.Fold`)
- [x] Implement `GameApplicationService` (Decide + append events; Open/Execute + score/progression gears)

## Phase 6: Read Models (Projections)
Pure `GameState → View` functions in the Domain (one per spec view slice). Quiz Catalog
is NOT here — it is Web-only reference data read straight from the CSV catalog (no event
source, no GT).
- [x] `GameLobbyView` — gameId, joinCode, players (host first, then joins)
- [x] `QuestionView` — current card by index (text/items, NOT raw values) + progress
- [x] `WaitingForOthersView` — submitted vs pending player ids for the current question
- [x] `RoundResultsView` — revealed answer + per-player round + running total
- [x] `FinalStandingsView` — final scoreboard + winner(s) (folded from GameEnded)
- [x] `OutstandingGuessesView` — one `OutstandingQuestion` row per question (todo list)
- [x] `GameProgressView` — index, totals, scoredQuestionCount, hasNextQuestion (todo)

## Phase 7: Web Endpoints
- [x] `POST /games` — create game (HX-Redirect to lobby shell)
- [x] `POST /games/{code}/join` — join game (HX-Redirect to lobby shell)
- [x] `POST /games/{code}/start` — start game (host only)
- [x] `POST /games/{code}/guess` — submit guess (direction + raw difference); score gear fires on allGuessesIn
- [x] `POST /games/{code}/next` — next question / end game (host progression gear)
- [x] `GET /games/{code}/state` — polling endpoint (renders the per-viewer screen fragment)

## Phase 8: Razor Pages + HTMX
- [x] Home page (Quiz catalog) with "Create Game" form per pack
- [x] Lobby pages (host + player) with join code/link and player list
- [x] Host lobby renders a scannable QR (server-side inline SVG via QRCoder) encoding the absolute `/games/{code}/join` URL
- [x] Question page with mer/mindre buttons + unit difference slider (0→max(A,B), live two-bars)
- [x] Waiting page (submitted vs pending) while others guess
- [x] Results page with answer revealed (raw values + normalized facit) + per-player score breakdown
- [x] Final standings page (winner banner, lowest-total-wins scoreboard)

## Phase 9: Polish
- [x] nu när vi inte längre ser maxvärdet, utan bara hur den mindre förhåller sig till den större (mindre vs mer) så är det lämpligt att på något vis introducera %. dvs när rikting har valts och man rör slidern, så är det ju i praktiken den mindres % av mer man gissar. detta ska du ultrathink om ur ett spel- och UX-mässigt perspektiv innan lösning.
- [x] textöverlapp under staplarna när man svarar. ser bra ut när man har svarat och ser svaret.
- [x] pongsammanställning ska visa vilken "rond" som just spelats (under spelets gång visas rond#/21)
- [ ] Mobile-friendly CSS
- [x] PWA manifest
  - [x] Skärmdumpar för rikare installationsdialog (valfritt — påverkar inte "installable")
    - [x] Starta appen lokalt: `dotnet run --project src/MerEllerMindre.Web`
    - [x] Desktop (wide): `wwwroot/screenshots/screenshot-1280x720.png` (faktisk 1536×864)
    - [x] Mobil (narrow): `wwwroot/screenshots/screenshot-720x1280.png` (faktisk 810×1440)
    - [x] Krav på bilderna: 320–3840 px per sida, längsta sidan ≤ 2,3× kortaste, PNG/JPEG; alla med samma form_factor måste ha samma bildförhållande — båda 16:9, OK
    - [x] Lägg till `screenshots`-blocket i `wwwroot/manifest.json` (en `wide` + en `narrow`, med `src`, `sizes`, `type`, `form_factor`, `label`)
    - [x] Verifiera i Chrome DevTools → Application → Manifest att skärmdumps-varningarna är borta
- [ ] Error handling UI
- [ ] Loading states
- [x] create favicon
- [ ] feedback. nu saknar spelet kontaktmöjlighet helt och hållet.  
- [x] vid poängsammanställningen, när alla spelare svarat, ska det också framgå hur de enskilda spelarna svarat (rondresultatet visar per spelare svarad % "mindre av mer" + rätt/fel-riktning; facit som "mindre är X% av mer")

## Phase 10: deploy
- [x] deploy to fly.io from github actions (live på https://kvissig.fly.dev, CI Deploy grön, single-instance)
- [x] buy kvissig.se (Loopia)
- [x] make kvissig.se work, ie the url (incl. www.kvissig.se) works on the internet
  - [x] fly certs add kvissig.se + www.kvissig.se
  - [x] Cloudflare DNS: A/AAAA på apex + www → fly-IP (66.241.125.165, 2a09:8280:1::12a:1486:0)
  - [x] verifiera cert issued + https på båda värdnamnen (båda 200 ok, rätt CN)

## Phase 11: review by reviewers
- [x] inga värden, bara förhållandet och enheten. nu kan kan man lätt förstå att maxvärdet är maxvärdet av "mest". exempelvis blir frågan vikten av haj vs. människa lätt att gissa sig till då de flest ju vet vikten av en människa. detta lägger ytterligare vikt vid spelets unika attribut: en slider som får två staplar att röra sig upp och ned. annorlunda an traditionellt quiz / kviss och inte något som går att replikera på papperskort.
- [x] stor förändring ska testas: tvåstegsraket. först besvaras mer eller minde och de som gissat rätt riktning får sina -10 poäng. därefter används slidern för att precis som nu gissa %-skillnad

## Phase 12: product-led growth and growth
- [ ] hur får vi användare/spela att dela spelet med familj och vänner?
- [ ] logisk nästa lokala marknad? nb. ej blott baserat på språk med land/kultur (t.ex. sverige OCH svenska)
- [x] analytics (Plausible Cloud: snippet for traffic + server-side Events API gameplay funnel)

## product feature validation / lean-startup MVP'ing
- [ ] klicka på en knapp "köp detta spel som app till en engångskostnad precis som en fysisk kortlek"
- [ ] feature-flagging, t.ex. WIP-kortlekar

## ACMM Level 3
- [ ] [[acmm]]
