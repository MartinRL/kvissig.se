# Implementation Tasks

## Phase 1: Domain Types (from spec)
- [ ] Create `Direction` enum (Mer, Mindre)
- [ ] Create `Commands.cs` with all command records
- [ ] Create `Events.cs` with all event records
- [ ] Create `Errors.cs` with all error records
- [ ] Create `State.cs` with `GameState` record
- [ ] Create `PlayerScore` record for scoring breakdown

## Phase 2: Decider Implementation
- [ ] Implement `Evolve` function with pattern matching
- [ ] Implement `Decide` function with pattern matching
- [ ] Implement scoring logic: diff + bonus calculation
- [ ] Add `Result<T>` type for error handling

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
- [ ] Create `GameRepository` (in-memory event store per game)
- [ ] Implement `GetState` (fold events through Evolve)
- [ ] Implement `Execute` (Decide + append events)

## Phase 6: Read Models (Projections)
- [ ] `LobbyView` — players waiting, join code
- [ ] `QuestionView` — current question, who has answered
- [ ] `ResultsView` — guesses, scores, breakdown per player
- [ ] `ScoreboardView` — running totals, sorted by score (lowest first)

## Phase 7: Web Endpoints
- [ ] `POST /games` — create game
- [ ] `POST /games/{code}/join` — join game
- [ ] `POST /games/{code}/start` — start game (host only)
- [ ] `POST /games/{code}/guess` — submit guess (direction + difference)
- [ ] `POST /games/{code}/next` — next question (host only)
- [ ] `GET /games/{code}/state` — polling endpoint

## Phase 8: Razor Pages + HTMX
- [ ] Home page with "Create Game" button
- [ ] Lobby page with QR code and player list
- [ ] Question page with mer/mindre buttons + difference slider
- [ ] Results page with answers revealed + score breakdown
- [ ] Final scoreboard page

## Phase 9: Polish
- [ ] Mobile-friendly CSS
- [ ] PWA manifest
- [ ] Error handling UI
- [ ] Loading states
