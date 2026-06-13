# Authoring the emlang spec (`game-flows.yaml`)

This folder holds the **Event-Modeling spec** for one round ("omgång") of
*Mer eller Mindre*. `game-flows.yaml` is the single source of truth that the C#
domain maps to 1:1. It is written in **emlang YAML** (CLI v1.0.0) — not the old
custom `.em` DSL.

The board this spec transcribes is `specs/MEM-omgång.png`.

## Running the tools

`emlang` lives on PATH via `~/go/bin`. If a fresh shell can't find it:

```bash
export PATH="$PATH:/c/Program Files/Go/bin:$HOME/go/bin"
```

From the repo root:

```bash
emlang lint specs/game-flows.yaml       # must print: OK (no issues found)
emlang parse specs/game-flows.yaml      # inspect parsed structure
emlang fmt  specs/game-flows.yaml -w    # format in place
emlang diagram specs/game-flows.yaml -o specs/game-flows.html   # visual review
```

`emlang` reads `.emlang.yaml` from the repo root. It ignores `slice-missing-event`
so that standalone read-model (view-only) slices stay lint-clean.

## YAML cheat-sheet

Root is a single `slices:` map. Each slice is either:

- **direct form** — a list of step elements (no tests):

  ```yaml
  slices:
    OpenLobby:
      - t: GameMaster / Quiz catalog
      - c: OpenLobby
      - e: Game / LobbyOpened
      - v: Game lobby
  ```

- **extended form** — `steps:` + `tests:`:

  ```yaml
  slices:
    JoinGame:
      steps:
        - t: Player / Join form
        - c: JoinGame
        - e: Game / PlayerJoined
      tests:
        PlayerJoins:
          given:
            - e: Game / LobbyOpened
          when:
            - c: JoinGame
              props: { playerName: Nils }
          then:
            - e: Game / PlayerJoined
              props: { playerName: Nils }
  ```

### Element type keys (exactly one per element, plus optional `props`)

| Type      | Short | Long         |
|-----------|-------|--------------|
| Trigger   | `t:`  | `trigger:`   |
| Command   | `c:`  | `command:`   |
| Event     | `e:`  | `event:`     |
| Exception | `x:`  | `exception:` |
| View      | `v:`  | `view:`      |

### Tests (given / when / then)

- `given`: events + views only
- `when`: commands only
- `then`: events + views + exceptions only
- All sections optional.

## Swimlane / naming conventions (locked decisions)

Two user roles + System. Events live on the `Game` stream; views are bare.

- **Triggers carry the actor role + originating screen**: a command is issued *from*
  a screen, which is itself a read model. Name the trigger after that screen:
  `GameMaster / Quiz catalog`, `Player / Question`, `GameMaster / Game lobby`. The
  one exception is a plain UI entry point with no backing read model — the
  `Player / Join form` (you type a join code before any game state exists for you).
  `System / ...` triggers name the policy.
- **`⚙️ System / ...` triggers carry a gear-emoji prefix.** Rationale: in canonical
  Event Modeling a processor is drawn as a **gear icon in the initiator lane** — it sits
  where a human's UI wireframe would sit, above the command it fires (see the hotel
  example's "Checkout Processor" / "Payment Processor" gears). emlang has no cog element
  and renders a trigger as `Role → swimlane` + `Name → box`, so prefixing the System
  trigger's role (`⚙️ System`) puts the gear in the **initiator lane** — the faithful
  analog of that gear icon, NOT a slice-name marker. It parses as plain UTF-8 (lint does
  not validate name content) and renders on the `⚙️ System` swimlane label. Apply it
  CONSISTENTLY to all three System processors (`⚙️ System / Score question`,
  `⚙️ System / Ask next question`, `⚙️ System / End game`) — never a subset, or the
  swimlane labels disagree. The three System processors share ONE `System` initiator
  lane (distinguished by box name), a simplification of the canonical per-processor gear.
  Human-role triggers (`GameMaster /`, `Player /`) stay bare.
