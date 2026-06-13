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
- [ ] `game can be started`
- [ ] `cannot start nonexistent game`
- [ ] `cannot start without enough players`

### Guess Tests
- [ ] `guess submitted successfully`
- [ ] `cannot guess in nonexistent game`
- [ ] `cannot guess before game starts`
- [ ] `cannot guess as non-member`
- [ ] `cannot guess again on same question`
- [ ] `difference out of range rejected`

### Scoring Tests
- [ ] `all guesses scored and answer revealed`
- [ ] `exact difference with correct direction`
- [ ] `scores accumulate across rounds`
- [ ] `cannot score before all guesses in`
- [ ] `cannot score an already-scored question`
- [ ] `lowest score wins` (deferred to End Game — winner determination)

## Phase 4: Question Loading
- [ ] Create `Question` record (text, optionA, optionB, correctDirection, correctDifference)
- [ ] Create `QuestionRepository` with CSV loading
- [ ] Add sample `questions.csv` with 10 questions

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
