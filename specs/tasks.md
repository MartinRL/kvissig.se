# Implementation Tasks

## Phase 1: Domain Types
- [ ] Create `Commands.cs` with all command records from spec
- [ ] Create `Events.cs` with all event records from spec
- [ ] Create `Errors.cs` with all error records from spec
- [ ] Create `State.cs` with `GameState` record

## Phase 2: Decider Implementation
- [ ] Implement `Evolve` function with pattern matching
- [ ] Implement `Decide` function with pattern matching
- [ ] Add `Result<T>` type for error handling

## Phase 3: GWT Tests
- [ ] `GameCanBeCreated` test
- [ ] `PlayerCanJoinLobby` test
- [ ] `CannotJoinNonexistentGame` test
- [ ] `CannotJoinStartedGame` test
- [ ] `GuessSubmittedSuccessfully` test
- [ ] `GuessOutOfRangeRejected` test
- [ ] `CannotGuessAgainOnSameQuestion` test
- [ ] `AllGuessesTriggersScoring` test
- [ ] `ClosestGuessWins` test
- [ ] `LastQuestionEndsGame` test

## Phase 4: Question Loading
- [ ] Create `Question` record
- [ ] Create `QuestionRepository` with CSV loading
- [ ] Add sample `questions.csv` with 10 questions

## Phase 5: Game Repository
- [ ] Create `GameRepository` (in-memory event store per game)
- [ ] Implement `GetState` (fold events through Evolve)
- [ ] Implement `Execute` (Decide + append events)

## Phase 6: Read Models (Projections)
- [ ] `LobbyView` - players waiting
- [ ] `QuestionView` - current question + who has answered
- [ ] `ResultsView` - guesses + scores for round
- [ ] `ScoreboardView` - running totals

## Phase 7: Web Endpoints
- [ ] `POST /games` - create game
- [ ] `POST /games/{code}/join` - join game
- [ ] `POST /games/{code}/start` - start game
- [ ] `POST /games/{code}/guess` - submit guess
- [ ] `GET /games/{code}/state` - polling endpoint

## Phase 8: Razor Pages + HTMX
- [ ] Home page with "Create Game" button
- [ ] Lobby page with QR code and player list
- [ ] Question page with guess input
- [ ] Results page with answers revealed
- [ ] Final scoreboard page

## Phase 9: Polish
- [ ] Mobile-friendly CSS
- [ ] PWA manifest
- [ ] Error handling UI
- [ ] Loading states