- **Commands are bare**: `OpenLobby`, `SubmitGuess`.
- **Events carry the stream**: `Game / LobbyOpened`.
- **Exceptions are bare**: `GameNotFound`.
- **Views carry a view lane**: `Screen / Game lobby`, `Todo / Outstanding guesses`
  (see below).

### Automations (processor pattern)

emlang has **no dedicated cog/gear element**. An automation is the Event-Modeling
**processor pattern** (cf. the official "Invoice generation" example):

```
event(s) → Todo read-model → System trigger (the gear) → command → event(s) → Screen
```

Consequences for this spec:

- **"All guesses in" is a read-model condition, not an event.** It is the
  `allGuessesIn` flag on `Todo / Outstanding guesses`; the `System / Score question`
  processor observes it. The player count is known, so there is no
  `AllGuessesSubmitted` event.
- **`QuestionScored` is per-player** — one event per `playerId`. A single
  `ScoreQuestion` emits one `QuestionAnswered` (the answer reveal) plus N
  `QuestionScored`.
- The three System slices (`Score question`, `Ask next question`, `End game`) each
  read a `Todo /` read-model before their trigger. `Ask next question` / `End game`
  both react to `QuestionAnswered` and are mutually exclusive on
  `Todo / Game progress`'s `hasNextQuestion`.

### Views (read models)

Two view lanes distinguish human screens from processor-facing projections:

- `Screen / ...` — screens players & the GM actually see.
- `Todo / ...` — read-models a `System /` processor consumes; never shown to a human.

> **NOTE**: emlang parses + lints both lanes fine, but the diagram renders views by
> their bare name without a per-view lane label (only triggers and events get a
> visible swimlane label). The prefix still documents intent and maps to code, so we
> keep it regardless.

| View (Screen lane)            | Shown when                                  |
|-------------------------------|---------------------------------------------|
| `Screen / Quiz catalog`       | GM browses packs (kviss) to start a game    |
| `Screen / Game lobby`         | After open-lobby / join, waiting to start   |
| `Screen / Question`           | A question is presented (Q0 or next)        |
| `Screen / Waiting for others` | Player guessed, others still pending        |
| `Screen / Round results`      | Question scored, per-player round + total   |
| `Screen / Final standings`    | Game ended, scoreboard + winner             |

| View (Todo lane)              | Consumed by                                 |
|-------------------------------|---------------------------------------------|
| `Todo / Outstanding guesses`  | `System / Score question` (allGuessesIn)    |
| `Todo / Game progress`        | `System / Ask next question`, `End game`    |

`Screen / Question` derives its card text/options from the chosen **question pack**
by index; those are not carried on events.

Roles:

- `GameMaster /` — exactly one (Martin, id0): `OpenLobby`, `StartGame`.
- `Player /` — one or more (Nils id1, Sven id2): `JoinGame`, `SubmitGuess`.
  **The Game Master also plays** — Martin guesses through the same `Player /`
  `SubmitGuess` slice (GM ⊃ Player).
- `System /` — processors named by action: `System / Score question`,
  `System / Ask next question`, `System / End game`. The GM does **not** manually
  advance or end.

### Event vocabulary

`LobbyOpened → PlayerJoined → GameStarted → GuessSubmitted → QuestionAnswered →
QuestionScored(×players) → NextQuestionStarted → … → GameEnded`.

- `QuestionAnswered {questionIndex, correctDirection, correctDifference}` — the
  answer reveal, once per question.
- `QuestionScored {…, playerId, …}` — **per-player** scoring, one per player.
- No `AllGuessesSubmitted` event (it is a `Todo / Outstanding guesses` condition).

Granularity: **behavior-based slices** (one `JoinGame`, one `SubmitGuess`), not
per-player. Concrete players (Martin id0, Nils id1, Sven id2) appear as test props /
per-player test cases, keeping the model generic to N players.

### Prop type vocabulary

emlang props are free-form annotations (`emlang lint` does not validate them), but
this spec is the single source of truth the C# domain maps to 1:1, so the annotations
**are** the intended types. Apply this vocabulary consistently:

| Prop family                                   | Type                    |
|-----------------------------------------------|-------------------------|
| `gameId` `playerId` `hostPlayerId` `winnerId` | `Guid`                  |
| `questionPackId` `packId`                     | `Guid`                  |
| `submittedPlayerIds` `pendingPlayerIds`       | `Guid[]`                |
| `joinCode`                                    | `Guid`                  |
| `*At` (created/joined/started/submitted/ended)| `DateTimeOffset`        |
| `direction` `correctDirection` `guessedDirection` | `Direction (mer\|mindre)` |
| `difference` `correctDifference` `guessedDifference` | `int (0-100)`    |
| `differencePoints` `bonusPoints` `roundScore` `totalScore` | `int` (may be negative) |
| index/count (`questionIndex`, `totalQuestions`, `questionCount`, …) | `int`  |
| `allGuessesIn` `hasNextQuestion` `directionCorrect` `isHost` | `bool`      |
| names/text (`hostName`, `playerName`, `questionText`, `optionA/B`, `name`) | `string` |

Notes:

- **`direction` is a `Direction` enum** with members `mer | mindre`. Annotated
  `Direction (mer|mindre)` on the `SubmitGuess` command **and** on the events
  (`GuessSubmitted`, `QuestionAnswered.correctDirection`,
  `QuestionScored.guessedDirection`) — the event types are not weaker than the command.
- **`joinCode` is a `Guid`** — a minted join token, distinct from `gameId`.
- **Timestamps are `DateTimeOffset`** (unambiguous instant; survives serialization
  without `DateTime.Kind` loss; matches `GameEvent.OccurredAt` / `GameContext.Now`).
- Complex element types carry precise field types inline:
  `QuestionPack { packId: Guid, name: string, questionCount: int }`,
  `Player { playerId: Guid, name: string, isHost: bool }`,
  `PlayerScore { playerId: Guid, roundScore: int, totalScore: int }`,
  `ScoreboardEntry { playerId: Guid, playerName: string, totalScore: int }`.
- **`State / Game` folds the whole deck as `questions: IReadOnlyList<QuestionRound>`** —
  the game loads all questions up front and each player's guess folds into its question.
  `QuestionRound { card: Question, guesses: IReadOnlyDictionary<Guid, Guess>,
  correctDirection: Direction?, correctDifference: int?, roundScores:
  IReadOnlyDictionary<Guid, int>, scored: bool }`. Progress is DERIVED, not stored:
  `pendingPlayerIds(i)`, `allGuessesIn(i)`, `currentQuestionScored`, `hasNextQuestion`,
  and `totalScore(p) = Σ scored questions roundScores[p]`. `Todo / Outstanding guesses`
  mirrors this as one row per question (`OutstandingQuestion { questionIndex,
  pendingPlayerIds, allGuessesIn }`) — a real todo list the scorer gears on.

> The current C# records (string ids, `int Guess`, single `DateTimeOffset` on
> `OccurredAt`) are reconciled to this vocabulary in a later, separate effort.

## SV ↔ EN glossary

| Svenska (board)        | English (spec/code) |
|------------------------|---------------------|
| omgång (spelomgång)    | Game (session)      |
| spelledare / värd      | GameMaster (host)   |
| spelare                | Player              |
| kviss / frågepaket     | QuestionPack        |
| fråga (frågekort)      | Question (card)     |
| gissning               | Guess               |
| riktning (mer/mindre)  | direction           |
| differens / skillnad   | difference          |
| poäng                  | score               |
| resultat               | Round results       |
| slutställning          | Final standings     |
| vinnare                | winner              |

**QuestionPack**: a deck of designated question-cards (one card per question, e.g.
"0-100"). Choosing to play MEM = choosing a pack from the `Quiz catalog`. The
`questionPackId` on `LobbyOpened` *is* the catalog selection — there is no separate
`quizId`. The catalog is reference data, not on the `Game` stream.

## Iterative workflow

1. **Spec first** — edit `game-flows.yaml`.
2. **Lint** after every change until `OK (no issues found)`.
3. **Diagram** and compare against `MEM-omgång.png`.
4. Then propagate to C# domain (records, Decider, tests) — a separate effort.

When a board detail is ambiguous, pick the simplest board-faithful option and leave
a `# ASSUMPTION:` comment in the YAML.
